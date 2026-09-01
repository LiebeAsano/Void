using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoidTemplate.MenuTinkery;

public interface ILeaderTableApiClient
{
    Task<LeaderTableFetchResult> GetLeaderTableAsync(string slugcatApiName, string eTag, CancellationToken cancellationToken = default);
    Task<WriteDataResponse> SubmitPlayerDataAsync(PlayerResultDto request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<StoryStartResponse> StartStoryAsync(StoryStartRequest request, CancellationToken cancellationToken = default);
}

public sealed class LeaderTableApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public LeaderTableApiException(HttpStatusCode statusCode, string message) : base(message) => StatusCode = statusCode;
}

public sealed class LeaderTableApiClient : ILeaderTableApiClient
{
    private const string Endpoint = "https://flarya.me/api/leader-table";
    internal const string ClientKey = "fe08ed1761fa366d5576ebe5fabd32ad0704e5b4429390a47be6b7187d3058c8";
    private const long MaxResponseBytes = 1024 * 1024;
    private static readonly HttpClient httpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static void AuthorizeClient(HttpRequestMessage message) =>
        message.Headers.TryAddWithoutValidation("X-Leaderboard-Client-Key", ClientKey);

    public async Task<LeaderTableFetchResult> GetLeaderTableAsync(string slugcatApiName, string eTag, CancellationToken cancellationToken = default)
    {
        string url = Endpoint + "?slugcat=" + Uri.EscapeDataString(slugcatApiName);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(eTag)) request.Headers.TryAddWithoutValidation("If-None-Match", eTag);
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified)
            return new LeaderTableFetchResult { NotModified = true, ETag = eTag };

        if (response.Content.Headers.ContentLength > MaxResponseBytes)
            throw new LeaderTableApiException(response.StatusCode, "Leader table response exceeds the client size limit");

        string body = await response.Content.ReadAsStringAsync();
        if (Encoding.UTF8.GetByteCount(body) > MaxResponseBytes)
            throw new LeaderTableApiException(response.StatusCode, "Leader table response exceeds the client size limit");
        if (!response.IsSuccessStatusCode)
            throw new LeaderTableApiException(response.StatusCode, $"Leader table GET returned {(int)response.StatusCode}: {SafeError(body)}");
        if (string.IsNullOrWhiteSpace(body))
            throw new LeaderTableApiException(response.StatusCode, "Leader table GET returned an empty response");

        LeaderTableResponse data;
        try { data = JsonConvert.DeserializeObject<LeaderTableResponse>(body); }
        catch (JsonException exception) { throw new LeaderTableApiException(response.StatusCode, $"Leader table JSON is invalid: {exception.Message}"); }
        if (data == null || !data.Ok || data.Entries == null)
            throw new LeaderTableApiException(response.StatusCode, "Leader table response has an invalid schema");

        return new LeaderTableFetchResult { Data = data, ETag = response.Headers.ETag?.Tag };
    }

    public async Task<WriteDataResponse> SubmitPlayerDataAsync(PlayerResultDto request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
        };
        AuthorizeClient(message);
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new LeaderTableApiException(response.StatusCode, $"Leader table POST returned {(int)response.StatusCode}: {SafeError(body)}");
        try
        {
            WriteDataResponse result = JsonConvert.DeserializeObject<WriteDataResponse>(body);
            if (result == null || !result.Ok) throw new LeaderTableApiException(response.StatusCode, result?.Error ?? "Leader table POST was rejected");
            return result;
        }
        catch (JsonException exception) { throw new LeaderTableApiException(response.StatusCode, $"Leader table POST JSON is invalid: {exception.Message}"); }
    }

    public async Task<StoryStartResponse> StartStoryAsync(StoryStartRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://flarya.me/api/story-start")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
        };
        AuthorizeClient(message);
        using HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new LeaderTableApiException(response.StatusCode, $"Story start returned {(int)response.StatusCode}: {SafeError(body)}");

        try
        {
            StoryStartResponse result = JsonConvert.DeserializeObject<StoryStartResponse>(body);
            if (result == null || !result.Ok || string.IsNullOrWhiteSpace(result.Token))
                throw new LeaderTableApiException(response.StatusCode, result?.Error ?? "Story start token was rejected");
            return result;
        }
        catch (JsonException exception) { throw new LeaderTableApiException(response.StatusCode, $"Story start JSON is invalid: {exception.Message}"); }
    }

    private static string SafeError(string body)
    {
        if (string.IsNullOrEmpty(body)) return "empty response";
        return body.Length <= 512 ? body : body.Substring(0, 512);
    }
}
