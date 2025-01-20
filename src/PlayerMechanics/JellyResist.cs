using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics
{
    internal static class JellyResist
    {
        public static void Hook()
        {
            IL.JellyFish.Update += JellyFish_Update;
        }

        private static void JellyFish_Update(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel label = c.DefineLabel();

            if (c.TryGotoNext(MoveType.After, x => x.MatchIsinst<Creature>(), x => x.MatchBrfalse(out _)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_2);
                c.EmitDelegate<Func<JellyFish, int, bool>>((self, chunk) =>
                {
                    if (self.latchOnToBodyChunks[chunk].owner is Player player)
                    {
                        return player.slugcatStats.name == VoidEnums.SlugcatID.Void;
                    }
                    return false;
                });

                c.Emit(OpCodes.Brtrue, label);
            }
            else
            {
                LogExErr("&Failed to find the proper place to insert IL code, Jelly Update");
            }
            if (c.TryGotoNext(MoveType.After, i => i.MatchCallvirt<Creature>("Stun")))
            {
                c.MarkLabel(label);
            }
            else
            {
                LogExErr("&Failed to find the proper place to insert IL code Creature Stun, Jelly Update");
            }
        }
    }
}
