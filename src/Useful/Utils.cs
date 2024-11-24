using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using System.Runtime.CompilerServices;
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
