using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.Objects.NoodleEgg;
internal static class ShellGrabUpdate
{
    public static void Hook()
    {
        IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    private static void Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new(il);
        if (c.TryGotoNext(x => x.MatchStloc(13))
            && c.TryGotoNext(MoveType.After, x => x.MatchBrfalse(out _)))
        {
            LogExInf(c.ToString());
            ILCursor u = c.Clone();
            if (u.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(13),
                x => x.MatchStloc(6)))
            {
                ILLabel label = u.MarkLabel();

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, 13);
                c.EmitDelegate((Player player, int grasp) =>
                {
                    if (player.grasps[grasp].grabbed is NeedleEgg egg && egg.GetEdible().shellCrack)
                    {
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, label);
            }
            else
            {
                logerr($"{nameof(VoidTemplate.Objects.NoodleEgg)}.{nameof(ShellGrabUpdate)}.{nameof(Player_GrabUpdate)}: second match failed");
            }
        }
        else
        {
            logerr($"{nameof(VoidTemplate.Objects.NoodleEgg)}.{nameof(ShellGrabUpdate)}.{nameof(Player_GrabUpdate)}: first match failed");
        }
    }
}