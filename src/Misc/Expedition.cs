namespace VoidTemplate.Misc
{
    internal static class Expedition
    {
        public static void Hook()
        {
            On.Expedition.AchievementChallenge.ValidForThisSlugcat += AchievementChallenge_ValidForThisSlugcat;
        }

        private static bool AchievementChallenge_ValidForThisSlugcat(On.Expedition.AchievementChallenge.orig_ValidForThisSlugcat orig, global::Expedition.AchievementChallenge self, SlugcatStats.Name slugcat)
        {
            if (slugcat == VoidEnums.SlugcatID.Void && self.ID == MoreSlugcats.MoreSlugcatsEnums.EndgameID.Martyr) return false;
            return orig(self, slugcat);
        }
    }
}
