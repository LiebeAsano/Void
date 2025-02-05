using Menu;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.MenuTinkery;


public static class InitGame
{
	public static void Hook()
	{
		//statistics screen if viy is dead
		On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
		//set room to start if viy and playing first time
		//On.StoryGameSession.ctor += StoryGameSessionOnctor;
		//reset need to set starting room when playing as viy after first cycle is over
		//On.RainWorldGame.Win += RainWorldGameOnWin;
	}
	private const string startingRoom = "SH_S10";

	private static void RainWorldGameOnWin(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
	{
        if (self.GetStorySession is StoryGameSession storySession
            && storySession.saveStateNumber == VoidEnums.SlugcatID.Viy
            && !storySession.saveState.GetViyFirstCycle())
        {
            storySession.saveState.SetViyFirstCycle(true);
        }
        orig(self, malnourished);
	}
	
/// <summary>
/// HEAVYPERF
/// sets starting room if playing as viy first time
/// </summary>
/// <param name="orig"></param>
/// <param name="self"></param>
/// <param name="saveStateNumber"></param>
/// <param name="game"></param>
	private static void StoryGameSessionOnctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
	{
		if (saveStateNumber == VoidEnums.SlugcatID.Void)
		{
			SaveState saveState = game.rainWorld.progression.GetOrInitiateSaveState(saveStateNumber, game, game.manager.menuSetup, !ModManager.MSC || (!game.wasAnArtificerDream && !game.manager.rainWorld.safariMode));
			if (!saveState.GetViyFirstCycle())
			{
				game.startingRoom = startingRoom;
			}
		}
		orig(self, saveStateNumber, game);
	}

	private static void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
	{
		if (storyGameCharacter == VoidEnums.SlugcatID.Void)
		{
			Menu.SlugcatSelectMenu.SaveGameData saveGameData = self.saveGameData[storyGameCharacter];
            RainWorld rainWorld = self.manager.rainWorld;
            SaveState save = rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, self.manager.menuSetup, false);
            if (save.GetVoidCatDead())
			{
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                self.PlaySound(SoundID.MENU_Switch_Page_Out);
            }
			else if (save.GetVoidCatDead())
			{
				self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
				self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
				self.PlaySound(SoundID.MENU_Switch_Page_Out);
			}
			//normal void
			else orig(self, storyGameCharacter);
		}
		else orig(self, storyGameCharacter);
	}
}