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
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
    }

    private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        var result = orig(self, obj);
        if (self.slugcatStats.name != StaticStuff.TheVoid) return result;
        int amountOfSpearsInHands = self.grasps.Aggregate(func: (int acc, Creature.Grasp grasp) => acc + ((grasp?.grabbed is Spear) ? 1 : 0), seed: 0);
        if (amountOfSpearsInHands == 1 && self.Grabability(obj) == Player.ObjectGrabability.Drag) return true;
        return result;
    }
}
