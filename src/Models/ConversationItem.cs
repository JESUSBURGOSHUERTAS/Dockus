using System;

namespace Dockus.Models;

public class ConversationItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "Conversación";
    public int StepCount { get; set; }
    public DateTime LastModified { get; set; }
    public long ContextTokens { get; set; }
    public bool IsCurrent { get; set; }

    public string FormattedContext => TokenUsageSnapshot.FormatNumber(ContextTokens);
    public string FormattedRelativeTime
    {
        get
        {
            var diff = DateTime.Now - LastModified;
            if (diff.TotalMinutes < 1) return "Justo ahora";
            if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes}m";
            if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours}h";
            return LastModified.ToString("dd/MM HH:mm");
        }
    }
}

