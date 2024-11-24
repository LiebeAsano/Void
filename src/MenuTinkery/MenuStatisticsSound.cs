using MonoMod.RuntimeDetour;
using Music;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IL.Menu.MenuScene;

namespace VoidTemplate.MenuTinkery;

internal static class MenuStatisticsSound
{
	static Dictionary<string, SoundID> StatisticSoundMap;
	public static void Hook()
	{
		On.Menu.StoryGameStatisticsScreen.GetDataFromGame += GetDataFromGame_Update;
		On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;

		StatisticSoundMap = new()
		{
			{ "Static_Death_Void",  VoidEnums.SoundID.StaticDeathSound },
			{ "Static_Death_Void11",  VoidEnums.SoundID.StaticDeathSound11 },
			{ "Static_End_Scene_Void", VoidEnums.SoundID.StaticEndSound },
			{ "Static_End_Scene_Void11", VoidEnums.SoundID.StaticEndSound11 },
		};
	}

	private static void KarmaLadderScreen_Singal(On.Menu.KarmaLadderScreen.orig_Singal orig, Menu.KarmaLadderScreen self, Menu.MenuObject sender, string message)
	{
		orig(self, sender, message);
		if ((message == "CONTINUE"
			|| message == "EXIT")
			&& self.manager.musicPlayer is MusicPlayer p
			&& StatisticSoundMap.Values.Any(value => value.value == p.song.name))
		{
			p.song.FadeOut(20f);
		}
	}

	private static void GetDataFromGame_Update(On.Menu.StoryGameStatisticsScreen.orig_GetDataFromGame orig, Menu.StoryGameStatisticsScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
	{
		if (package.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
		{
			string currentScene = self.scene.sceneID.value;

			if (StatisticSoundMap.Keys.Contains(currentScene)
				&& self.manager.musicPlayer is MusicPlayer player)
			{
				player.song = new(player, StatisticSoundMap[currentScene].value, MusicPlayer.MusicContext.Menu);
				player.song.playWhenReady = true;

			}
		}
		orig(self, package);
	}
}
