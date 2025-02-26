using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class Karma11Update
{
    public static void Hook()
    {
        On.Player.ctor += Player_ctor;
    }

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (self.IsVoid())
        {
            if (self.KarmaCap == 10)
            {
                ExternalSaveData.VoidKarma11 = true;
            }
            else
            {
                ExternalSaveData.VoidKarma11 = false;
            }
        }
    }
}
