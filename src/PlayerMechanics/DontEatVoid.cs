using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class DontEatVoid
{
	public static void Hook()
	{
        On.Player.CanEatMeat += Player_CanEatMeat;
        On.Player.EatMeatUpdate += DontEat_Void;
	}

    private static bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
    {
        return crit is Player player && player.AreVoidViy() || orig(self, crit);
    }

    private static void DontEat_Void(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        orig(self, graspIndex);
        if (self.eatMeat != 50 || self.AreVoidViy()) return;
        foreach (var grasp in self.grasps)
        {
            if (grasp?.grabbed is Player prey && prey.AreVoidViy())
            {
                self.Die();
                break;
            }
        }
    }
}
