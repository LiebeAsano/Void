global using static VoidTemplate.Useful.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Menu;
using UnityEngine;
using SlugBase.Features;

namespace VoidTemplate.Useful;
public static class Utils
{
	public const string ModID = "rainworldlastwish";
	public const int TicksPerSecond = 40;
	public static void loginf(object e) => _Plugin.logger.LogInfo(e);
	public static void logerr(object e) => _Plugin.logger.LogError(e);
	public static string TranslateStringComplex(this string str) => RWCustom.Custom.rainWorld.inGameTranslator.Translate(str).Replace("<LINE>", "\n");
	public static string TranslateString(this string str) => RWCustom.Custom.rainWorld.inGameTranslator.Translate(str);
    
    public static bool IsVoid(this Player p) => p.SlugCatClass == VoidEnums.SlugcatID.Void;
    public static bool IsViy(this Player p) => p.SlugCatClass == VoidEnums.SlugcatID.Viy;
    public static bool AreVoidViy(this Player p) => p.SlugCatClass == VoidEnums.SlugcatID.Void || p.SlugCatClass == VoidEnums.SlugcatID.Viy;
    public static bool IsVoidWorld(this RainWorldGame game) => game.StoryCharacter == VoidEnums.SlugcatID.Void;
	public static bool IsVoidStoryCampaign(this RainWorldGame game) => (game.IsVoidWorld()
			&& !(ModManager.Expedition && game.rainWorld.ExpeditionMode));
	public static bool IsViyWorld(this RainWorldGame game) => game.StoryCharacter == VoidEnums.SlugcatID.Viy;
    public static bool IsViyStoryCampaign(this RainWorldGame game) => (game.IsViyWorld()
            && !(ModManager.Expedition && game.rainWorld.ExpeditionMode));
    public static bool KarmaKapCheck(this Player p, int karmaRequirement) => p.KarmaCap >= karmaRequirement;

    public static Color[] VoidColors = new Color[32];

    public static Color[] ViyColors = new Color[32];

    //stolen with permission from Henpemaz' https://github.com/henpemaz/Rain-Meadow/blob/main/RainMeadow.Logging.cs
	private static string LogTime() { return ((int)(Time.time * 1000)).ToString(); }
	private static string LogDOT() { return DateTime.Now.ToUniversalTime().TimeOfDay.ToString().Substring(0, 8); }
	public static void LogExInf(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
	{
		loginf($"{LogDOT()}|{LogTime()}|{callerFile}.{callerName}:\n{data}");
	}
	public static void LogExErr(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
	{
		logerr($"{LogDOT()}|{LogTime()}|{callerFile}.{callerName}:\n{data}");
	}

    private static bool? dressMySlugcatEnabled = null;
    public static bool DressMySlugcatEnabled
    {
        get
        {
            dressMySlugcatEnabled ??= ModManager.ActiveMods.Exists(mod => mod.id == "dressmyslugcat");
            return (bool)dressMySlugcatEnabled;
        }
    }
}
public static class POMUtils
{
    public static Vector2[] AddRealPosition(Vector2[] Polygon, Vector2 pos)
    {
        if (Polygon == null) return null;
        Vector2[] result = new Vector2[Polygon.Length];
        for (int i = 0; i < Polygon.Length; i++)
        { result[i] = Polygon[i] + pos; }
        return result;
    }

    public static bool PositionWithinPoly(Vector2[] Polygon, Vector2 point)
    {
        if (Polygon == null) return false;
        bool result = true;
        for (int i = 0; i < Polygon.Length; i++)
        {
            if (IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], point)
                != IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], Polygon[(i + 2) % Polygon.Length])) result = false;
        }
        return result;
    }
    private static bool IsAboveEquationByTwoPoints(Vector2 point1, Vector2 point2, Vector2 v)
    {
        bool isAboveLine = (point1.x - v.x) * (point2.y - point1.y) <= (point1.y - v.y) * (point2.x - point1.x);
        return isAboveLine;
    }

    public static Pom.Pom.Vector2ArrayField defaultVectorField => new Pom.Pom.Vector2ArrayField("trigger zone", 4, true, Pom.Pom.Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.right * 20f, (Vector2.right + Vector2.up) * 20f, Vector2.up * 20f);
}
