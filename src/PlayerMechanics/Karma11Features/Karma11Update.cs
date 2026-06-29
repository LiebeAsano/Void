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
                VoidPermaNightmare = ExternalSaveData.VoidPermaNightmare == 2 ||
                                     game.GetStorySession.saveState.GetVoidFoodToHibernate() == 6;
            }
            else
            {
                ExternalSaveData.VoidKarma11 = false;
                VoidKarma11 = false;
                game.GetStorySession.saveState.SetKarmaToken(5);
            }
        }
        else
        {
            VoidKarma11 = ExternalSaveData.VoidKarma11 &&
                          !VoidDreamScript.IsVoidDream;
            VoidPermaNightmare = VoidKarma11;
        }

        if (self.abstractCreature.GetPlayerState().InDream)
        {
            ExternalSaveData.VoidPermaNightmare = 2;
            VoidPermaNightmare = true;
            VoidKarma11 = true;
        }

        if (game.rainWorld.ExpeditionMode)
        {
            game.GetStorySession.saveState.SetVoidMarkV3(true);
            VoidPermaNightmare = false;
            VoidKarma11 = false;
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

            VoidKarma11 = self.FoodInStomach >= 7 - voidFoodToHibernate;
        }
    }
}
