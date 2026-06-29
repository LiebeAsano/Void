namespace VoidTemplate.PlayerMechanics.GhostFeatures;

public class SBGhostSave
{
    public static void Hook()
    {
        On.SaveState.GhostEncounter += SaveState_GhostEncounter;
    }

    private static void SaveState_GhostEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
    {
        orig(self, ghost, rainWorld);
        if (self.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            if (ghost == GhostWorldPresence.GhostID.SB && self.cycleNumber == 0 && self.denPosition == SaveState.GetStoryDenPosition(VoidEnums.SlugcatID.Void, out _))
            {
                self.denPosition = "SB_S06";
            }
            self.progression.SaveWorldStateAndProgression(false);
        }
    }
}
