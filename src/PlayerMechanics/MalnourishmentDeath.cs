using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.RainCycleChanges;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class MalnourishmentDeath
{
    public static void Hook()
    {
        On.Player.Update += Malnourishment_Death;
    }

    public static int Malnourished;

    private static void Malnourishment_Death(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (!self.IsVoid() || self.room is null)
            return;

        RainWorldGame game = self.room.game;
        StoryGameSession session = game.GetStorySession;
        SaveState save = session?.saveState;
        bool inDream = self.abstractCreature.GetPlayerState().InDream;

        bool rainStarvation = game.world?.rainCycle != null
            && game.world.rainCycle.GetRainCycleExt().starvationRequirement;

        bool savedStarvation = save?.malnourished == true;
        bool persistentStarvation = rainStarvation || savedStarvation;

        if ((game.IsVoidWorld() || inDream)
            && self.Malnourished
            && !persistentStarvation)
        {
            Malnourished++;
        }
        else if (!self.Malnourished || persistentStarvation)
        {
            Malnourished = 0;
        }

        if (Malnourished >= 440 && SlugStats.illness < 7200)
        {
            self.SetMalnourished(false);
            Malnourished = 0;

            if (game.IsVoidStoryCampaign())
                FoodChange.RefreshLiveFoodStats(game);
        }

        bool hasVoidMark = save != null && save.GetVoidMarkV3();

        if (!game.IsVoidWorld()
            && self.Malnourished
            && !inDream
            && !game.rainWorld.ExpeditionMode
            && !hasVoidMark)
        {
            self.Die();
        }
    }
}