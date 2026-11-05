using System;
using System.IO;
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

    // Optional custom API keys for external providers
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
                if (settings != null) return settings;
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
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }
}

