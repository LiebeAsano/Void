using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

internal static class Grabability
{
    public static void Hook()
    {
        On.Player.Grabability += Player_Grabability;
    }

    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (obj is PoleMimic || obj is TentaclePlant)
            return Player.ObjectGrabability.CantGrab;
        return orig(self, obj);
    }
}
