using OverseerHolograms;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

internal static class OverseerBehavior
{
    public static void Hook()
    {
        On.OverseerCommunicationModule.AnyProgressionDirection += OverseerCommunicationModule_AnyProgressionDirection;
        On.OverseerCommunicationModule.ReevaluateConcern += OverseerCommunicationModule_ReevaluateConcern;
        On.OverseerCommunicationModule.WantToShowImage += OverseerCommunicationModule_WantToShowImage;
    }
    private static bool OverseerCommunicationModule_WantToShowImage(On.OverseerCommunicationModule.orig_WantToShowImage orig, OverseerCommunicationModule self, string roomName)
    {
        if (self.player.abstractCreature.world.game.IsVoidStoryCampaign())
            return self.overseerAI.overseer.hologram.message != OverseerHologram.Message.GateScene &&
                   !self.GuideState.HasImageBeenShownInRoom(roomName);
        return orig(self, roomName);
    }

    private static void OverseerCommunicationModule_ReevaluateConcern(On.OverseerCommunicationModule.orig_ReevaluateConcern orig, OverseerCommunicationModule self, Player player)
    {
        if (player.abstractCreature.world.game.IsVoidStoryCampaign())
        {
            self.forcedDirectionToGive = null;
            self.inputInstruction = null;
        }
        orig(self, player);
    }

    private static bool OverseerCommunicationModule_AnyProgressionDirection(On.OverseerCommunicationModule.orig_AnyProgressionDirection orig, OverseerCommunicationModule self, Player player)
    {
        if (player.abstractCreature.world.game.IsVoidStoryCampaign())
            return false;
        return orig(self, player);
    }
}
