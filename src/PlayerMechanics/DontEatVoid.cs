using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class DontEatVoid
{
    public static void Hook()
    {
        On.Player.EatMeatUpdate += DontEat_Void;
    }
    
    private static void DontEat_Void(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        orig(self, graspIndex);
        if (self.eatMeat != 50 || self.IsVoid()) return;
        Array.ForEach(self.grasps, grasp =>
        {
            if (grasp != null
            && grasp.grabbed is Player prey
            && prey.IsVoid())
                self.Die();

        });
    }
}
