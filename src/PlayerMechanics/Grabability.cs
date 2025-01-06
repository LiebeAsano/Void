using CoralBrain;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class Grabability
{
	public static void Hook()
	{
        //prevents grabbing pole plant for void
        //IL.Player.MovementUpdate += Player_Movement;
        On.Player.Grabability += Player_Grabability;
		//allows hand switching when holding big object
        //IL.Player.GrabUpdate += Player_GrabUpdate;
	}

    private static void Player_Movement(ILContext il)
    {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNext(
            i => i.MatchLdflda<PhysicalObject>("dynamicRunSpeed"),
            i => i.MatchLdcR4(3.6f),
            i => i.MatchStfld<float[]>("[1]")
            ))
        {
            cursor.Index += 3;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(self =>
            {
                if (self.slugcatStats.name == VoidEnums.SlugcatID.Void)
                {
                    self.dynamicRunSpeed[0] *= 10f;
                    self.dynamicRunSpeed[1] *= 10f;
                }
            });
        }
    }

    private static void Player_GrabUpdate(MonoMod.Cil.ILContext il)
    {
		ILCursor c = new(il);
		ILLabel skipGrababilityCheck = c.DefineLabel();
		if (c.TryGotoNext(x => x.MatchCall(typeof(Player).GetMethod(nameof(Player.Grabability), bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)))
			&& c.TryGotoNext(MoveType.After, x => x.MatchLdcI4(3))
			&& c.TryGotoPrev(MoveType.After, x => x.MatchBrfalse(out skipGrababilityCheck)))
		{
			LogExInf("applying hooke");
			c.Emit(OpCodes.Ldarg, 0);
			c.EmitDelegate<Predicate<Player>>((player) => player.IsVoid());
			c.Emit(OpCodes.Brtrue, skipGrababilityCheck);
		}
		else LogExErr("search for grabability check failed. Void won't be able to swap hands with heavy objects");
    }

    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
	{
		if (self.IsVoid() &&(obj is PoleMimic || obj is TentaclePlant))
			return Player.ObjectGrabability.CantGrab;
		return orig(self, obj);
	}
	
}
