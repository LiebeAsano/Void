using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class KarmaLadderTweaks
{
    const int karma11index = 10;
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.FoodCountDownDone += SleepAndDeathScreen_FoodCountDownDone;
    }

    private static void SleepAndDeathScreen_FoodCountDownDone(On.Menu.SleepAndDeathScreen.orig_FoodCountDownDone orig, Menu.SleepAndDeathScreen self)
    {
        orig(self);
        if (self.karma.x == 10)
            self.karmaLadder.GoToKarma(10, true);
    }
}
