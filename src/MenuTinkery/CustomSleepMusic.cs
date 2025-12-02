using Menu;
using On.Menu;
using System;
using VoidTemplate.PlayerMechanics.Karma11Features;

namespace VoidTemplate.MenuTinkery;

public static class CustomSleepMusic
{
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;
        On.Menu.SleepAndDeathScreen.Singal += SleepAndDeathScreen_Singal;
    }

    private static void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
    {
        orig(self);

        if (self?.saveState == null) return;

        if (self.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            if (self.IsSleepScreen && (self.saveState.GetVoidMarkV3() || Karma11Update.VoidKarma11 || Karma11Update.VoidNightmare) && self.soundLoop != null && self.mySoundLoopID != VoidEnums.SoundID.SleepMarkSound)
            {
                self.soundLoop.Destroy();
                self.mySoundLoopID = VoidEnums.SoundID.SleepMarkSound;
                self.PlaySound(self.mySoundLoopID);
            }
        }
    }

    private static void SleepAndDeathScreen_Singal(On.Menu.SleepAndDeathScreen.orig_Singal orig, Menu.SleepAndDeathScreen self, Menu.MenuObject sender, string message)
    {
        orig(self, sender, message);

        if ((message == "CONTINUE" || message == "EXIT") && self.saveState?.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            if (self.soundLoop != null && self.mySoundLoopID == VoidEnums.SoundID.SleepMarkSound)
            {
                self.soundLoop.Destroy();
                self.soundLoop = null;
            }
        }
    }
}
