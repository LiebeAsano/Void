using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.MenuTinkery;

internal static class DisablePassage
{
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.AddPassageButton += RemoveButtonForVoid;
    }

    private static void RemoveButtonForVoid(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, Menu.SleepAndDeathScreen self, bool buttonBlack)
    {
        if (self.saveState != null && (self.saveState.saveStateNumber == StaticStuff.TheVoid)) return; //no need in calling orig if it's void, because the button is not supposed to be here at all
        orig(self, buttonBlack);
    }
}
