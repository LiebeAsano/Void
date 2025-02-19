using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

public static class AntiSpiderStun
{
	public static void Hook()
	{
		IL.DartMaggot.Update += DartMaggot_Update;
	}
	static void logerr(object e) => _Plugin.logger.LogError(e);
	//Dart Maggots, the things spider spitter (class bigspider) shoots, have two stuns: the one that happens each tick gradually increases with a maximum of 22 stun application (not += but =)
	//and then there's the "when destroyed, stun as much as the amount of darts in body, up to 4", which is 40*(2 + amount * 3) so from 200 to 560 stun
	//quick lookup didn't reveal anything special about as little as 22 stun, so this method only applies to the latter function, "stun when out of poison"
	private static void DartMaggot_Update(ILContext il)
	{
		ILCursor c = new(il);
		//stun = max( current stun,  40*(2 + amount*3) < if void multiply by karma resist coefficient  > )
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchMul()))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, DartMaggot, int>>((int orig, DartMaggot maggot) =>
			{
				if (maggot.stuckInChunk.owner is Player p && (p.IsVoid() || p.IsViy()))
				{
					var karma = p.KarmaCap;
					if (SaveManager.ExternalSaveData.VoidKarma11)
					{
                        return (int)((float)orig * 0.1f);
                    }
					return (int)((float)orig * (1f - 0.09f * karma));
				}
				return orig;
			});
		}
		else logerr(nameof(CreatureInteractions) + "." + nameof(AntiSpiderStun) + "." + nameof(DartMaggot_Update) + ": the IL hook making Void resistant to spider darts didn't find its place");
	}
}
