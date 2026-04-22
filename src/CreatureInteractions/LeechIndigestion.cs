using System;
using System.Runtime.CompilerServices;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class LeechIndigestion
{
    private sealed class LeechState
    {
        public int killTimer;
        public bool wasAttachedToVoidViy;
    }

    private static readonly ConditionalWeakTable<Leech, LeechState> leechStates = new();

    public static void Hook()
    {
        On.Leech.Update += Leech_Update;
    }

    private static void Leech_Update(On.Leech.orig_Update orig, Leech self, bool eu)
    {
        orig(self, eu);

        var state = leechStates.GetOrCreateValue(self);

        if (self.slatedForDeletetion || self.room == null || self.dead)
        {
            state.killTimer = 0;
            state.wasAttachedToVoidViy = false;
            return;
        }

        bool attachedToVoidViy =
            self.grasps != null &&
            Array.Exists(self.grasps, grasp =>
                grasp is not null &&
                grasp.grabbed is Player player &&
                player.AreVoidViy());

        if (attachedToVoidViy)
        {
            if (!state.wasAttachedToVoidViy)
            {
                state.killTimer = UnityEngine.Random.Range(160, 321);
                state.wasAttachedToVoidViy = true;
            }

            if (state.killTimer > 0)
            {
                state.killTimer--;

                if (state.killTimer <= 0)
                {
                    self.Die();
                    state.wasAttachedToVoidViy = false;
                }
            }
        }
        else
        {
            state.killTimer = 0;
            state.wasAttachedToVoidViy = false;
        }
    }
}