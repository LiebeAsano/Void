using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace VoidTemplate.MenuTinkery;

public sealed class LeaderTableResponse
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("slugcat")]
    public string Slugcat { get; set; }

    [JsonProperty("entries")]
    public List<LeaderTableEntry> Entries { get; set; } = [];
}

public sealed class LeaderTableEntry
{
    [JsonProperty("rank")]
    public int? Rank { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("time")]
    public string Time { get; set; }

    [JsonProperty("score")]
    public string Score { get; set; }

    [JsonProperty("cycles")]
    public string Cycles { get; set; }

    [JsonProperty("deaths")]
    public string Deaths { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalData { get; set; }

    public string Get(string canonicalName, params string[] aliases)
    {
        string value = GetKnown(canonicalName);
        if (value != null) return value;

        if (AdditionalData != null)
        {
            foreach (string alias in aliases)
            {
                if (AdditionalData.TryGetValue(alias, out JToken token) && token.Type != JTokenType.Null)
                    return token.Type == JTokenType.String ? token.ToString() : token.ToString(Formatting.None);
            }
        }
        return null;
    }

    private string GetKnown(string name)
    {
        return name switch
        {
            "rank" => Rank?.ToString(CultureInfo.InvariantCulture),
            "name" => Name,
            "time" => Time,
            "score" => Score,
            "cycles" => Cycles,
            "deaths" => Deaths,
            _ => null
        };
    }
}

public sealed class LeaderTableCacheFile
{
    public int Version { get; set; } = 1;
    public Dictionary<string, LeaderTableCacheEntry> Tables { get; set; } = [];
}

public sealed class LeaderTableCacheEntry
{
    public string Slugcat { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime LastCheckedAtUtc { get; set; }
    public string ETag { get; set; }
    public LeaderTableResponse Data { get; set; }
}

public sealed class LeaderTableFetchResult
{
    public LeaderTableResponse Data { get; set; }
    public string ETag { get; set; }
    public bool NotModified { get; set; }
}

public sealed class PlayerResultDto
{
    [JsonProperty("slugcat")]
    public string Slugcat { get; set; }

    [JsonProperty("player")]
    public string Player { get; set; }

    [JsonProperty("score")]
    public int Score { get; set; }

    [JsonProperty("time")]
    public int Time { get; set; }

    [JsonProperty("cycles")]
    public int Cycles { get; set; }

    [JsonProperty("deaths")]
    public int Deaths { get; set; }

    [JsonProperty("steam_id")]
    public string SteamId { get; set; }

    [JsonProperty("run_token")]
    public string RunToken { get; set; }
}

public sealed class WriteDataResponse
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }
}

public sealed class StoryStartRequest
{
    [JsonProperty("slugcat")]
    public string Slugcat { get; set; }

    [JsonProperty("steam_id")]
    public string SteamId { get; set; }

    [JsonProperty("client_nonce")]
    public string ClientNonce { get; set; }
}

public sealed class StoryStartResponse
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }
}

public enum LeaderTableUiState
{
    Cached,
    Loading,
    Refreshing,
    Loaded,
    Empty,
    NetworkError,
    ServerError,
    Submitting,
    SubmitSuccess,
    SubmitError
}
