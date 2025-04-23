using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

internal class BigJellyfishStunImmunity
{
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
            c.EmitDelegate<Func<BigJellyFish, int, bool>>((BigJellyFish jellyfish, int inspectedGrasp) =>
            jellyfish.latchOnToBodyChunks[inspectedGrasp].owner is Player p && (p.IsVoid() || p.IsViy()));
            c.Emit(OpCodes.Brtrue, label);
        }
        else LogExErr("failed to find place checking for creature stun in IL; void will be unintentionally vulnerable to MSC big jellyfish");
    }

    private static Dictionary<AbstractCreature, int> jellyfishDeathTimers = new Dictionary<AbstractCreature, int>();

    private static void BigJellyFish_ConsumeCreateUpdate(On.MoreSlugcats.BigJellyFish.orig_ConsumeCreateUpdate orig, BigJellyFish self)
    {
        orig(self);
        for (int i = self.consumedCreatures.Count - 1; i >= 0; i--)
        {
            var creature = self.consumedCreatures[i];

            if (creature is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void)
            {
                if (!jellyfishDeathTimers.ContainsKey(self.abstractCreature))
                {
                    jellyfishDeathTimers.Add(self.abstractCreature, 180);
                }
            }
        }
    }

    private static void BigJellyFish_Update2(On.MoreSlugcats.BigJellyFish.orig_Update orig, BigJellyFish self, bool eu)
    {
        orig(self, eu);
        if (jellyfishDeathTimers.TryGetValue(self.abstractCreature, out int timer))
        {
            timer -= 1;

            if (timer <= 0)
            {
                self.Die();
                jellyfishDeathTimers.Remove(self.abstractCreature);
            }
            else
            {
                jellyfishDeathTimers[self.abstractCreature] = timer;
            }
        }
    }
}
