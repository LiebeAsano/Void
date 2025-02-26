using RWCustom;

namespace VoidTemplate.PlayerMechanics.Karma11Features
{
    internal static class NourishmentOfObjectEaten
    {
        public static void Hook()
        {
            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
        }

        private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {


            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.session is StoryGameSession session &&
                (session.characterStats.name == VoidEnums.SlugcatID.Void || session.characterStats.name == VoidEnums.SlugcatID.Viy))
            {

                bool hasMark = game.IsStorySession && game.GetStorySession.saveState.deathPersistentSaveData.theMark;

                if (OptionInterface.OptionAccessors.SimpleFood)
                {
                    string objectId = eatenobject.ToString();
                    if (objectId is "Fly" or "DangleFruit" or "WaterNut" or
                        "SlimeMold" or "SSOracleSwarmer" or "MoreSlugcats.GooieDuck" or
                        "MoreSlugcats.LillyPuck" or "MoreSlugcats.DandelionPeach" or "MoreSlugcats.GlowWeed" or
                        "MoreSlugcats.Seed")
                    {
                        if (session.characterStats.name == VoidEnums.SlugcatID.Viy)
                        {
                            return 0;
                        }
                        else
                            return orig(slugcatIndex, eatenobject);
                    }
                    else
                    {
                        return orig(slugcatIndex, eatenobject) * 2;
                    }
                }
                else if (session.characterStats.name == VoidEnums.SlugcatID.Viy)
                {
                    string objectId = eatenobject.ToString();
                    if (objectId is "Fly" or "DangleFruit" or "WaterNut" or
                        "SlimeMold" or "SSOracleSwarmer" or "MoreSlugcats.GooieDuck" or
                        "MoreSlugcats.LillyPuck" or "MoreSlugcats.DandelionPeach" or "MoreSlugcats.GlowWeed" or
                        "MoreSlugcats.Seed")
                    {
                        return 0;
                    }
                }
                else
                {
                    return orig(slugcatIndex, eatenobject);
                }
            }
            return orig(slugcatIndex, eatenobject);
        }

    }
}
