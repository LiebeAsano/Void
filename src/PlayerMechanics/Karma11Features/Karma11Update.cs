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
        On.StoryGameSession.ctor += StoryGameSession_ctor;
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        if (saveStateNumber == VoidEnums.SlugcatID.Void && game.IsVoidStoryCampaign())
        {
            if (self.saveState.deathPersistentSaveData.karma == 10)
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
