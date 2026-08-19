using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public interface ILeaderTableCache
{
    LeaderTableCacheEntry Get(string slugcatApiName);
    void Set(string slugcatApiName, LeaderTableResponse response, string eTag);
    void MarkChecked(string slugcatApiName);
    void Load();
    void Save();
}

public sealed class LeaderTableCache : ILeaderTableCache
{
    private const int CurrentVersion = 1;
    private readonly object sync = new();
    private readonly string path;
    private readonly JsonSerializerSettings settings = new()
    {
        Error = (sender, args) => args.ErrorContext.Handled = true
    };
    private LeaderTableCacheFile cacheFile = new();
    private bool loaded;

    public LeaderTableCache()
    {
        string directory = Path.Combine(RWCustom.Custom.RootFolderDirectory(), "modsavedata", "lastwish");
        path = Path.Combine(directory, "leader-table-cache.json");
    }

    public LeaderTableCacheEntry Get(string slugcatApiName)
    {
        lock (sync)
            return cacheFile.Tables.TryGetValue(slugcatApiName, out LeaderTableCacheEntry entry) ? entry : null;
    }

    public void Set(string slugcatApiName, LeaderTableResponse response, string eTag)
    {
        if (response == null || !response.Ok || response.Entries == null) return;
        lock (sync)
        {
            cacheFile.Tables[slugcatApiName] = new LeaderTableCacheEntry
            {
                Slugcat = slugcatApiName,
                UpdatedAtUtc = DateTime.UtcNow,
                LastCheckedAtUtc = DateTime.UtcNow,
                ETag = eTag,
                Data = response
            };
        }
    }

    public void MarkChecked(string slugcatApiName)
    {
        lock (sync)
        {
            if (cacheFile.Tables.TryGetValue(slugcatApiName, out LeaderTableCacheEntry entry))
                entry.LastCheckedAtUtc = DateTime.UtcNow;
        }
    }

    public void Load()
    {
        lock (sync)
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(path)) return;
                LeaderTableCacheFile read = JsonConvert.DeserializeObject<LeaderTableCacheFile>(File.ReadAllText(path), settings);
                if (read == null || read.Version != CurrentVersion || read.Tables == null) return;
                cacheFile = read;
            }
            catch (Exception exception)
            {
                Utils.LogExErr($"Could not read leader-table cache: {exception.Message}");
            }
        }
    }

    public void Save()
    {
        lock (sync)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(cacheFile, Formatting.None));
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            catch (Exception exception)
            {
                Utils.LogExErr($"Could not save leader-table cache: {exception.Message}");
            }
        }
    }
}
