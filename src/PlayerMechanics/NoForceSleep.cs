using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class NoForceSleep
{
    public static void Hook()
    {
        On.Player.Update += NoForceSleep_Update;
    }

    private static void NoForceSleep_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.IsVoid() && self.KarmaCap != 10)
            self.forceSleepCounter = 0;
    }
}
