using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

internal class BigJellyfishStunImmunity
{
    public static void Hook()
    {
        IL.MoreSlugcats.BigJellyFish.Update += BigJellyFish_Update;
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
            c.EmitDelegate<Func<BigJellyFish, int, bool>>((BigJellyFish jellyfish, int inspectedGrasp) =>
            jellyfish.latchOnToBodyChunks[inspectedGrasp].owner is Player p && p.IsVoid());
            c.Emit(OpCodes.Brtrue, label);
        }
        else LogExErr("failed to find place checking for creature stun in IL; void will be unintentionally vulnerable to MSC big jellyfish");
    }
}
