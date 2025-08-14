using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.MenuTinkery;

public static class CustomSleepMusic
{
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.ctor += SleepAndDeathScreen_ctor;
        On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;
    }

    private static void SleepAndDeathScreen_ctor(On.Menu.SleepAndDeathScreen.orig_ctor orig, Menu.SleepAndDeathScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
    {
        orig(self, manager, ID);

        if (self.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            if (self.IsSleepScreen && self.saveState.GetVoidMarkV3())
            {
                self.soundLoop?.Destroy();

                self.mySoundLoopID = VoidEnums.SoundID.SleepMarkSound;

                self.PlaySound(self.mySoundLoopID);
            }
        }
    }

    private static void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
    {
        orig(self);
        if (self.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            if (self.IsSleepScreen && self.saveState.GetVoidMarkV3() && self.soundLoop != null && self.mySoundLoopID != VoidEnums.SoundID.SleepMarkSound)
            {
                self.soundLoop.Destroy();
                self.mySoundLoopID = VoidEnums.SoundID.SleepMarkSound;
                self.PlaySound(self.mySoundLoopID);
            }
        }
    }
}
