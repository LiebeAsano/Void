using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

internal static class CorruptionEaten
{
    public static void Hook()
    {
        IL.DaddyCorruption.EatenCreature.Update += EatenCreatureUpdateHook;
    }

    private static void EatenCreatureUpdateHook(ILContext il)
    {
        var c = new ILCursor(il);

        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<DaddyCorruption.EatenCreature>("creature"),
            x => x.MatchCallvirt<Creature>("Die")
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(DaddyCorruption.EatenCreature).GetField("creature"));
            c.EmitDelegate<System.Action<Creature>>(creature =>
            {
                if (creature is Player player && player.IsVoid())
                {
                    if (creature.room?.updateList?.Find(x => x is DaddyCorruption) is DaddyCorruption corruption)
                    {
                        corruption.effectColor = Color.black;
                        corruption.eyeColor = Color.black;

                        foreach (var bulb in corruption.allBulbs)
                        {
                            bulb.eatChunk = null;
                            bulb.hasEye = false;
                        }

                        corruption.eatCreatures.Clear();
                    }
                }
            });
        }
    }
}
