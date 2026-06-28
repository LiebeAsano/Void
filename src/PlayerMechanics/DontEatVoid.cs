using VoidTemplate.PlayerMechanics.Karma11Features;
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
            if (grasp?.grabbed is Player prey && prey.IsVoid() && !Karma11Update.VoidKarma11)
            {
                self.Die();
                break;
            }
            if (grasp?.grabbed is Player prey2 && (prey2.GetPlayerExt().voidPoisonBody || prey2.IsViy() || prey2.IsVoid() && Karma11Update.VoidKarma11))
            {
                self.GetPlayerExt().voidPoisonBody = true;
                break;
            }
        }
    }
}
