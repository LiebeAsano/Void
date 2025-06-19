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
    public static class JellyResist
    {
        public static void Hook()
        {
            IL.JellyFish.Update += JellyFish_Update;
            On.JellyFish.Update += OnJellyFish_Update;
            On.JellyFish.Collide += JellyFish_Collide;
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
                        return player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy;
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

        public static int cooldown = 0;
        private static void OnJellyFish_Update(On.JellyFish.orig_Update orig, JellyFish self, bool eu)
        {
            orig(self, eu);
            cooldown++;
            cooldown = Math.Min(100, cooldown);
        }

        private static void JellyFish_Collide(On.JellyFish.orig_Collide orig, JellyFish self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (otherObject is Player player && player != self.thrownBy && (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy) && self.Electric)
            {
                if (cooldown == 100)
                {
                    self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos);
                    self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                    cooldown = 0;
                }
                return;
            }
            orig(self, otherObject, myChunk, otherChunk);
        }


    }
}
