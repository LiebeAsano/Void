using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Menu;
using UnityEngine;

namespace VoidTemplate.Useful;
internal static class Utils
{
	public const string ModID = "void.lwteam";
	public const int TicksPerSecond = 40;
	public static void loginf(object e) => _Plugin.logger.LogInfo(e);
	public static void logerr(object e) => _Plugin.logger.LogError(e);
	public static string TranslateStringComplex(this string str) => RWCustom.Custom.rainWorld.inGameTranslator.Translate(str).Replace("<LINE>", "\n");
	public static string TranslateString(this string str) => RWCustom.Custom.rainWorld.inGameTranslator.Translate(str);

    public static bool IsViy(SaveState saveState)
    {
	    return saveState.saveStateNumber == VoidEnums.SlugcatID.Void 
	           && saveState.GetVoidCatDead() 
	           && saveState.deathPersistentSaveData.karmaCap == 10;
    }
    public static bool IsViy(SlugcatSelectMenu.SaveGameData saveGameData) => saveGameData.karmaCap == 10 && saveGameData.redsExtraCycles;
    public static bool IsAliveViy(SlugcatSelectMenu.SaveGameData saveGameData) => IsViy(saveGameData) && !saveGameData.redsDeath;
    public static bool IsDeadViy(SlugcatSelectMenu.SaveGameData saveGameData) => IsViy(saveGameData) && saveGameData.redsDeath;
    

    public static bool IsVoid(this Player p) => p.slugcatStats.name == VoidEnums.SlugcatID.Void;
	public static bool IsVoidWorld(this RainWorldGame game) => game.StoryCharacter == VoidEnums.SlugcatID.Void;
	public static bool IsVoidStoryCampaign(this RainWorldGame game) => (game.IsVoidWorld()
			&& !(ModManager.Expedition && game.rainWorld.ExpeditionMode));
	public static bool KarmaKapCheck(this Player p, int karmaRequirement) => p.KarmaCap >= karmaRequirement;



	//stolen with permission from Henpemaz' https://github.com/henpemaz/Rain-Meadow/blob/main/RainMeadow.Logging.cs
	private static string TrimCaller(string callerFile) { return (callerFile = callerFile.Substring(Mathf.Max(callerFile.LastIndexOf(Path.DirectorySeparatorChar), callerFile.LastIndexOf(Path.AltDirectorySeparatorChar)) + 1)).Substring(0, callerFile.LastIndexOf('.')); }
	private static string LogTime() { return ((int)(Time.time * 1000)).ToString(); }
	private static string LogDOT() { return DateTime.Now.ToUniversalTime().TimeOfDay.ToString().Substring(0, 8); }
	public static void LogExInf(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
	{
		loginf($"{LogDOT()}|{LogTime()}|{callerFile}.{callerName}:{data}");
	}
	public static void LogExErr(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
	{
		logerr($"{LogDOT()}|{LogTime()}|{callerFile}.{callerName}:{data}");
	}

	public static void LogdumpIL(ILContext iLContext)
	{
		loginf("Dumping IL -----");

		foreach (Instruction instruction in iLContext.Instrs)
		{
			try
			{
				loginf(instruction.OpCode);
				loginf(instruction.Operand);
			}
			catch { }
		}

		loginf("IL Dump end -----");
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
