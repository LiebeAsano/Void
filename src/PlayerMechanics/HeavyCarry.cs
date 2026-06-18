using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class HeavyCarry
{
    public static void Hook()
    {
        On.Player.HeavyCarry += Player_HeavyCarry;
    }

    public static bool Player_HeavyCarry(On.Player.orig_HeavyCarry orig, Player self, PhysicalObject obj)
    {
        if (self.IsViy())
        {
            if (obj is Player)
            {
                return false;
            }
        }
        return orig(self, obj);
    }
}
