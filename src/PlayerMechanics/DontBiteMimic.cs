using Mono.Cecil.Cil;
using MonoMod.Cil;
using SlugBase.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class DontBiteMimic
{
    public static void Hook()
    {
        IL.Player.UpdateAnimation += DontBite_Mimic;
    }

    private static void DontBite_Mimic(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.After,
                i => i.MatchCallvirt<ClimbableVinesSystem>("VineCurrentlyClimbable"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, Player, bool>>((re, self) =>
            {
                if (self.IsVoid() &&
                    self.room.climbableVines.vines[self.vinePos.vine] is PoleMimic)
                    return false;
                return re;
            });
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
