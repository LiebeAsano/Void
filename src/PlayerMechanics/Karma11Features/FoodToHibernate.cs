using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class FoodToHibernate
{
    public static void Hook()
    {
        On.StoryGameSession.ctor += StoryGameSession_ctor;
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        if (self.saveState.saveStateNumber == VoidEnums.SlugcatID.TheVoid && self.saveState.deathPersistentSaveData.karma == 10)
        {
            self.characterStats.foodToHibernate = 6;
            self.characterStats.maxFood = 9;
        }
    }
}
