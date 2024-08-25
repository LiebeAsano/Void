using MonoMod.Cil;
using static VoidTemplate.Useful.Utils;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Cecil.Cil;
using static Creature;

namespace VoidTemplate.PlayerMechanics
{
	internal static class HealthSpear
	{
		public static void Hook()
		{
			IL.Spear.HitSomething += Spear_HitSomething;
		}

		public static void Spear_HitSomething(ILContext il)
		{
			ILCursor c = new(il);
			//Creature.Violence(1 <OR .99 IF VOID>);
			if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(typeof(Creature.DamageType).GetField(nameof(Creature.DamageType.Stab))),
				x => x.MatchLdloc(2)))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_1);
				c.EmitDelegate<Func<float, Spear, SharedPhysics.CollisionResult, float>>((float orig, Spear self, SharedPhysics.CollisionResult result) =>
				{
					if (self.thrownBy is Scavenger && result.obj is Player p && p.IsVoid()) return 0.99f;
					return orig;
				});
			}
			else
				logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(HealthSpear)}.{nameof(Spear_HitSomething)}: match for permanent damage tracking failed");

			//permanent damage cap 1.25 instead of 1 for void
			if (c.TryGotoNext(MoveType.After,
				x => x.MatchLdfld<global::PlayerState>(nameof(global::PlayerState.permanentDamageTracking)),
				x => x.MatchLdcR8(1d)))
			{
				c.Emit(OpCodes.Ldloc_3);
				c.EmitDelegate<Func<double, Player, double>>((double orig, Player p) =>
				{
					if (p.IsVoid() && p.KarmaCap == 10)
						return 1.25;
					else
						return orig;
				}); 
			}
			else
				logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(HealthSpear)}.{nameof(Spear_HitSomething)}: match for permanent damage tracking failed");
		}
	}
}
