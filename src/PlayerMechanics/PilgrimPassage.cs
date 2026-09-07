using MoreSlugcats;
using System.Collections.Generic;

namespace VoidTemplate.PlayerMechanics;

public static class PilgrimPassage
{
    public static void Hook()
    {
        On.SlugcatStats.SlugcatStoryRegions += SlugcatStats_SlugcatStoryRegions;
        On.WinState.CycleCompleted += WinState_CycleCompleted;
    }

    private static List<string> SlugcatStats_SlugcatStoryRegions(On.SlugcatStats.orig_SlugcatStoryRegions orig, SlugcatStats.Name i)
    {
        string[] source;
        if (i == VoidEnums.SlugcatID.Void)
        {
            source =
                [
                    "SU",
                    "HI",
                    "DS",
                    "CC",
                    "GW",
                    "SH",
                    "VS",
                    "SL",
                    "SI",
                    "LF",
                    "UW",
                    "SS",
                    "SB",
                    "LC",
                    "WARB",
                    "WARD",
                    "WARE",
                    "WARF",
                    "WARG",
                    "WRFA",
                    "WRFB"
                ];
            return [.. source];
        }
        return orig(i);
    }

    private static void WinState_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
    {
        orig(self, game);

        if (game.StoryCharacter != VoidEnums.SlugcatID.Void ||
            game.GetStorySession.saveState.deathPersistentSaveData.karma != 10)
        {
            return;
        }

        if (self.GetTracker(MoreSlugcatsEnums.EndgameID.Pilgrim, false) is not WinState.BoolArrayTracker pilgrimTracker)
        {
            return;
        }

        for (int i = 0; i < pilgrimTracker.progress.Length; i++)
        {
            pilgrimTracker.progress[i] = false;
            pilgrimTracker.lastShownProgress[i] = false;
        }
        pilgrimTracker.consumed = false;
    }
}
