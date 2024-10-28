using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.VoidEnums;

namespace VoidTemplate.PlayerMechanics;

internal static class HeavyCarry
{
    public static void Hook()
    {
        On.Player.HeavyCarry += Player_HeavyCarry;
    }

    public static bool Player_HeavyCarry(On.Player.orig_HeavyCarry orig, Player self, PhysicalObject obj)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            var crit = obj as Creature;
            if (obj is Player && crit.dead) return false;
        }
        return orig(self, obj);
    }
}
