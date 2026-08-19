using System;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Menu;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public static class LeaderTableSubmission
{
    private static readonly ConditionalWeakTable<StoryGameStatisticsScreen, SubmissionState> states = new();

    public static async Task SubmitAsync(StoryGameStatisticsScreen screen)
    {
        SubmissionState state = states.GetValue(screen, _ => new SubmissionState());
        if (state.IsSubmitting) return;
        if (!TryBuildRequest(screen, out PlayerResultDto request, out string error))
        {
            Utils.LogExErr($"WRITE DATA was not sent: {error}");
            return;
        }

        state.IsSubmitting = true;
        try
        {
            string idempotencyKey = CreateIdempotencyKey(request);
            WriteDataResponse response = await new LeaderTableApiClient().SubmitPlayerDataAsync(request, idempotencyKey, CancellationToken.None);
            if (!response.Ok) throw new InvalidOperationException(response.Error ?? "Server rejected WRITE DATA");
            await LeaderTableService.Instance.RefreshAsync(request.Slugcat, CancellationToken.None);
            Utils.LogExInf($"WRITE DATA succeeded: slugcat={request.Slugcat}");
        }
        catch (Exception exception)
        {
            Utils.LogExErr($"WRITE DATA failed: {exception.Message}");
        }
        finally
        {
            state.IsSubmitting = false;
        }
    }

    private static bool TryBuildRequest(StoryGameStatisticsScreen screen, out PlayerResultDto request, out string error)
    {
        request = null;
        error = null;
        if (screen?.saveState == null)
        {
            error = "Save state is unavailable";
            return false;
        }

        if (!TryReadInt(screen, ["score", "Score", "finalScore", "FinalScore", "rating", "Rating"], out int score)
            || !TryReadInt(screen, ["time", "Time", "totalTime", "TotalTime", "gameTime", "GameTime"], out int time)
            || !TryReadInt(screen, ["deaths", "Deaths", "deathCount", "DeathCount"], out int deaths))
        {
            error = "Statistics screen does not expose score, time, or deaths";
            return false;
        }

        string player = "TestPlayer";
        if (string.IsNullOrWhiteSpace(player))
        {
            error = "Player name is unavailable";
            return false;
        }

        request = new PlayerResultDto
        {
            Slugcat = SlugcatApiNameMapper.GetApiName(screen.saveState.saveStateNumber),
            Player = player.Trim(),
            Score = score,
            Time = time,
            Cycles = screen.saveState.cycleNumber,
            Deaths = deaths
        };
        return true;
    }

    private static bool TryReadInt(object source, string[] names, out int value)
    {
        object raw = ReadMember(source, names);
        if (raw is null)
        {
            value = 0;
            return false;
        }
        if (raw is TimeSpan timeSpan)
        {
            value = (int)Math.Min(int.MaxValue, Math.Max(0, timeSpan.TotalSeconds));
            return true;
        }
        if (raw is IConvertible convertible)
        {
            try
            {
                value = Convert.ToInt32(convertible, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
            }
        }
        return int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static string ReadString(object source, string[] names) => ReadMember(source, names)?.ToString();

    private static object ReadMember(object source, string[] names)
    {
        for (Type type = source.GetType(); type != null; type = type.BaseType)
        {
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(source);
                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property?.CanRead == true) return property.GetValue(source, null);
            }
        }
        return null;
    }

    private static string CreateIdempotencyKey(PlayerResultDto request)
    {
        string material = string.Join("|", request.Slugcat, request.Player, request.Score, request.Time, request.Cycles, request.Deaths);
        using SHA256 hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(material))).Replace("-", string.Empty).ToLowerInvariant();
    }

    private sealed class SubmissionState
    {
        public bool IsSubmitting;
    }
}
