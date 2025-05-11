using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

internal static class OverseerBehavior
{
    public static void Hook()
    {
        // Ваш существующий хук
        On.OverseerCommunicationModule.Update += OverseerCommunicationModule_Update;
    }

    private static void OverseerCommunicationModule_Update(On.OverseerCommunicationModule.orig_Update orig, OverseerCommunicationModule self)
    {
        if (self.room == null || self.room.game.Players.Count == 0 ||
            self.room.game.Players[0].realizedCreature == null ||
            self.room.game.Players[0].realizedCreature.room != self.room)
        {
            return;
        }

        if (self.room.game.IsVoidStoryCampaign())
        {
            return;
        }

        orig(self);
    }
}
