using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

public static class VoidSpawnGraphics
{
    public static void Hook()
    {
        On.VoidSpawnGraphics.Update += VoidSpawnGraphics_Update;
    }

    private static void VoidSpawnGraphics_Update(On.VoidSpawnGraphics.orig_Update orig, global::VoidSpawnGraphics self)
    {
        if (self.owner.room.game.session is StoryGameSession story 
            && (story.game.IsVoidStoryCampaign() && story.saveState.deathPersistentSaveData.karmaCap == 10 || story.game.IsVoidWorld() && Karma11Update.VoidKarma11))
            return;
        orig(self);
    }
}
