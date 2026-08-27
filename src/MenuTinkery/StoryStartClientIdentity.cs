using System;
using System.Reflection;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public static class StoryStartClientIdentity
{
    private static readonly object sync = new();
    private static string steamId;

    public static bool TryGetSteamId(out string result)
    {
        lock (sync)
        {
            if (!string.IsNullOrEmpty(steamId))
            {
                result = steamId;
                return true;
            }

            try
            {
                Type steamUserType = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    steamUserType = assembly.GetType("Steamworks.SteamUser", false);
                    if (steamUserType != null) break;
                }

                MethodInfo getSteamId = steamUserType?.GetMethod("GetSteamID", BindingFlags.Public | BindingFlags.Static);
                object value = getSteamId?.Invoke(null, null);
                string candidate = value?.ToString();
                if (!string.IsNullOrWhiteSpace(candidate) && ulong.TryParse(candidate, out _))
                {
                    steamId = candidate;
                    result = steamId;
                    return true;
                }
            }
            catch (Exception exception)
            {
                Utils.LogExErr($"Could not read Steam ID: {exception.Message}");
            }

            result = null;
            return false;
        }
    }
}
