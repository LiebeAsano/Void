using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Runtime.CompilerServices;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

public class BigJellyfishStunImmunity
{
    private sealed class JellyState
    {
        public int deathTimer;
    }

    private static readonly ConditionalWeakTable<BigJellyFish, JellyState> jellyStates = new();

    public static void Hook()
    {
        IL.MoreSlugcats.BigJellyFish.Update += BigJellyFish_Update;
        On.MoreSlugcats.BigJellyFish.ConsumeCreateUpdate += BigJellyFish_ConsumeCreateUpdate;
        On.MoreSlugcats.BigJellyFish.Update += BigJellyFish_Update2;
    }

    private static void BigJellyFish_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = c.MarkLabel();
        if (c.TryGotoNext(
            x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Stun))))
            && c.TryGotoPrev(MoveType.After,
            x => x.MatchIsinst<Creature>(),
            x => x.MatchBrfalse(out label)
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 16);
            c.EmitDelegate<Func<BigJellyFish, int, bool>>((jellyfish, inspectedGrasp) =>
            jellyfish.latchOnToBodyChunks[inspectedGrasp].owner is Player p && (p.AreVoidViy()));
            c.Emit(OpCodes.Brtrue, label);
        }
        else LogExErr("failed to find place checking for creature stun in IL; void will be unintentionally vulnerable to MSC big jellyfish");
    }

    private static void BigJellyFish_ConsumeCreateUpdate(On.MoreSlugcats.BigJellyFish.orig_ConsumeCreateUpdate orig, BigJellyFish self)
    {
        orig(self);

        for (int i = self.consumedCreatures.Count - 1; i >= 0; i--)
        {
            if (self.consumedCreatures[i] is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void)
            {
                jellyStates.GetOrCreateValue(self).deathTimer = UnityEngine.Random.Range(160, 321);
                break;
            }
        }
    }

    private static void BigJellyFish_Update2(On.MoreSlugcats.BigJellyFish.orig_Update orig, BigJellyFish self, bool eu)
    {
        orig(self, eu);

        if (!jellyStates.TryGetValue(self, out var state) || state.deathTimer <= 0)
            return;

        if (self.slatedForDeletetion || self.room == null)
        {
            state.deathTimer = 0;
            return;
        }

        state.deathTimer--;
        if (state.deathTimer <= 0)
        {
            self.Die();
        }
    }
}