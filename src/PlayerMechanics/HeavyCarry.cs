using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class HeavyCarry
{
    public static void Hook()
    {
        //On.Player.HeavyCarry += Player_HeavyCarry;
    }

    public static bool Player_HeavyCarry(On.Player.orig_HeavyCarry orig, Player self, PhysicalObject obj)
    {
        if (self.AreVoidViy())
        {
            if (obj is Player)
            {
                return false;
            }
        }
        return orig(self, obj);
    }
}
