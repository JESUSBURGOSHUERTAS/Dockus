using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Dockus.Models;
using Microsoft.Web.WebView2.Core;

namespace Dockus.Services;

/// <summary>
/// Lee el uso del plan de Claude (5 horas, semanal y por modelo) desde claude.ai con la
/// sesión que el usuario inicia en la ventana de Dockus (WebView2).
///
/// - La sesión (cookies) vive solo en %LOCALAPPDATA%\Dockus\ClaudeWebSession y la cifra
///   el propio WebView2 con la cuenta de Windows. Dockus nunca lee ni guarda la cookie:
///   las consultas se hacen con fetch() dentro de un WebView2 oculto.
/// - Usa los servicios internos de claude.ai (no documentados para terceros): si
///   cambian, Dockus muestra "formato no reconocido" y guarda la respuesta para revisarla.
/// </summary>
public sealed class ClaudeWebUsageService : IDisposable
{
    private const string BaseUrl = "https://claude.ai";
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ForcedMinInterval = TimeSpan.FromSeconds(20);

    public static string UserDataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dockus", "ClaudeWebSession");

    public static string DebugFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dockus", "claude-web-usage-debug.json");

    /// <summary>Copia de las cookies de claude.ai cifrada con DPAPI (solo la cuenta de Windows actual).</summary>
    public static string SessionSnapshotPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dockus", "claude-session.dat");

    /// <summary>Registro de diagnóstico: estados HTTP y conteos, nunca valores de cookies.</summary>
    public static string LogFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dockus", "claude-web.log");

    private bool _restoreAttempted;

    private CoreWebView2Environment? _env;
    private CoreWebView2Controller? _controller;
    private Window? _hostWindow;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _orgId;
    private DateTime _lastAttempt = DateTime.MinValue;
    private ClaudePlanUsage? _last;

    /// <summary>Texto para mostrar en la tarjeta de Claude.</summary>
    public string LastStatus { get; private set; } = string.Empty;

    /// <summary>true cuando claude.ai respondió que la sesión no es válida.</summary>
    public bool SessionExpired { get; private set; }

    public async Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        if (_env != null) return _env;
        Directory.CreateDirectory(UserDataFolder);
        _env = await CoreWebView2Environment.CreateAsync(null, UserDataFolder);
        return _env;
    }

    private async Task<CoreWebView2> EnsureCoreAsync()
    {
        if (_controller != null) return _controller.CoreWebView2;

        // Ventana WPF que nunca se muestra: solo aporta el HWND padre del WebView2 oculto.
        _hostWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false
        };
        var hwnd = new WindowInteropHelper(_hostWindow).EnsureHandle();

        var env = await GetEnvironmentAsync();
        _controller = await env.CreateCoreWebView2ControllerAsync(hwnd);
        _controller.IsVisible = false;

        var core = _controller.CoreWebView2;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;

        if (!_restoreAttempted)
        {
            _restoreAttempted = true;
            await RestoreSessionSnapshotAsync(core);
        }
        return core;
    }

    private sealed record CookieDto(string Name, string Value, string Domain, string Path,
        DateTime? Expires, bool HttpOnly, bool Secure, int SameSite);

    /// <summary>
    /// Guarda las cookies de claude.ai cifradas con DPAPI. Así la sesión sobrevive aunque el
    /// navegador integrado no persista alguna cookie (p. ej. cookies de sesión sin vencimiento).
    /// </summary>
    private static async Task SaveSessionSnapshotAsync(CoreWebView2 core)
    {
        try
        {
            var cookies = await core.CookieManager.GetCookiesAsync(BaseUrl);
            var list = new List<CookieDto>();
            foreach (var c in cookies)
            {
                list.Add(new CookieDto(c.Name, c.Value, c.Domain, c.Path,
                    c.IsSession ? null : c.Expires, c.IsHttpOnly, c.IsSecure, (int)c.SameSite));
            }
            var plain = JsonSerializer.SerializeToUtf8Bytes(list);
            var encrypted = System.Security.Cryptography.ProtectedData.Protect(
                plain, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            Array.Clear(plain);

            Directory.CreateDirectory(Path.GetDirectoryName(SessionSnapshotPath)!);
            var tmp = SessionSnapshotPath + ".tmp";
            File.WriteAllBytes(tmp, encrypted);
            File.Move(tmp, SessionSnapshotPath, overwrite: true);
            Log($"copia de sesión guardada ({list.Count} cookies)");
        }
        catch (Exception ex)
        {
            Log("no se pudo guardar la copia de sesión: " + ex.GetType().Name);
        }
    }

    /// <summary>Restaura solo las cookies que falten (no pisa valores más nuevos del navegador).</summary>
    private static async Task RestoreSessionSnapshotAsync(CoreWebView2 core)
    {
        try
        {
            var existing = await core.CookieManager.GetCookiesAsync(BaseUrl);
            var present = new HashSet<string>();
            foreach (var c in existing) present.Add(c.Name + "|" + c.Domain + "|" + c.Path);

            if (!File.Exists(SessionSnapshotPath))
            {
                Log($"inicio: {existing.Count} cookies en el perfil, sin copia de sesión");
                return;
            }

            var plain = System.Security.Cryptography.ProtectedData.Unprotect(
                File.ReadAllBytes(SessionSnapshotPath), null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            var list = JsonSerializer.Deserialize<List<CookieDto>>(plain) ?? new();
            Array.Clear(plain);

            int restored = 0;
            foreach (var d in list)
            {
                if (present.Contains(d.Name + "|" + d.Domain + "|" + d.Path)) continue;
                if (d.Expires != null && d.Expires.Value <= DateTime.Now) continue;

                var cookie = core.CookieManager.CreateCookie(d.Name, d.Value, d.Domain, d.Path);
                if (d.Expires != null) cookie.Expires = d.Expires.Value;
                cookie.IsHttpOnly = d.HttpOnly;
                cookie.IsSecure = d.Secure;
                cookie.SameSite = (CoreWebView2CookieSameSiteKind)d.SameSite;
                core.CookieManager.AddOrUpdateCookie(cookie);
                restored++;
            }
            Log($"inicio: {existing.Count} cookies en el perfil, {restored} restauradas de la copia ({list.Count} en la copia)");
        }
        catch (Exception ex)
        {
            Log("no se pudo restaurar la copia de sesión: " + ex.GetType().Name);
        }
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            var info = new FileInfo(LogFilePath);
            if (info.Exists && info.Length > 200_000) File.Delete(LogFilePath);
            File.AppendAllText(LogFilePath, $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch { }
    }

    /// <summary>true solo si claude.ai dice explícitamente que la sesión no es válida (no un bloqueo antibots).</summary>
    private static bool IsSessionInvalid(int status, string body) =>
        status == 401 ||
        (status == 403 && (body.Contains("account_session_invalid", StringComparison.Ordinal) ||
                           body.Contains("\"permission_error\"", StringComparison.Ordinal)));

    /// <summary>Carga una página de claude.ai para que fetch() corra con el origen y las cookies correctas.</summary>
    private static async Task EnsureOriginAsync(CoreWebView2 core)
    {
        if (core.Source?.StartsWith(BaseUrl + "/", StringComparison.OrdinalIgnoreCase) == true) return;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? s, CoreWebView2NavigationCompletedEventArgs e) => tcs.TrySetResult(e.IsSuccess);
        core.NavigationCompleted += Handler;
        try
        {
            // Respuesta JSON liviana: no carga la aplicación web completa.
            core.Navigate(BaseUrl + "/api/organizations");
            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        }
        finally
        {
            core.NavigationCompleted -= Handler;
        }
    }

    private static async Task<(int Status, string Body)> FetchAsync(CoreWebView2 core, string path)
    {
        var expression =
            "(async()=>{try{const r=await fetch(" + JsonSerializer.Serialize(path) +
            ",{credentials:'include',headers:{'accept':'application/json'}});" +
            "return JSON.stringify({s:r.status,b:await r.text()});}" +
            "catch(e){return JSON.stringify({s:0,b:String(e)});}})()";

        var args = JsonSerializer.Serialize(new { expression, awaitPromise = true, returnByValue = true });
        var raw = await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", args);

        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("result", out var result) ||
            !result.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.String)
        {
            return (0, string.Empty);
        }

        using var inner = JsonDocument.Parse(value.GetString()!);
        var status = inner.RootElement.GetProperty("s").GetInt32();
        var body = inner.RootElement.GetProperty("b").GetString() ?? string.Empty;
        return (status, body);
    }

    /// <summary>Comprueba si hay una sesión válida de claude.ai (sin leer el uso).</summary>
    public async Task<bool> CheckSignedInAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var core = await EnsureCoreAsync();
            await EnsureOriginAsync(core);
            var (status, _) = await FetchAsync(core, "/api/organizations");
            Log($"comprobación de sesión: HTTP {status}");
            if (status == 200) await SaveSessionSnapshotAsync(core);
            return status == 200;
        }
        catch
        {
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Devuelve el uso del plan. Sin <paramref name="force"/> consulta como máximo cada 3 minutos
    /// y devuelve el último valor entre consultas.
    /// </summary>
    public async Task<ClaudePlanUsage?> GetUsageAsync(bool force = false)
    {
        var sinceLast = DateTime.Now - _lastAttempt;
        if (sinceLast < (force ? ForcedMinInterval : NormalInterval)) return _last;
        if (!await _lock.WaitAsync(0)) return _last;

        try
        {
            _lastAttempt = DateTime.Now;
            var core = await EnsureCoreAsync();
            await EnsureOriginAsync(core);

            var candidates = new List<string>();
            if (_orgId != null) candidates.Add(_orgId);

            if (_orgId == null)
            {
                var (status, body) = await FetchAsync(core, "/api/organizations");
                Log($"organizaciones: HTTP {status}" + (status == 403 && !IsSessionInvalid(status, body) ? " (no es error de sesión)" : ""));
                if (IsSessionInvalid(status, body))
                {
                    SessionExpired = true;
                    _last = null;
                    LastStatus = "✗ La sesión de claude.ai no es válida o venció. Vuelve a iniciar sesión.";
                    return null;
                }
                if (status != 200)
                {
                    LastStatus = status == 0
                        ? "✗ Sin conexión con claude.ai. Se reintentará."
                        : $"✗ claude.ai respondió HTTP {status}. Se reintentará.";
                    return _last;
                }
                candidates.AddRange(ParseOrganizationIds(body));
            }

            string lastBody = string.Empty;
            foreach (var orgId in candidates)
            {
                var (status, body) = await FetchAsync(core, $"/api/organizations/{Uri.EscapeDataString(orgId)}/usage");
                lastBody = body;
                if (status != 200) Log($"uso: HTTP {status}");
                if (status == 401 || (status == 403 && body.Contains("account_session_invalid", StringComparison.Ordinal)))
                {
                    SessionExpired = true;
                    _orgId = null;
                    _last = null;
                    LastStatus = "✗ La sesión de claude.ai no es válida o venció. Vuelve a iniciar sesión.";
                    return null;
                }
                if (status != 200) continue; // p. ej. organización solo de API, sin uso de plan

                var usage = ParseUsage(body);
                if (usage != null)
                {
                    bool firstForOrg = _orgId != orgId;
                    _orgId = orgId;
                    _last = usage;
                    SessionExpired = false;
                    LastStatus = "✓ Sesión de claude.ai activa. Uso actualizado cada 3 minutos.";
                    if (firstForOrg) Log("uso leído correctamente");
                    await SaveSessionSnapshotAsync(core); // mantiene la copia al día si claude.ai renueva la sesión
                    return usage;
                }
            }

            _orgId = null;
            WriteDebugFile(lastBody);
            LastStatus = candidates.Count == 0
                ? "✗ La cuenta no tiene organizaciones con plan de Claude."
                : "✗ claude.ai respondió con un formato de uso no reconocido. Se guardó en claude-web-usage-debug.json.";
            return _last;
        }
        catch (Exception ex)
        {
            LastStatus = $"✗ Error al consultar claude.ai: {ex.Message}";
            return _last;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Borra la sesión de claude.ai guardada por Dockus (cookies, caché y almacenamiento).</summary>
    public async Task SignOutAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Primero la copia cifrada, para que EnsureCoreAsync no la restaure.
            try { File.Delete(SessionSnapshotPath); } catch { }
            var core = await EnsureCoreAsync();
            core.CookieManager.DeleteAllCookies();
            await core.Profile.ClearBrowsingDataAsync();
            Log("sesión cerrada: cookies, datos del navegador y copia cifrada borrados");
        }
        catch { }
        finally
        {
            _orgId = null;
            _last = null;
            _lastAttempt = DateTime.MinValue;
            SessionExpired = false;
            LastStatus = "Sesión de claude.ai cerrada en Dockus.";
            _lock.Release();
        }
    }

    /// <summary>Permite que la próxima consulta salga de inmediato (p. ej. tras iniciar sesión).</summary>
    public void ResetThrottle()
    {
        _lastAttempt = DateTime.MinValue;
        SessionExpired = false;
    }

    internal static List<string> ParseOrganizationIds(string json)
    {
        var ids = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return ids;

            // Primero las organizaciones con chat (planes Pro/Max/Team); luego el resto.
            var withChat = new List<string>();
            foreach (var org in doc.RootElement.EnumerateArray())
            {
                if (!org.TryGetProperty("uuid", out var uuidEl) || uuidEl.ValueKind != JsonValueKind.String) continue;
                var uuid = uuidEl.GetString()!;
                bool chat = false;
                if (org.TryGetProperty("capabilities", out var caps) && caps.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in caps.EnumerateArray())
                    {
                        if (c.ValueKind == JsonValueKind.String && c.GetString() == "chat") chat = true;
                    }
                }
                (chat ? withChat : ids).Add(uuid);
            }
            withChat.AddRange(ids);
            return withChat;
        }
        catch
        {
            return ids;
        }
    }

    /// <summary>
    /// Interpreta la respuesta de uso. Cada límite es un objeto con "utilization" (% usado)
    /// y "resets_at" (fecha ISO o null), p. ej. five_hour, seven_day, seven_day_opus.
    /// </summary>
    internal static ClaudePlanUsage? ParseUsage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var usage = new ClaudePlanUsage { Source = "claude.ai", UpdatedAt = DateTime.Now };
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Object) continue;
                if (!prop.Value.TryGetProperty("utilization", out var utilEl)) continue;
                var used = ClaudeCodeUsageService.ReadNumber(utilEl);
                if (used == null) continue;

                var reset = ClaudeCodeUsageService.ReadEpoch(prop.Value, "resets_at");
                switch (prop.Name)
                {
                    case "five_hour":
                        usage.FiveHour = ClaudeCodeUsageService.MakeBucket(prop.Name, "5h", "Ventana de 5 horas", used.Value, reset);
                        break;
                    case "seven_day":
                        usage.Weekly = ClaudeCodeUsageService.MakeBucket(prop.Name, "weekly", "Límite semanal", used.Value, reset);
                        break;
                    default:
                        usage.ModelBuckets.Add(ClaudeCodeUsageService.MakeBucket(prop.Name, "weekly", ModelLabel(prop.Name), used.Value, reset));
                        break;
                }
            }
            return usage.HasData ? usage : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ModelLabel(string key)
    {
        var name = key.StartsWith("seven_day_", StringComparison.Ordinal) ? key["seven_day_".Length..] : key;
        name = name switch
        {
            "oauth_apps" => "Apps conectadas",
            _ => name.Replace('_', ' ')
        };
        return name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] + " (semanal)" : key;
    }

    private static void WriteDebugFile(string body)
    {
        try
        {
            // La respuesta de uso solo trae porcentajes y fechas; se recorta por si acaso.
            var text = body.Length > 8000 ? body[..8000] : body;
            Directory.CreateDirectory(Path.GetDirectoryName(DebugFilePath)!);
            File.WriteAllText(DebugFilePath, text);
        }
        catch { }
    }

    public void Dispose()
    {
        try { _controller?.Close(); } catch { }
        _controller = null;
        try { _hostWindow?.Close(); } catch { }
        _hostWindow = null;
    }
}
