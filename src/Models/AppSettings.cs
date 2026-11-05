using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dockus.Models;

public class AppSettings
{
    public string DockPosition { get; set; } = "BottomRight"; // BottomRight, BottomCenter, BottomLeft, Floating
    public bool AlwaysOnTop { get; set; } = true;
    public double GlassOpacity { get; set; } = 0.90;
    public long MaxContextTokens { get; set; } = 1_000_000;
    public string ActiveProviderId { get; set; } = "antigravity_gemini";
    public bool AutoSnapToTaskbar { get; set; } = true;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public bool IsExpanded { get; set; } = false;
    public bool IsHourlyMode { get; set; } = false;

    // Optional custom API keys for external providers (Decrypted in-memory, encrypted at rest via DPAPI)
    public string GeminiApiKey { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string DeepSeekApiKey { get; set; } = string.Empty;
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dockus",
        "settings.json"
    );

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    settings.GeminiApiKey = DecryptSecret(settings.GeminiApiKey);
                    settings.OpenAiApiKey = DecryptSecret(settings.OpenAiApiKey);
                    settings.ClaudeApiKey = DecryptSecret(settings.ClaudeApiKey);
                    settings.DeepSeekApiKey = DecryptSecret(settings.DeepSeekApiKey);
                    return settings;
                }
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Create a clone with encrypted secrets for on-disk security (Windows DPAPI)
            var onDisk = new AppSettings
            {
                DockPosition = this.DockPosition,
                AlwaysOnTop = this.AlwaysOnTop,
                GlassOpacity = this.GlassOpacity,
                MaxContextTokens = this.MaxContextTokens,
                ActiveProviderId = this.ActiveProviderId,
                AutoSnapToTaskbar = this.AutoSnapToTaskbar,
                WindowLeft = this.WindowLeft,
                WindowTop = this.WindowTop,
                IsExpanded = this.IsExpanded,
                IsHourlyMode = this.IsHourlyMode,
                GeminiApiKey = EncryptSecret(this.GeminiApiKey),
                OpenAiApiKey = EncryptSecret(this.OpenAiApiKey),
                ClaudeApiKey = EncryptSecret(this.ClaudeApiKey),
                DeepSeekApiKey = EncryptSecret(this.DeepSeekApiKey),
                OllamaBaseUrl = this.OllamaBaseUrl
            };

            var json = JsonSerializer.Serialize(onDisk, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }

    private static string EncryptSecret(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;
        if (plainText.StartsWith("dpapi:")) return plainText;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plainText.Trim());
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return "dpapi:" + Convert.ToBase64String(encrypted);
        }
        catch
        {
            return plainText;
        }
    }

    private static string DecryptSecret(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText)) return string.Empty;
        if (!cipherText.StartsWith("dpapi:")) return cipherText;
        try
        {
            var encrypted = Convert.FromBase64String(cipherText[6..]);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}
