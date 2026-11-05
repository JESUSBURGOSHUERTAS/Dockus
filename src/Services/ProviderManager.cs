using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dockus.Models;

namespace Dockus.Services;

public class ProviderManager
{
    private readonly Dictionary<string, ITokenProvider> _providers = new();

    public ProviderManager()
    {
        Register(new AntigravityGeminiProvider());
        Register(new GoogleGeminiApiProvider());
        Register(new OpenAiProvider());
        Register(new ClaudeProvider());
        Register(new DeepSeekProvider());
    }

    public void Register(ITokenProvider provider)
    {
        _providers[provider.Id] = provider;
    }

    public IEnumerable<ITokenProvider> GetAllProviders() => _providers.Values;

    public ITokenProvider GetProvider(string id)
    {
        if (_providers.TryGetValue(id, out var provider))
            return provider;
        return _providers["antigravity_gemini"];
    }

    public async Task<TokenUsageSnapshot> GetActiveUsageAsync(AppSettings settings)
    {
        var provider = GetProvider(settings.ActiveProviderId);
        return await provider.GetUsageAsync(settings);
    }

    public async Task<List<ConversationItem>> GetRecentConversationsAsync(string providerId, int count = 5)
    {
        var provider = GetProvider(providerId);
        return await provider.GetRecentConversationsAsync(count);
    }
}

