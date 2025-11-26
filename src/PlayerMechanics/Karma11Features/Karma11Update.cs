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
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
    }

    public static bool VoidKarma11 = false;

    public static bool VoidNightmare = false;

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (self.IsVoid())
        {
            if (self.abstractCreature.world.game.IsVoidStoryCampaign())
            {
                if (self.KarmaCap == 10)
                {
                    ExternalSaveData.VoidKarma11 = true;
                    VoidKarma11 = false;
                }
                else
                {
                    ExternalSaveData.VoidKarma11 = false;
                    VoidKarma11 = false;
                }
            }
            if (!self.abstractCreature.world.game.IsVoidStoryCampaign())
            {
                if (ExternalSaveData.VoidKarma11 && !VoidDreamScript.IsVoidDream)
                {
                    VoidKarma11 = true;
                }
                else
                {
                    VoidKarma11 = false;
                }
            }
        }
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.IsVoid() && self.abstractCreature.world.game.IsVoidStoryCampaign() && !VoidKarma11
            && self.KarmaCap == 10 
            && (self.FoodInStomach >= 7 - self.abstractCreature?.world?.game?.GetStorySession?.saveState.GetVoidFoodToHibernate() 
            || self.abstractCreature?.world?.game?.GetStorySession?.saveState.GetVoidFoodToHibernate() == 6))
        {
            VoidKarma11 = true;
        }
        if (self.abstractCreature.GetPlayerState().InDream)
        {
            VoidNightmare = true;
        }
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        VoidNightmare = false;
    }
}
