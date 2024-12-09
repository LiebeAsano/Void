namespace VoidTemplate.MenuTinkery;
using Music;
using System.Collections.Generic;
using System.Linq;

internal static class MenuStatisticsSound
{
	static Dictionary<string, string> StatisticSoundMap;
	public static void Hook()
	{
		On.Menu.StoryGameStatisticsScreen.GetDataFromGame += GetDataFromGame_Update;
		//On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;
        StatisticSoundMap = new()
		{
			{ "Static_Death_Void",  "StaticDeathSound" },
			{ "Static_Death_Void11",  "StaticDeathSound11" },
			{ "Static_End_Scene_Void", "StaticEndSound" },
			{ "Static_End_Scene_Void11", "StaticEndSound11" },
		};
	}

	private static void KarmaLadderScreen_Singal(On.Menu.KarmaLadderScreen.orig_Singal orig, Menu.KarmaLadderScreen self, Menu.MenuObject sender, string message)
	{
		orig(self, sender, message);
		if ((message == "CONTINUE"
			|| message == "EXIT")
			&& self.manager.musicPlayer is MusicPlayer p
			&& p.song is not null
			&& StatisticSoundMap.Values.Any(value => value == p.song.name))
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
                player.song = new(player, StatisticSoundMap[currentScene], MusicPlayer.MusicContext.Menu)
                {
                    playWhenReady = true
                };

            }
		}
		orig(self, package);
	}
}
