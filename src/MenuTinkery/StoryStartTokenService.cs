using System;
using System.Threading;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public static class StoryStartTokenService
{
    private static readonly object sync = new();
    private static Task<string> currentRequest;

    public static string CurrentToken { get; private set; }

    public static Task<string> CurrentRequest
    {
        get
        {
            lock (sync) return currentRequest;
        }
    }

    public static Task<string> GetActiveTokenAsync()
    {
        lock (sync)
        {
            if (!string.IsNullOrEmpty(CurrentToken)) return Task.FromResult(CurrentToken);
            if (currentRequest != null) return currentRequest;
        }
        return Task.FromException<string>(new InvalidOperationException("No active story token"));
    }

    public static Task<string> RequestAsync(SlugcatStats.Name slugcat, CancellationToken cancellationToken = default)
    {
        if (!StoryStartClientIdentity.TryGetSteamId(out string steamId))
            return Task.FromException<string>(new InvalidOperationException("Steam ID is unavailable"));

        var request = new StoryStartRequest
        {
            Slugcat = SlugcatApiNameMapper.GetApiName(slugcat),
            SteamId = steamId,
            ClientNonce = Guid.NewGuid().ToString("N")
        };

        Task<string> tokenRequest = RequestCoreAsync(request, cancellationToken);
        lock (sync) currentRequest = tokenRequest;
        return tokenRequest;
    }

    private static async Task<string> RequestCoreAsync(StoryStartRequest request, CancellationToken cancellationToken)
    {
        StoryStartResponse response = await new LeaderTableApiClient().StartStoryAsync(request, cancellationToken);
        lock (sync) CurrentToken = response.Token;
        Utils.LogExInf($"Story start token received: slugcat={request.Slugcat}");
        return response.Token;
    }
}
