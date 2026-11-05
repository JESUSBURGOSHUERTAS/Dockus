using System;
using System.Collections.Generic;
using System.Linq;

namespace Dockus.Models;

public class QuotaBucket
{
    public string BucketId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Window { get; set; } = string.Empty; // "weekly", "5h"
    public double RemainingFraction { get; set; } = 1.0;
    public double RemainingPercentage => Math.Clamp(RemainingFraction * 100.0, 0, 100);
    public double UsedPercentage => Math.Max(0, 100.0 - RemainingPercentage);
    public DateTime? ResetTime { get; set; }

    public string FormattedRemaining => $"{RemainingPercentage:F0}%";
    public string FormattedUsed => $"{UsedPercentage:F0}%";

    public string FormattedReset
    {
        get
        {
            if (ResetTime == null) return string.Empty;
            var diff = ResetTime.Value.ToLocalTime() - DateTime.Now;
            if (diff.TotalMinutes <= 0) return "En breve";
            if (diff.TotalDays >= 1) return $"{(int)diff.TotalDays}d {diff.Hours}h";
            if (diff.TotalHours >= 1) return $"{(int)diff.TotalHours}h {diff.Minutes}m";
            return $"{diff.Minutes}m";
        }
    }
}

public class QuotaGroup
{
    public string DisplayName { get; set; } = string.Empty; // "Gemini Models", "Claude and GPT models", "Gemini 4 Argon"
    public string Description { get; set; } = string.Empty;
    public List<QuotaBucket> Buckets { get; set; } = new();

    public QuotaBucket? WeeklyBucket => Buckets.FirstOrDefault(b => b.Window.Equals("weekly", StringComparison.OrdinalIgnoreCase) || b.DisplayName.Contains("Weekly", StringComparison.OrdinalIgnoreCase));
    public QuotaBucket? FiveHourBucket => Buckets.FirstOrDefault(b => b.Window.Equals("5h", StringComparison.OrdinalIgnoreCase) || b.DisplayName.Contains("5", StringComparison.OrdinalIgnoreCase) || b.DisplayName.Contains("Five", StringComparison.OrdinalIgnoreCase));

    public string FormattedWeeklyRemaining => WeeklyBucket?.FormattedRemaining ?? "100%";
    public string FormattedFiveHourRemaining => FiveHourBucket?.FormattedRemaining ?? "100%";
    public double WeeklyRemainingPercentage => WeeklyBucket?.RemainingPercentage ?? 100.0;
    public double FiveHourRemainingPercentage => FiveHourBucket?.RemainingPercentage ?? 100.0;
}

public class AntigravityQuotaSummary
{
    public List<QuotaGroup> Groups { get; set; } = new();
    public string OverallDescription { get; set; } = string.Empty;
    public DateTime LastFetched { get; set; } = DateTime.Now;
    public bool IsConnected { get; set; }

    public QuotaGroup? GeminiGroup => Groups.FirstOrDefault(g => g.DisplayName.Contains("Gemini Models", StringComparison.OrdinalIgnoreCase) || g.DisplayName.StartsWith("Gemini", StringComparison.OrdinalIgnoreCase));
    public QuotaGroup? ClaudeGptGroup => Groups.FirstOrDefault(g => g.DisplayName.Contains("Claude", StringComparison.OrdinalIgnoreCase));
    public QuotaGroup? ArgonGroup => Groups.FirstOrDefault(g => g.DisplayName.Contains("Argon", StringComparison.OrdinalIgnoreCase));

    // Summary strings for Compact Bar
    public string CompactGeminiWeekly => GeminiGroup?.WeeklyBucket != null ? $"{GeminiGroup.WeeklyBucket.FormattedRemaining} Gemini" : "100% Gemini";
    public string CompactFiveHour => GeminiGroup?.FiveHourBucket != null ? $"{GeminiGroup.FiveHourBucket.FormattedRemaining} (5h)" : "100% (5h)";
    public string CompactClaudeGpt => ClaudeGptGroup?.WeeklyBucket != null ? $"{ClaudeGptGroup.WeeklyBucket.FormattedRemaining} Claude" : "100% Claude";
    public double CompactGeminiPercentage => GeminiGroup?.WeeklyBucket?.RemainingPercentage ?? 100.0;
}

