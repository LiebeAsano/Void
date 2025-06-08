using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using static VoidTemplate.Useful.Utils;
using System;

namespace VoidTemplate.Objects.NoodleEgg
{
    internal static class _NoodleEggMeta
    {
        public static void Hook()
        {
            IL.Player.BiteEdibleObject += Player_BiteEdibleObject;
            IL.SlugcatHand.Update += SlugcatHand_Update;
        }

        private static void SlugcatHand_Update(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(x => x.MatchLdfld<Player>("eatCounter"));
            c.GotoNext(MoveType.After, x => x.MatchBrfalse(out _));

            ILCursor u = c.Clone();
            u.GotoNext(MoveType.Before,
                x => x.MatchLdloc(4),
                x => x.MatchStloc(3));
            ILLabel label = u.MarkLabel();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 4);
            c.EmitDelegate((SlugcatHand hand, int grasp) =>
            {
                if ((hand.owner.owner as Player).grasps[grasp].grabbed is NeedleEgg)
                {
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brtrue, label);
        }

        private static void Player_BiteEdibleObject(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After, x => x.MatchBrfalse(out _));

            ILLabel label = c.Clone().MarkLabel();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((Player player, bool eu, int grasp) =>
            {
                if (player.AreVoidViy() && player.grasps[grasp].grabbed is NeedleEgg egg)
                {
                    if (player.graphicsModule != null)
                    {
                        (player.graphicsModule as PlayerGraphics).BiteFly(grasp);
                    }
                    egg.GetEdible().Bite(player.grasps[grasp], eu);
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brfalse, label);
            c.Emit(OpCodes.Ret);
        }
    }
}
