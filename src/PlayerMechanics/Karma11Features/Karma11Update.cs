using RegionKit.Modules.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Objects;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

public static class Karma11Update
{
    public static void Hook()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
    }

    public static bool VoidKarma11 { get; set; }
    public static bool VoidNightmare { get; set; }
    public static bool VoidPermaNightmare { get; set; }
    public static bool VoidBigViyKarma { get; set; }

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (!self.IsVoid()) return;

        var game = self.abstractCreature.world.game;

        if (game.IsVoidStoryCampaign())
        {
            if (self.KarmaCap == 10)
            {
                ExternalSaveData.VoidKarma11 = true;
                VoidKarma11 = ExternalSaveData.VoidPermaNightmare != 0;
                VoidPermaNightmare = ExternalSaveData.VoidPermaNightmare == 2;
                VoidBigViyKarma = ExternalSaveData.VoidPermaNightmare == 2;
            }
            else
            {
                ExternalSaveData.VoidKarma11 = false;
                VoidKarma11 = false;
            }
        }
        else
        {
            VoidKarma11 = ExternalSaveData.VoidKarma11 &&
                          ExternalSaveData.VoidPermaNightmare != 0 &&
                          !VoidDreamScript.IsVoidDream;
        }

        if (self.abstractCreature.GetPlayerState().InDream)
        {
            ExternalSaveData.VoidPermaNightmare = 2;
            VoidPermaNightmare = true;
            VoidKarma11 = true;
        }
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (self.IsVoid() &&
            self.abstractCreature.world.game.IsVoidStoryCampaign() &&
            !VoidKarma11 &&
            self.KarmaCap == 10)
        {
            int voidFoodToHibernate = self.abstractCreature?.world?.game?.GetStorySession?.saveState?.GetVoidFoodToHibernate() ?? 0;

            VoidKarma11 = self.FoodInStomach >= 7 - voidFoodToHibernate ||
                          voidFoodToHibernate == 6;
        }
    }
}
