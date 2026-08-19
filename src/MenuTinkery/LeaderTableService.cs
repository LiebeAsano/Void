using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public sealed class LeaderTableService
{
    public static readonly LeaderTableService Instance = new(new LeaderTableApiClient(), new LeaderTableCache());

    private readonly ILeaderTableApiClient apiClient;
    private readonly ILeaderTableCache cache;
    private readonly object sync = new();
    private readonly Dictionary<string, Task<LeaderTableResponse>> inFlightRequests = [];

    public LeaderTableService(ILeaderTableApiClient apiClient, ILeaderTableCache cache)
    {
        this.apiClient = apiClient;
        this.cache = cache;
        cache.Load();
    }

    public LeaderTableCacheEntry GetCached(string slugcatApiName) => cache.Get(slugcatApiName);

    public Task<LeaderTableResponse> RefreshAsync(string slugcatApiName, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (inFlightRequests.TryGetValue(slugcatApiName, out Task<LeaderTableResponse> request)) return request;
            Task<LeaderTableResponse> created = RefreshCoreAsync(slugcatApiName, CancellationToken.None);
            inFlightRequests.Add(slugcatApiName, created);
            _ = created.ContinueWith(_ =>
            {
                lock (sync) inFlightRequests.Remove(slugcatApiName);
            }, TaskScheduler.Default);
            return created;
        }
    }

    private async Task<LeaderTableResponse> RefreshCoreAsync(string slugcatApiName, CancellationToken cancellationToken)
    {
        LeaderTableCacheEntry old = cache.Get(slugcatApiName);
        Utils.LogExInf($"Leader table refresh started: slugcat={slugcatApiName}, cache={(old == null ? "miss" : "hit")}");
        LeaderTableFetchResult result = await apiClient.GetLeaderTableAsync(slugcatApiName, old?.ETag, cancellationToken);
        if (result.NotModified)
        {
            if (old?.Data == null) throw new InvalidOperationException("Server returned 304 without a local leader-table cache");
            cache.MarkChecked(slugcatApiName);
            _ = Task.Run(cache.Save);
            return old.Data;
        }

        cache.Set(slugcatApiName, result.Data, result.ETag);
        _ = Task.Run(cache.Save);
        Utils.LogExInf($"Leader table refresh succeeded: slugcat={slugcatApiName}, entries={result.Data.Entries.Count}");
        return result.Data;
    }
}
