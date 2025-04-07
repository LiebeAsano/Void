using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class Karma11Update
{
    public static void Hook()
    {
        On.Player.ctor += Player_ctor;
    }

    public static bool VoidKarma11 = false;

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (self.IsVoid() && world.game.IsVoidStoryCampaign())
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
        if (ExternalSaveData.VoidKarma11)
        {
            VoidKarma11 = true;
        }
        else
        {
            VoidKarma11 = false;
        }
    }
}
