using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class DontEatVoid
{
	public static void Hook()
	{
		On.Player.EatMeatUpdate += DontEat_Void;
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
