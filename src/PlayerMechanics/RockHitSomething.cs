using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class RockHitSomething
{
	public static void Hook()
	{
		On.Rock.HitSomething += Rock_HitSomething_Update;
	}

	private static bool Rock_HitSomething_Update(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
	{
		if (self.thrownBy is Player player
			&& player.IsVoid()
			&& result.obj is Creature creature)
		{
			string creatureTypeName = creature.Template.type.ToString();

			string[] excludedCreatureTypes = [
					"Vulture",
					"BrotherLongLegs",
					"DaddyLongLegs",
					"BigEel",
					"PoleMimic",
					"TentaclePlant",
					"MirosBird",
					"RedLizard",
					"KingVulture",
					"Centipede",
					"RedCentipede",
					"TempleGuard",
					"Deer",
					"MirosVulture",
					"HunterDaddy",
					"ScavengerKing",
					"TrainLizard",
					"Inspector",
					"TerrorLongLegs",
					"AquaCenti",
					"StowawayBug"
			];

			if (Array.IndexOf(excludedCreatureTypes, creatureTypeName) == -1)
			{
				creature.Stun(69);
			}
		}

		return orig(self, result, eu);
	}
}
