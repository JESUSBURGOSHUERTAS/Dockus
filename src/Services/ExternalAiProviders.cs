using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dockus.Models;

namespace Dockus.Services;

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public class GoogleGeminiApiProvider : ITokenProvider
{
    public string Id => "gemini_api";
    public string DisplayName => "Gemini API (Google AI Studio)";
    public string Description => "API directa de Google AI Studio con clave API (Gemini 1.5/2.0/3.x)";
    public string IconGlyph => "✦";
    public string ShortCode => "GEM";
    public string AccentColorHex => "#38BDF8"; // Sky blue

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.GeminiApiKey);

    public async Task<ConnectionTestResult> TestConnectionAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ConnectionTestResult { Success = false, Message = "Ingresa tu API Key de Google AI Studio." };

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey.Trim()}";
            var resp = await client.GetAsync(url);
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                int count = doc.RootElement.TryGetProperty("models", out var arr) ? arr.GetArrayLength() : 0;
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = $"✓ Conexión exitosa ({count} modelos disponibles)",
                    Detail = "Acceso verificado a la API de Google AI Studio."
                };
            }

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"✗ Error HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}",
                Detail = "Verifica que la clave sea válida en aistudio.google.com"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Message = $"✗ Fallo de red: {ex.Message}" };
        }
    }

    public async Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
    {
        var snapshot = new TokenUsageSnapshot
        {
            ProviderId = Id,
            ProviderName = DisplayName,
            IconGlyph = IconGlyph,
            AccentColorHex = AccentColorHex,
            MaxContextTokens = 1_000_000
        };

        if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            snapshot.ConversationTitle = "Configura tu API Key de Google AI Studio en Proveedores";
            return snapshot;
        }

        var test = await TestConnectionAsync(settings.GeminiApiKey);
        if (test.Success)
        {
            snapshot.IsActiveNow = true;
            snapshot.ConversationTitle = test.Message;
            snapshot.MaxContextTokens = 1_000_000;
            snapshot.CurrentContextTokens = 0;
        }
        else
        {
            snapshot.ConversationTitle = test.Message;
        }

        return snapshot;
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

public class OpenAiProvider : ITokenProvider
{
    public string Id => "openai";
    public string DisplayName => "OpenAI (GPT-4o / o1 / o3)";
    public string Description => "Monitoreo de tokens y cuotas de OpenAI API";
    public string IconGlyph => "⚙";
    public string ShortCode => "GPT";
    public string AccentColorHex => "#10B981"; // Emerald green

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey);

    public async Task<ConnectionTestResult> TestConnectionAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ConnectionTestResult { Success = false, Message = "Ingresa tu API Key de OpenAI." };

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey.Trim()}");
            var resp = await client.GetAsync("https://api.openai.com/v1/models");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                int count = doc.RootElement.TryGetProperty("data", out var arr) ? arr.GetArrayLength() : 0;
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = $"✓ Conexión exitosa ({count} modelos)",
                    Detail = "Acceso verificado a la API de OpenAI."
                };
            }

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"✗ Error HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}",
                Detail = "Verifica que la clave sea válida en platform.openai.com"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Message = $"✗ Fallo de red: {ex.Message}" };
        }
    }

    public async Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
    {
        var snapshot = new TokenUsageSnapshot
        {
            ProviderId = Id,
            ProviderName = DisplayName,
            IconGlyph = IconGlyph,
            AccentColorHex = AccentColorHex,
            MaxContextTokens = 128_000
        };

        if (string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            snapshot.ConversationTitle = "Configura tu API Key de OpenAI en Proveedores";
            return snapshot;
        }

        var test = await TestConnectionAsync(settings.OpenAiApiKey);
        snapshot.ConversationTitle = test.Message;
        snapshot.IsActiveNow = test.Success;
        return snapshot;
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

public class ClaudeProvider : ITokenProvider
{
    public string Id => "claude";
    public string DisplayName => "Anthropic Claude (3.5 / 3.7 Sonnet & Opus)";
    public string Description => "Monitoreo de tokens de Anthropic Claude API";
    public string IconGlyph => "✻";
    public string ShortCode => "CLD";
    public string AccentColorHex => "#C084FC"; // Purple/amber

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.ClaudeApiKey);

    public async Task<ConnectionTestResult> TestConnectionAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ConnectionTestResult { Success = false, Message = "Ingresa tu API Key de Anthropic." };

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            var resp = await client.GetAsync("https://api.anthropic.com/v1/models");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                int count = doc.RootElement.TryGetProperty("data", out var arr) ? arr.GetArrayLength() : 0;
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = $"✓ Conexión exitosa ({count} modelos Claude)",
                    Detail = "Acceso verificado a la API de Anthropic."
                };
            }

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"✗ Error HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}",
                Detail = "Verifica que la clave sea válida en console.anthropic.com"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Message = $"✗ Fallo de red: {ex.Message}" };
        }
    }

    public async Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
    {
        var snapshot = new TokenUsageSnapshot
        {
            ProviderId = Id,
            ProviderName = DisplayName,
            IconGlyph = IconGlyph,
            AccentColorHex = AccentColorHex,
            MaxContextTokens = 200_000
        };

        if (string.IsNullOrWhiteSpace(settings.ClaudeApiKey))
        {
            snapshot.ConversationTitle = "Configura tu API Key de Claude en Proveedores";
            return snapshot;
        }

        var test = await TestConnectionAsync(settings.ClaudeApiKey);
        snapshot.ConversationTitle = test.Message;
        snapshot.IsActiveNow = test.Success;
        return snapshot;
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

public class DeepSeekProvider : ITokenProvider
{
    public string Id => "deepseek";
    public string DisplayName => "DeepSeek (V3 / R1)";
    public string Description => "Monitoreo de tokens y cuotas de DeepSeek API";
    public string IconGlyph => "🐋";
    public string ShortCode => "DSK";
    public string AccentColorHex => "#38BDF8"; // Deep blue/cyan

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.DeepSeekApiKey);

    public async Task<ConnectionTestResult> TestConnectionAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ConnectionTestResult { Success = false, Message = "Ingresa tu API Key de DeepSeek." };

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey.Trim()}");

            // DeepSeek user balance endpoint
            var resp = await client.GetAsync("https://api.deepseek.com/user/balance");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                string balanceText = "Activo";
                if (doc.RootElement.TryGetProperty("balance_infos", out var arr) && arr.GetArrayLength() > 0)
                {
                    var first = arr[0];
                    var curr = first.TryGetProperty("currency", out var c) ? c.GetString() : "USD";
                    var total = first.TryGetProperty("total_balance", out var t) ? t.GetString() : "";
                    if (!string.IsNullOrEmpty(total))
                        balanceText = $"Saldo: {total} {curr}";
                }

                return new ConnectionTestResult
                {
                    Success = true,
                    Message = $"✓ Conexión exitosa ({balanceText})",
                    Detail = "Acceso verificado a la API de DeepSeek Platform."
                };
            }

            // Fallback to models check if balance endpoint is restricted
            var respModels = await client.GetAsync("https://api.deepseek.com/models");
            if (respModels.IsSuccessStatusCode)
            {
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = "✓ Conexión exitosa con DeepSeek",
                    Detail = "Modelos V3 y R1 listos."
                };
            }

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"✗ Error HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}",
                Detail = "Verifica que la clave sea válida en platform.deepseek.com"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Message = $"✗ Fallo de red: {ex.Message}" };
        }
    }

    public async Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
    {
        var snapshot = new TokenUsageSnapshot
        {
            ProviderId = Id,
            ProviderName = DisplayName,
            IconGlyph = IconGlyph,
            AccentColorHex = AccentColorHex,
            MaxContextTokens = 64_000
        };

        if (string.IsNullOrWhiteSpace(settings.DeepSeekApiKey))
        {
            snapshot.ConversationTitle = "Configura tu API Key de DeepSeek en Proveedores";
            return snapshot;
        }

        var test = await TestConnectionAsync(settings.DeepSeekApiKey);
        snapshot.ConversationTitle = test.Message;
        snapshot.IsActiveNow = test.Success;
        return snapshot;
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

public class OllamaProvider : ITokenProvider
{
    public string Id => "ollama";
    public string DisplayName => "Ollama (Servidor Local)";
    public string Description => "Monitoreo de modelos LLM ejecutándose localmente";
    public string IconGlyph => "🦙";
    public string ShortCode => "OLM";
    public string AccentColorHex => "#F59E0B"; // Amber

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.OllamaBaseUrl);

    public async Task<ConnectionTestResult> TestConnectionAsync(string baseUrl)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:11434" : baseUrl.Trim().TrimEnd('/');
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var resp = await client.GetAsync($"{url}/api/tags");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                int count = doc.RootElement.TryGetProperty("models", out var arr) ? arr.GetArrayLength() : 0;
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = $"✓ Conectado a Ollama ({count} modelos locales instalados)",
                    Detail = $"Servidor activo en {url}"
                };
            }

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"✗ Servidor respondió con estado {(int)resp.StatusCode}",
                Detail = "Asegúrate de que 'ollama serve' esté en ejecución."
            };
        }
        catch
        {
            return new ConnectionTestResult
            {
                Success = false,
                Message = $"✗ No se pudo conectar a {url}",
                Detail = "Verifica que Ollama esté iniciado localmente."
            };
        }
    }

    public async Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
    {
        var snapshot = new TokenUsageSnapshot
        {
            ProviderId = Id,
            ProviderName = DisplayName,
            IconGlyph = IconGlyph,
            AccentColorHex = AccentColorHex,
            MaxContextTokens = 32_000
        };

        var test = await TestConnectionAsync(settings.OllamaBaseUrl);
        snapshot.ConversationTitle = test.Message;
        snapshot.IsActiveNow = test.Success;
        return snapshot;
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}
