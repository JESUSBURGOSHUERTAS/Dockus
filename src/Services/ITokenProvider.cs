using System.Collections.Generic;
using System.Threading.Tasks;
using Dockus.Models;

namespace Dockus.Services;

public interface ITokenProvider
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string IconGlyph { get; }
    string AccentColorHex { get; }
    bool IsConfigured(AppSettings settings);

    Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings);
    Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5);
}

