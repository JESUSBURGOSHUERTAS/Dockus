using System;

namespace Dockus.Models;

public class TokenUsageSnapshot
{
    public string ProviderId { get; set; } = "antigravity_gemini";
    public string ProviderName { get; set; } = "Gemini (Antigravity)";
    public string IconGlyph { get; set; } = "✦";
    public string AccentColorHex { get; set; } = "#38BDF8";

    // Conversation metadata
    public string ConversationId { get; set; } = string.Empty;
    public string ConversationTitle { get; set; } = "Sin conversación activa";
    public int StepCount { get; set; }
    public DateTime LastActivityTime { get; set; } = DateTime.Now;
    public bool IsActiveNow { get; set; }

    // Active Context Window (What is currently in the model)
    public long CurrentContextTokens { get; set; }
    public long MaxContextTokens { get; set; } = 1_000_000;
    public long RemainingTokens => Math.Max(0, MaxContextTokens - CurrentContextTokens);
    public double ContextUsagePercentage
    {
        get => MaxContextTokens > 0 ? Math.Clamp(((double)CurrentContextTokens / MaxContextTokens) * 100.0, 0, 100) : 0;
        set { }
    }
    public double ContextRemainingPercentage => Math.Max(0, 100.0 - ContextUsagePercentage);

    // Latest turn stats
    public long LastTurnInputTokens { get; set; }
    public long LastTurnCacheTokens { get; set; }
    public long LastTurnOutputTokens { get; set; }

    // Session aggregates
    public long SessionTotalInputTokens { get; set; }
    public long SessionTotalCacheTokens { get; set; }
    public long SessionTotalOutputTokens { get; set; }
    public long SessionTotalTokens => SessionTotalInputTokens + SessionTotalCacheTokens + SessionTotalOutputTokens;
    public double SessionCacheHitRatio => (SessionTotalInputTokens + SessionTotalCacheTokens) > 0
        ? ((double)SessionTotalCacheTokens / (SessionTotalInputTokens + SessionTotalCacheTokens)) * 100.0
        : 0;

    // Today aggregates across all sessions
    public long TodayInputTokens { get; set; }
    public long TodayCacheTokens { get; set; }
    public long TodayOutputTokens { get; set; }
    public long TodayTotalTokens => TodayInputTokens + TodayCacheTokens + TodayOutputTokens;

    // Formatted strings for high-end UI
    public string FormattedCurrentContext => FormatNumber(CurrentContextTokens);
    public string FormattedMaxContext => FormatNumber(MaxContextTokens);
    public string FormattedRemaining => FormatNumber(RemainingTokens);
    public string FormattedTodayTotal => FormatNumber(TodayTotalTokens);
    public string FormattedSessionTotal => FormatNumber(SessionTotalTokens);
    public string FormattedCacheHit => $"{SessionCacheHitRatio:F1}%";
    public string FormattedPercentUsed => $"{ContextUsagePercentage:F1}%";
    public string FormattedPercentRemaining => $"{ContextRemainingPercentage:F1}%";

    public static string FormatNumber(long num)
    {
        if (num >= 1_000_000)
            return $"{(num / 1_000_000.0):F2}M";
        if (num >= 1_000)
            return $"{(num / 1_000.0):F1}k";
        return num.ToString("N0");
    }
}
