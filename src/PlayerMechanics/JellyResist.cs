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
            //IL.JellyFish.Update += JellyFish_Update;
        }

        private static void JellyFish_Update(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchIsinst<Creature>(), x => x.MatchBrfalse(out _)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(BodyChunk).GetField("owner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public));
                c.Emit(OpCodes.Ldfld, typeof(JellyFish).GetField("latchOnToBodyChunks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));

                c.EmitDelegate<Func<object, bool>>(owner =>
                {
                    if (owner is Player player)
                    {
                        return player.slugcatStats.name != VoidEnums.SlugcatID.Void;
                    }
                    return true;
                });

                c.Emit(OpCodes.Brtrue, c.Next.Next);
            }
            else
            {
                logerr("&Failed to find the proper place to insert IL code, Jelly Update");
            }
        }
    }
}
