using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

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

            if (c.TryGotoNext(MoveType.After,
                i => i.MatchCallvirt<ClimbableVinesSystem>("VineCurrentlyClimbable")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, Player, bool>>((re, self) =>
                {
                    if (self.IsVoid() || self.IsViy())
                    {
                        var vine = self.room.climbableVines.GetVineObject(self.vinePos);
                        if (vine is PoleMimic)
                            return false;
                    }
                    return re;
                });
            }
            else
            {
                LogExErr("&IL.Player.UpdateAnimation += DontBite_Mimic error IL Hook");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
