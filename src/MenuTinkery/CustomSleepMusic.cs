using VoidTemplate.PlayerMechanics.Karma11Features;
using KarmaLadderScreen = Menu.KarmaLadderScreen;

namespace VoidTemplate.MenuTinkery;

public static class CustomSleepMusic
{
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreenOnGetDataFromGame;
    }

    static void SleepAndDeathScreenOnGetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        SaveState save = package.saveState;
        if (save.saveStateNumber == VoidEnums.SlugcatID.Void 
            && self.IsSleepScreen 
            && (save.GetVoidMarkV3() || Karma11Update.VoidKarma11 || Karma11Update.VoidNightmare))
        {
            self.mySoundLoopID = VoidEnums.SoundID.SleepMarkSound;
        }
    }
}
