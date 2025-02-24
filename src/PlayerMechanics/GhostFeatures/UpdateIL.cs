using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

internal static class UpdateIL
{
	public static void Hook()
	{
		IL.Ghost.Update += Ghost_UpdateIL;
	}

	private static void Ghost_UpdateIL(ILContext il)
	{
		try
		{
			ILCursor c = new ILCursor(il);
			if (c.TryGotoNext(MoveType.After, i => i.MatchLdfld<StoryGameSession>("saveStateNumber"),
				i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
				i => i.MatchCall(out var call) && call.Name.Contains("op_Equality")))
			{
				var label = (ILLabel)c.Next.Operand;
				Debug.Log(label.Target);
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<bool, Ghost, bool>>((re, self) =>
					re || ((self.room.game.session is StoryGameSession session) &&
						   session.saveStateNumber == VoidEnums.SlugcatID.Void));
			}
			else
			{
                LogExErr("&IL.Ghost.Update += Ghost_UpdateIL error IL Hook");
            }

        }
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
}
