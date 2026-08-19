using Menu;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public static class LeaderTableMenuSyncHooks
{
    private static readonly ConditionalWeakTable<LeaderTableMenu, SyncState> states = new();

    public static void Hook()
    {
        On.Menu.Menu.Update += Menu_Update;
    }

    private static void Menu_Update(On.Menu.Menu.orig_Update orig, Menu.Menu self)
    {
        orig(self);
        if (self is not LeaderTableMenu menu) return;

        SyncState state = states.GetValue(menu, CreateState);
        if (!state.CachedTablesSeeded)
        {
            SeedCachedTables(menu);
            state.CachedTablesSeeded = true;
        }
        string slugcatApiName = SlugcatApiNameMapper.GetApiName(menu.slugcatSelect.CurrentSlug);
        if (state.CurrentSlugcatApiName != slugcatApiName)
        {
            bool loadCurrentTable = !state.InitialSelectionHandled && !HasCachedTable(menu.slugcatSelect.CurrentSlug);
            state.CurrentSlugcatApiName = slugcatApiName;
            state.RequestVersion++;
            ApplyCachedTable(menu, state, slugcatApiName, loadCurrentTable);
            state.Pending.Add(new PendingRefresh
            {
                SlugcatApiName = slugcatApiName,
                Version = state.RequestVersion,
                LoadCurrentTable = loadCurrentTable,
                Request = LeaderTableService.Instance.RefreshAsync(slugcatApiName, CancellationToken.None)
            });
            state.InitialSelectionHandled = true;
        }

        ProcessCompletedRequests(menu, state);
    }

    private static SyncState CreateState(LeaderTableMenu menu)
    {
        return new SyncState();
    }

    private static void ApplyCachedTable(LeaderTableMenu menu, SyncState state, string slugcatApiName, bool loadCurrentTable)
    {
        LeaderTableCacheEntry cached = LeaderTableService.Instance.GetCached(slugcatApiName);
        if (cached?.Data == null) return;

        StoreTable(menu, menu.slugcatSelect.CurrentSlug, cached.Data, loadCurrentTable);
    }

    private static void ProcessCompletedRequests(LeaderTableMenu menu, SyncState state)
    {
        for (int i = state.Pending.Count - 1; i >= 0; i--)
        {
            PendingRefresh pending = state.Pending[i];
            if (!pending.Request.IsCompleted) continue;
            state.Pending.RemoveAt(i);

            bool currentRequest = pending.Version == state.RequestVersion && pending.SlugcatApiName == state.CurrentSlugcatApiName;
            if (pending.Request.IsCanceled)
                continue;

            if (pending.Request.IsFaulted)
            {
                Exception exception = pending.Request.Exception?.GetBaseException();
                Utils.LogExErr($"Leader table refresh failed: {exception?.Message}");
                continue;
            }

            if (!currentRequest) continue;
            LeaderTableResponse response = pending.Request.GetAwaiter().GetResult();
            StoreTable(menu, menu.slugcatSelect.CurrentSlug, response, pending.LoadCurrentTable);
        }
    }

    public static void SeedCachedTables(LeaderTableMenu menu)
    {
        foreach (SlugcatStats.Name slugcat in menu.slugcatSelect.slugcats)
        {
            LeaderTableCacheEntry cached = LeaderTableService.Instance.GetCached(SlugcatApiNameMapper.GetApiName(slugcat));
            if (cached?.Data != null) StoreTable(menu, slugcat, cached.Data, false);
        }
    }

    private static bool HasCachedTable(SlugcatStats.Name slugcat)
    {
        return LeaderTableService.Instance.GetCached(SlugcatApiNameMapper.GetApiName(slugcat))?.Data != null;
    }

    private static void StoreTable(LeaderTableMenu menu, SlugcatStats.Name slugcat, LeaderTableResponse response, bool loadCurrentTable)
    {
        int rowCount = Math.Max(11, response.Entries.Count);
        object[,] rows = new object[6, rowCount];
        for (int y = 0; y < rowCount; y++)
        {
            for (int x = 0; x < rows.GetLength(0); x++)
                rows[x, y] = "—";
        }
        for (int i = 0; i < response.Entries.Count; i++)
        {
            LeaderTableEntry entry = response.Entries[i];
            rows[0, i] = entry.Get("rank", "place", "position") ?? (i + 1).ToString();
            rows[1, i] = entry.Get("name", "player", "username", "playerName", "displayName") ?? "—";
            rows[2, i] = entry.Get("time", "duration", "timeMs") ?? "—";
            rows[3, i] = entry.Get("score", "points") ?? "—";
            rows[4, i] = entry.Get("cycles", "cycleCount") ?? "—";
            rows[5, i] = entry.Get("deaths", "deathCount") ?? "—";
        }
        menu.cashedTableData[slugcat] = rows;
        if (loadCurrentTable) menu.table.LoadSlugcatTable(rows);
    }

    private sealed class SyncState
    {
        public string CurrentSlugcatApiName;
        public int RequestVersion;
        public bool CachedTablesSeeded;
        public bool InitialSelectionHandled;
        public List<PendingRefresh> Pending = [];
    }

    private sealed class PendingRefresh
    {
        public string SlugcatApiName;
        public int Version;
        public bool LoadCurrentTable;
        public Task<LeaderTableResponse> Request;
    }
}
