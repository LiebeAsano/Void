namespace VoidTemplate.PlayerMechanics.GhostFeatures;

public static class NoGhostFlakes
{
    public static void Hook()
    {
        On.Room.NowViewed += RoomOnNowViewed;
    }

    private static void RoomOnNowViewed(On.Room.orig_NowViewed orig, Room self)
    {
        orig(self);
        if ((self.game.StoryCharacter == VoidEnums.SlugcatID.Void || self.game.StoryCharacter == VoidEnums.SlugcatID.Viy)
            && self.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
        {
            foreach (UpdatableAndDeletable updatableAndDeletable in self.updateList)
            {
                if (updatableAndDeletable is GoldFlakes flakes) flakes.slatedForDeletetion = true;
            }
        }
    }
}