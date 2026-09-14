using Newtonsoft.Json;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VoidTemplate;

internal static class CampaignStatisticsStore
{
    internal enum ReadStatus { Missing, Ready, Error }

    internal sealed class Snapshot
    {
        [JsonProperty(Required = Required.Always)] public int Version { get; set; } = 1;
        [JsonProperty(Required = Required.Always)] public int SaveSlot { get; set; }
        [JsonProperty(Required = Required.Always)] public string Campaign { get; set; }
        [JsonProperty(Required = Required.Always)] public bool Cleared { get; set; }
        [JsonProperty(Required = Required.Always)] public bool ForceStatistics { get; set; }
        [JsonProperty(Required = Required.Always)] public string SaveData { get; set; }
        [JsonProperty(Required = Required.Always)] public string Checksum { get; set; }
        public string SavedAtUtc { get; set; }
    }

    private static readonly object fileLock = new();
    private static readonly JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.None };
    private static string Root => Path.Combine(Custom.RootFolderDirectory(), "modsavedata", "lastwish");

    internal static string GetPath(int saveSlot, string campaign)
    {
        return Path.Combine(Root, "finalstatistics", saveSlot.ToString(CultureInfo.InvariantCulture), Hash(campaign) + ".json");
    }

    internal static ReadStatus Read(int saveSlot, SlugcatStats.Name campaign, out Snapshot snapshot, out string error)
    {
        snapshot = null;
        error = null;

        if (saveSlot < 0 || campaign == null || string.IsNullOrEmpty(campaign.value))
            return ReadStatus.Missing;

        lock (fileLock)
        {
            try
            {
                string path = GetPath(saveSlot, campaign.value);

                if (File.Exists(path))
                {
                    snapshot = JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(path), settings);
                    Validate(snapshot, saveSlot, campaign.value);
                    return snapshot.Cleared ? ReadStatus.Missing : ReadStatus.Ready;
                }

                if (campaign != VoidEnums.SlugcatID.Void)
                    return ReadStatus.Missing;

                string legacyPath = Path.Combine(Root, "voidfinalstatistics.json");
                if (!File.Exists(legacyPath)) return ReadStatus.Missing;

                var legacy = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(legacyPath), settings) ?? throw new InvalidDataException("The legacy statistics file is empty or invalid.");
                if (!legacy.TryGetValue(saveSlot, out string data) || string.IsNullOrEmpty(data))
                    return ReadStatus.Missing;

                snapshot = Create(saveSlot, campaign.value, data, true, false);
                Validate(snapshot, saveSlot, campaign.value);
                WriteFile(path, snapshot);
                return ReadStatus.Ready;
            }
            catch (Exception exception)
            {
                snapshot = null;
                error = exception.ToString();
                return ReadStatus.Error;
            }
        }
    }

    internal static bool Write(int saveSlot, SlugcatStats.Name campaign, string data, bool forceStatistics, out string error)
    {
        return WriteEntry(saveSlot, campaign, data, forceStatistics, false, out error);
    }

    internal static bool Clear(int saveSlot, SlugcatStats.Name campaign, out string error)
    {
        return WriteEntry(saveSlot, campaign, string.Empty, false, true, out error);
    }

    private static bool WriteEntry(int saveSlot, SlugcatStats.Name campaign, string data, bool forceStatistics, bool cleared, out string error)
    {
        error = null;

        if (saveSlot < 0 || campaign == null || string.IsNullOrEmpty(campaign.value))
        {
            error = "Invalid story save slot or campaign ID.";
            return false;
        }

        lock (fileLock)
        {
            try
            {
                Snapshot snapshot = Create(saveSlot, campaign.value, data, forceStatistics, cleared);
                Validate(snapshot, saveSlot, campaign.value);
                string path = GetPath(saveSlot, campaign.value);
                PreservePreviousResult(path, snapshot);
                WriteFile(path, snapshot);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                return false;
            }
        }
    }

    private static void PreservePreviousResult(string path, Snapshot replacement)
    {
        if (!File.Exists(path)) return;

        string previousText = File.ReadAllText(path);
        Snapshot previous = JsonConvert.DeserializeObject<Snapshot>(previousText, settings);
        Validate(previous, replacement.SaveSlot, replacement.Campaign);

        if (previous.Cleared || !replacement.Cleared &&
            previous.Checksum == replacement.Checksum && previous.ForceStatistics == replacement.ForceStatistics)
            return;

        string folder = Path.Combine(Path.GetDirectoryName(path), "history", Hash(replacement.Campaign));
        Directory.CreateDirectory(folder);
        string name = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ", CultureInfo.InvariantCulture) +
            "_" + Guid.NewGuid().ToString("N") + ".json";
        string archivePath = Path.Combine(folder, name);

        byte[] bytes = new UTF8Encoding(false).GetBytes(previousText);
        using FileStream stream = new(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(true);
    }

    private static Snapshot Create(int saveSlot, string campaign, string data, bool forceStatistics, bool cleared)
    {
        return new Snapshot
        {
            SaveSlot = saveSlot,
            Campaign = campaign,
            Cleared = cleared,
            ForceStatistics = forceStatistics,
            SaveData = data,
            Checksum = Hash(data ?? string.Empty),
            SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private static void Validate(Snapshot snapshot, int saveSlot, string campaign)
    {
        if (snapshot == null || snapshot.Version != 1 || snapshot.SaveSlot != saveSlot ||
            !string.Equals(snapshot.Campaign, campaign, StringComparison.Ordinal))
            throw new InvalidDataException("Statistics version, slot or campaign ID does not match.");

        if (snapshot.SaveData == null || !string.Equals(snapshot.Checksum, Hash(snapshot.SaveData), StringComparison.Ordinal))
            throw new InvalidDataException("Statistics checksum does not match.");

        if (snapshot.Cleared)
        {
            if (snapshot.SaveData.Length != 0 || snapshot.ForceStatistics)
                throw new InvalidDataException("Invalid cleared statistics entry.");
            return;
        }

        const string marker = "SAV STATE NUMBER<svB>";
        string savedCampaign = null;

        foreach (string field in snapshot.SaveData.Split(["<svA>"], StringSplitOptions.None))
        {
            if (!field.StartsWith(marker, StringComparison.Ordinal)) continue;
            if (savedCampaign != null) throw new InvalidDataException("Duplicate campaign field in statistics.");
            savedCampaign = field.Substring(marker.Length);
        }

        if (!string.Equals(savedCampaign, campaign, StringComparison.Ordinal))
            throw new InvalidDataException("The serialized SaveState belongs to another campaign or is empty.");
    }

    private static void WriteFile(string path, Snapshot snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        string text = JsonConvert.SerializeObject(snapshot, Formatting.Indented, settings);

        try
        {
            using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            Snapshot written = JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(temporaryPath), settings);
            Validate(written, snapshot.SaveSlot, snapshot.Campaign);

            if (File.Exists(path)) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try { File.Delete(temporaryPath); }
                catch {  }
            }
        }
    }

    private static string Hash(string value)
    {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }
}
