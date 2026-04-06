using System;
using System.Runtime.CompilerServices;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class LeechIndigestion
{
    private sealed class LeechState
    {
        public int killTimer;
    }

    private static readonly ConditionalWeakTable<Leech, LeechState> leechStates = new();

    private const int KillDelay = 6 * 40;

    public static void Hook()
    {
        On.Leech.Attached += OnLeechAttached;
        On.Leech.Update += Leech_Update;
    }

    private static void OnLeechAttached(On.Leech.orig_Attached orig, Leech self)
    {
        orig(self);

        if (self.grasps == null)
            return;

        if (Array.Exists(self.grasps, grasp =>
                grasp is not null &&
                grasp.grabbed is Player player &&
                player.AreVoidViy()))
        {
            leechStates.GetOrCreateValue(self).killTimer = KillDelay;
        }
    }

    private static void Leech_Update(On.Leech.orig_Update orig, Leech self, bool eu)
    {
        orig(self, eu);

        if (!leechStates.TryGetValue(self, out var state) || state.killTimer <= 0)
            return;

        if (self.slatedForDeletetion || self.room == null || self.dead)
        {
            state.killTimer = 0;
            return;
        }

        bool stillAttachedToVoidViy = self.grasps != null && Array.Exists(self.grasps, grasp =>
            grasp is not null &&
            grasp.grabbed is Player player &&
            player.AreVoidViy());

        if (!stillAttachedToVoidViy)
        {
            state.killTimer = 0;
            return;
        }

        state.killTimer--;

        if (state.killTimer <= 0)
        {
            self.Die();
        }
    }
}