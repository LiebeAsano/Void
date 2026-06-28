using Expedition;
using MoreSlugcats;
using UnityEngine;

namespace VoidTemplate.Misc;

public static class Expedition
{
    public static void Hook()
    {
        On.Expedition.VistaChallenge.ModifyVistaCandidates += VistaChallenge_ModifyVistaCandidates;
        //On.Expedition.AchievementChallenge.ValidForThisSlugcat += AchievementChallenge_ValidForThisSlugcat;
    }

    private static void VistaChallenge_ModifyVistaCandidates(On.Expedition.VistaChallenge.orig_ModifyVistaCandidates orig, VistaChallenge self, VistaChallenge input)
    {
        if (input.room == "HI_B04" && ModManager.MSC && ExpeditionData.slugcatPlayer != MoreSlugcatsEnums.SlugcatStatsName.Saint)
        {
            input.location = new Vector2(2008f, 1385f);
            return;
        }
        orig(self, input);
    }

    private static bool AchievementChallenge_ValidForThisSlugcat(On.Expedition.AchievementChallenge.orig_ValidForThisSlugcat orig, global::Expedition.AchievementChallenge self, SlugcatStats.Name slugcat)
    {
        if (slugcat == VoidEnums.SlugcatID.Void && self.ID == MoreSlugcats.MoreSlugcatsEnums.EndgameID.Martyr) return false;
        return orig(self, slugcat);
    }
}
