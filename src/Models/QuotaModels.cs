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

    public string FormattedCountdown
    {
        get
        {
            if (ResetTime == null) return "Listo";
            var diff = ResetTime.Value.ToLocalTime() - DateTime.Now;
            if (diff.TotalSeconds <= 0) return "Listo";
            if (diff.TotalDays >= 1)
            {
                return $"{(int)diff.TotalDays}d {diff.Hours}h";
            }
            if (diff.TotalHours >= 1)
            {
                return $"{(int)diff.TotalHours}h {diff.Minutes:D2}m";
            }
            return $"{diff.Minutes}m";
        }
    }

    public string FormattedRemainingCountdown
    {
        get
        {
            if (ResetTime == null) return "Listo";
            var diff = ResetTime.Value.ToLocalTime() - DateTime.Now;
            if (diff.TotalSeconds <= 0) return "Listo";
            if (diff.TotalDays >= 1)
            {
                return $"Quedan {(int)diff.TotalDays}d {diff.Hours}h";
            }
            if (diff.TotalHours >= 1)
            {
                return $"Quedan {(int)diff.TotalHours}h {diff.Minutes:D2}m";
            }
            return $"Quedan {diff.Minutes}m";
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

    public string WeeklyCountdown => WeeklyBucket?.FormattedCountdown ?? "Listo";
    public string FiveHourCountdown => FiveHourBucket?.FormattedCountdown ?? "Listo";
    public string WeeklyRemainingCountdown => WeeklyBucket?.FormattedRemainingCountdown ?? "Listo";
    public string FiveHourRemainingCountdown => FiveHourBucket?.FormattedRemainingCountdown ?? "Listo";
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
}
