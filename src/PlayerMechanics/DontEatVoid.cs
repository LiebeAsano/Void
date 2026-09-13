using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class DontEatVoid
{
    public static void Hook()
    {
        On.Player.EatMeatUpdate += Player_EatMeatUpdate;
    }

    private static void Player_EatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        orig(self, graspIndex);

        if (self.eatMeat != 50 || self.AreVoidViy())
            return;

        if (self.grasps[graspIndex]?.grabbed is not Player prey)
            return;

        if (prey.IsVoid())
        {
            if (!Karma11Update.VoidKarma11)
            {
                self.Die();
                return;
            }

            self.GetPlayerExt().voidPoisonBody = true;
            return;
        }

        if (prey.IsViy() || prey.GetPlayerExt().voidPoisonBody) self.GetPlayerExt().voidPoisonBody = true;
        
    }
}