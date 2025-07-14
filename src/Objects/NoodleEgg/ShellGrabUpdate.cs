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
public static class ShellGrabUpdate
{
    public static void Hook()
    {
        IL.Player.GrabUpdate += Player_GrabUpdate;
        On.NeedleEgg.Update += NeedleEgg_Update;
        On.NeedleEgg.DrawSprites += NeedleEgg_DrawSprites;
    }

    private static void Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new(il);
        if (c.TryGotoNext(x => x.MatchStloc(13))
            && c.TryGotoNext(MoveType.After, x => x.MatchBrfalse(out _)))
        {
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

    private static void NeedleEgg_Update(On.NeedleEgg.orig_Update orig, NeedleEgg self, bool eu)
    {
        orig(self, eu);

        var edible = self.GetEdible();

        if (self == edible.sourceEgg && edible.shellCrack)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    edible.sourceEgg.shellpositions[i, j] = Vector2.zero;
                }
            }
        }
    }

    private static void NeedleEgg_DrawSprites(On.NeedleEgg.orig_DrawSprites orig, NeedleEgg self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
    {
        for (int i = 0; i < 2; i++)
        {
            sLeaser.sprites[self.halves[i].sprite].isVisible = !self.GetEdible().shellCrack;
        }
        orig(self, sLeaser, rCam, timeStacker, camPos);
    }
}