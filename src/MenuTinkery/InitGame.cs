using Menu;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.MenuTinkery;

public static class InitGame
{
    private const string startingRoom = "SH_S10";

    public static void Hook()
    {
        CampaignStatisticsSave.Hook();
        On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
        On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;

        //On.StoryGameSession.ctor += StoryGameSessionOnctor;
        //On.RainWorldGame.Win += RainWorldGameOnWin;
    }

    private static void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    {
        if (CampaignStatisticsSave.TryOpenFromMainButton(self, storyGameCharacter)) return;

        if (self.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.New)
            _ = RequestStoryStartTokenAsync(storyGameCharacter);

        orig(self, storyGameCharacter);
    }

    private static void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    {
        if (CampaignStatisticsSave.TryOpenFromMainButton(self, storyGameCharacter)) return;

        orig(self, storyGameCharacter);
    }

    private static void RainWorldGameOnWin(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
    {
        if (self.GetStorySession is StoryGameSession storySession &&
            storySession.saveStateNumber == VoidEnums.SlugcatID.Viy && !storySession.saveState.GetViyFirstCycle())
            storySession.saveState.SetViyFirstCycle(true);

        orig(self, malnourished, fromWarpPoint);
    }

    private static void StoryGameSessionOnctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        if (saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            SaveState saveState = game.rainWorld.progression.GetOrInitiateSaveState(saveStateNumber, game, game.manager.menuSetup,
                !ModManager.MSC || (!game.wasAnArtificerDream && !game.manager.rainWorld.safariMode));

            if (!saveState.GetViyFirstCycle())
                game.startingRoom = startingRoom;
        }

        orig(self, saveStateNumber, game);
    }

    private static async System.Threading.Tasks.Task RequestStoryStartTokenAsync(SlugcatStats.Name storyGameCharacter)
    {
        try
        {
            await StoryStartTokenService.RequestAsync(storyGameCharacter);
        }
        catch (System.Exception exception)
        {
            LogExErr($"Story start token request failed: {exception.Message}");
        }
    }
}
