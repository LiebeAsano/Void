namespace VoidTemplate.PlayerMechanics;

internal static class HeavyCarry
{
    public static void Hook()
    {
        On.Player.HeavyCarry += Player_HeavyCarry;
    }

    public static bool Player_HeavyCarry(On.Player.orig_HeavyCarry orig, Player self, PhysicalObject obj)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void || self.slugcatStats.name == VoidEnums.SlugcatID.Viy)
        {
            if (obj is Player)
            {
                return false;
            }
        }
        return orig(self, obj);
    }
}
