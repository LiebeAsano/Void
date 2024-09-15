using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class CanBeSwallowed
{
    public static void Hook()
    {
        On.Player.CanBeSwallowed += Player_CanBeSwallowed;
    }

    private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (self.IsVoid())
        {
            return testObj is not Creature && testObj is not Spear && testObj is not VultureMask || orig(self, testObj);
        }
        return orig(self, testObj);
    }
}
