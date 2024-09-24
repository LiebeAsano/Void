using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Features
{
    internal static class NourishmentOfObjectEaten
    {
        public static void Hook()
        {
            //On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
        }

        private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {


            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.session is StoryGameSession session &&
                session.characterStats.name == VoidEnums.SlugcatID.TheVoid)
            {

                bool hasMark = game.IsStorySession && game.GetStorySession.saveState.deathPersistentSaveData.theMark;

                if (hasMark || session.saveState.deathPersistentSaveData.karma == 10)
                {
                    string objectId = eatenobject.ToString();

                    if (objectId is "Fly" or "DangleFruit" or "WaterNut" or
                        "SlimeMold" or "SSOracleSwarmer" or "MoreSlugcats.GooieDuck" or
                        "MoreSlugcats.LillyPuck" or "MoreSlugcats.DandelionPeach" or "MoreSlugcats.GlowWeed" or
                        "MoreSlugcats.Seed")
                    {
                        return orig(slugcatIndex, eatenobject);
                    }
                    else
                    {
                        return orig(slugcatIndex, eatenobject) * 2;
                    }
                }
                return orig(slugcatIndex, eatenobject);
            }
            return orig(slugcatIndex, eatenobject);
        }

    }
}
