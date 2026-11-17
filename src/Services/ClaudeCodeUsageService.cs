using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dockus.Models;

namespace Dockus.Services;

/// <summary>
/// Límites del plan de Claude (Pro / Max / Team) leídos del archivo que escribe
/// el script de statusline de Claude Code. No usa API keys ni credenciales.
/// </summary>
public class ClaudePlanUsage
{
    public QuotaBucket? FiveHour { get; set; }
    public QuotaBucket? Weekly { get; set; }
    /// <summary>Límites semanales por modelo (Opus, Sonnet...) cuando la fuente los reporta.</summary>
    public List<QuotaBucket> ModelBuckets { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
    /// <summary>"claude.ai" o "Claude Code".</summary>
    public string Source { get; set; } = "Claude Code";

    public bool HasData => FiveHour != null || Weekly != null;

    /// <summary>La ventana con menos margen (la que se agotaría primero).</summary>
    public QuotaBucket? TightestBucket =>
        FiveHour == null ? Weekly
        : Weekly == null ? FiveHour
        : (Weekly.RemainingPercentage < FiveHour.RemainingPercentage ? Weekly : FiveHour);
    public TimeSpan Age => DateTime.Now - UpdatedAt;
    public bool IsStale => Age > TimeSpan.FromHours(1);

    public string FormattedAge
    {
        get
        {
            var age = Age;
            if (age.TotalMinutes < 1) return "ahora";
            if (age.TotalHours < 1) return $"hace {(int)age.TotalMinutes}m";
            if (age.TotalDays < 1) return $"hace {(int)age.TotalHours}h";
            return $"hace {(int)age.TotalDays}d";
        }
    }
}

/// <summary>Último uso del plan elegido por la ventana principal (claude.ai o Claude Code).</summary>
public static class ClaudePlanState
{
    public static ClaudePlanUsage? Current { get; set; }
}

public enum ClaudeStatusLineState
{
    NotInstalled,
    InstalledDockus,
    OtherStatusLine
}

public static class ClaudeCodeUsageService
{
    private const string ScriptResourceName = "Dockus.scripts.dockus-statusline.ps1";
    private const string ScriptMarker = "dockus-statusline.ps1";

    private static readonly string DockusDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dockus");

    public static string UsageFilePath => Path.Combine(DockusDataDir, "claude-usage.json");
    public static string ScriptPath => Path.Combine(DockusDataDir, "dockus-statusline.ps1");

    /// <summary>Respeta CLAUDE_CONFIG_DIR si el usuario cambió la carpeta de configuración.</summary>
    public static string ClaudeSettingsPath
    {
        get
        {
            var configDir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            if (string.IsNullOrWhiteSpace(configDir))
            {
                configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
            }
            return Path.Combine(configDir, "settings.json");
        }
    }

    public static ClaudePlanUsage? ReadUsage()
    {
        try
        {
            if (!File.Exists(UsageFilePath)) return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(UsageFilePath));
            var root = doc.RootElement;

            var usage = new ClaudePlanUsage
            {
                UpdatedAt = ReadEpoch(root, "updated_at")?.ToLocalTime() ?? File.GetLastWriteTime(UsageFilePath),
                FiveHour = ReadBucket(root, "five_hour", "5h", "Ventana de 5 horas"),
                Weekly = ReadBucket(root, "seven_day", "weekly", "Límite semanal")
            };
            return usage.HasData ? usage : null;
        }
        catch
        {
            return null;
        }
    }

    private static QuotaBucket? ReadBucket(JsonElement root, string name, string window, string displayName)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Object) return null;
        if (!el.TryGetProperty("used_percentage", out var usedEl)) return null;

        var used = ReadNumber(usedEl);
        if (used == null) return null;
        return MakeBucket(name, window, displayName, used.Value, ReadEpoch(el, "resets_at"));
    }

    internal static double? ReadNumber(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number) return el.GetDouble();
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return null;
    }

    /// <summary>Crea un bucket a partir del % usado. Si la ventana ya se reinició, el uso vuelve a 0 %.</summary>
    internal static QuotaBucket MakeBucket(string id, string window, string displayName, double usedPercent, DateTime? resetUtc)
    {
        var bucket = new QuotaBucket
        {
            BucketId = id,
            Window = window,
            DisplayName = displayName,
            RemainingFraction = Math.Clamp(1.0 - usedPercent / 100.0, 0, 1),
            ResetTime = resetUtc
        };
        if (bucket.ResetTime != null && bucket.ResetTime.Value <= DateTime.UtcNow)
        {
            bucket.RemainingFraction = 1.0;
            bucket.ResetTime = null;
        }
        return bucket;
    }

    /// <summary>Acepta segundos Unix (número o texto) o una fecha ISO 8601. Devuelve UTC.</summary>
    internal static DateTime? ReadEpoch(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var el)) return null;
        try
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var secs))
                return DateTimeOffset.FromUnixTimeSeconds(secs).UtcDateTime;
            if (el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString();
                if (long.TryParse(s, out var secs2)) return DateTimeOffset.FromUnixTimeSeconds(secs2).UtcDateTime;
                if (DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal, out var dto)) return dto.UtcDateTime;
            }
        }
        catch { }
        return null;
    }

    public static ClaudeStatusLineState GetStatusLineState()
    {
        try
        {
            var settings = LoadClaudeSettings();
            var command = settings?["statusLine"]?["command"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(command)) return ClaudeStatusLineState.NotInstalled;
            return command.Contains(ScriptMarker, StringComparison.OrdinalIgnoreCase)
                ? ClaudeStatusLineState.InstalledDockus
                : ClaudeStatusLineState.OtherStatusLine;
        }
        catch
        {
            return ClaudeStatusLineState.NotInstalled;
        }
    }

    /// <summary>
    /// Copia el script a %APPDATA%\Dockus y registra la statusLine en la configuración
    /// de Claude Code (con copia de respaldo). No reemplaza una statusLine ajena.
    /// </summary>
    public static (bool Success, string Message) InstallStatusLine()
    {
        try
        {
            var settings = LoadClaudeSettings() ?? new JsonObject();
            var existing = settings["statusLine"]?["command"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains(ScriptMarker, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "✗ Ya tienes otra statusLine configurada en Claude Code. No se reemplazó; revisa el README para combinarlas.");
            }

            Directory.CreateDirectory(DockusDataDir);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResourceName))
            {
                if (stream == null) return (false, "✗ No se encontró el script de statusline dentro de Dockus.");
                using var file = File.Create(ScriptPath);
                stream.CopyTo(file);
            }

            // Barras normales: el comando funciona igual en Git Bash, cmd y PowerShell.
            var scriptForCommand = ScriptPath.Replace('\\', '/');
            settings["statusLine"] = new JsonObject
            {
                ["type"] = "command",
                ["command"] = $"powershell -NoProfile -ExecutionPolicy Bypass -File \"{scriptForCommand}\""
            };
            SaveClaudeSettings(settings);

            return (true, "✓ Conectado. Los límites aparecen después de la próxima respuesta de Claude Code en la terminal.");
        }
        catch (Exception ex)
        {
            return (false, $"✗ No se pudo configurar Claude Code: {ex.Message}");
        }
    }

    public static (bool Success, string Message) UninstallStatusLine()
    {
        try
        {
            var settings = LoadClaudeSettings();
            var existing = settings?["statusLine"]?["command"]?.GetValue<string>();
            if (settings == null || string.IsNullOrWhiteSpace(existing))
                return (true, "No había statusLine configurada.");
            if (!existing.Contains(ScriptMarker, StringComparison.OrdinalIgnoreCase))
                return (false, "✗ La statusLine actual no es de Dockus; no se tocó.");

            settings.Remove("statusLine");
            SaveClaudeSettings(settings);
            return (true, "✓ Desconectado de Claude Code.");
        }
        catch (Exception ex)
        {
            return (false, $"✗ No se pudo quitar la statusLine: {ex.Message}");
        }
    }

    private static JsonObject? LoadClaudeSettings()
    {
        var path = ClaudeSettingsPath;
        if (!File.Exists(path)) return null;
        var text = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text)) return new JsonObject();
        // Si el archivo no es JSON válido se lanza la excepción y no se sobrescribe.
        return JsonNode.Parse(text, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) as JsonObject ?? throw new InvalidDataException("settings.json de Claude Code no es un objeto JSON.");
    }

    private static void SaveClaudeSettings(JsonObject settings)
    {
        var path = ClaudeSettingsPath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        if (File.Exists(path))
        {
            File.Copy(path, path + ".dockus-backup", overwrite: true);
        }

        var json = settings.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            // Deja las comillas y tildes legibles en el settings.json del usuario
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var tmp = path + ".dockus-tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }
}
