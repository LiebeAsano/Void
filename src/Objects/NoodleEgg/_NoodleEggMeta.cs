using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using static VoidTemplate.Useful.Utils;
using System;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.Objects.NoodleEgg
{
    public static class _NoodleEggMeta
    {
        public static void Hook()
        {
            IL.Player.BiteEdibleObject += Player_BiteEdibleObject;
            IL.SlugcatHand.Update += SlugcatHand_Update;
            ShellGrabUpdate.Hook();
        }

        private static void SlugcatHand_Update(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(x => x.MatchLdfld<Player>("eatCounter"))
                && c.TryGotoNext(MoveType.After, x => x.MatchBrfalse(out _)))
            {
                ILCursor u = c.Clone();
                if (u.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(4),
                    x => x.MatchStloc(3)))
                {
                    ILLabel label = u.MarkLabel();

                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc, 4);
                    c.EmitDelegate((SlugcatHand hand, int grasp) =>
                    {
                        if ((hand.owner.owner as Player).grasps[grasp].grabbed is NeedleEgg egg && egg.GetEdible().CanEat(hand.owner.owner as Player))
                        {
                            return true;
                        }
                        return false;
                    });
                    c.Emit(OpCodes.Brtrue, label);
                }
                else
                {
                    logerr($"{nameof(VoidTemplate.Objects.NoodleEgg)}.{nameof(_NoodleEggMeta)}.{nameof(SlugcatHand_Update)}: second match failed");
                }
            }
            else
            {
                logerr($"{nameof(VoidTemplate.Objects.NoodleEgg)}.{nameof(_NoodleEggMeta)}.{nameof(SlugcatHand_Update)}: first match failed");
            }
        }

        private static void Player_BiteEdibleObject(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchBrfalse(out _)))
            {

                ILLabel label = c.Clone().MarkLabel();

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate((Player player, bool eu, int grasp) =>
                {
                    if (player.grasps[grasp].grabbed is NeedleEgg egg && egg.GetEdible().CanEat(player))
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
            else
            {
                logerr($"{nameof(VoidTemplate.Objects.NoodleEgg)}.{nameof(_NoodleEggMeta)}.{nameof(Player_BiteEdibleObject)}: first match failed");
            }
        }
    }
}
