using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dockus.Models;

namespace Dockus.Services;

public class GoogleGeminiApiProvider : ITokenProvider
{
    public string Id => "gemini_api";
    public string DisplayName => "Gemini API (Google AI Studio)";
    public string Description => "API directa de Google AI Studio con clave API (Gemini 1.5/2.0/3.x)";
    public string IconGlyph => "✦";
    public string AccentColorHex => "#60A5FA"; // Blue

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.GeminiApiKey);

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
            snapshot.ConversationTitle = "Requiere API Key de Google AI Studio en Ajustes";
            return snapshot;
        }

        try
        {
            using var client = new HttpClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={settings.GeminiApiKey}";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                int modelCount = 0;
                if (doc.RootElement.TryGetProperty("models", out var modelsArr))
                {
                    modelCount = modelsArr.GetArrayLength();
                }
                snapshot.ConversationTitle = $"Google AI Studio Conectado ({modelCount} modelos disponibles)";
                snapshot.IsActiveNow = true;
                snapshot.MaxContextTokens = 1_000_000;
                snapshot.CurrentContextTokens = 0;
            }
            else
            {
                snapshot.ConversationTitle = $"Error API: {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            snapshot.ConversationTitle = $"Error de conexión: {ex.Message}";
        }

        return snapshot;
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5)
    {
        return Task.FromResult(new List<ConversationItem>());
    }
}

public class OpenAiProvider : ITokenProvider
{
    public string Id => "openai";
    public string DisplayName => "OpenAI (GPT-4o / o1)";
    public string Description => "Monitoreo de tokens y cuotas de OpenAI";
    public string IconGlyph => "⚙";
    public string AccentColorHex => "#10B981"; // Emerald green

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey);

    public Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
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
            snapshot.ConversationTitle = "Configura tu API Key de OpenAI en Ajustes";
            return Task.FromResult(snapshot);
        }

        snapshot.ConversationTitle = "OpenAI API Listo (GPT-4o 128k contexto)";
        return Task.FromResult(snapshot);
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

public class ClaudeProvider : ITokenProvider
{
    public string Id => "claude";
    public string DisplayName => "Anthropic Claude (3.5 / 3.7)";
    public string Description => "Monitoreo de ventana de contexto de Anthropic Claude";
    public string IconGlyph => "✻";
    public string AccentColorHex => "#D97706"; // Amber

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.ClaudeApiKey);

    public Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
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
            snapshot.ConversationTitle = "Configura tu API Key de Claude en Ajustes";
            return Task.FromResult(snapshot);
        }

        snapshot.ConversationTitle = "Claude 3.5 Sonnet Listo (200k contexto)";
        return Task.FromResult(snapshot);
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

public class DeepSeekProvider : ITokenProvider
{
    public string Id => "deepseek";
    public string DisplayName => "DeepSeek (V3 / R1)";
    public string Description => "Monitoreo de tokens de DeepSeek";
    public string IconGlyph => "🐋";
    public string AccentColorHex => "#3B82F6"; // Cobalt blue

    public bool IsConfigured(AppSettings settings) => !string.IsNullOrWhiteSpace(settings.DeepSeekApiKey);

    public Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
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
            snapshot.ConversationTitle = "Configura tu API Key de DeepSeek en Ajustes";
            return Task.FromResult(snapshot);
        }

        snapshot.ConversationTitle = "DeepSeek V3/R1 Listo (64k contexto)";
        return Task.FromResult(snapshot);
    }

    public Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5) => Task.FromResult(new List<ConversationItem>());
}

