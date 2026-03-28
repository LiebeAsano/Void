#nullable enable
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoidTemplate.Defender;

public static class ViolenceTracking
{
	public static void Init()
	{
		On.Creature.Violence += CreatureOnViolence;
	}

	//sadly there's no such thing as weak reference hashset
	//using CWT as hashset instead
	//strongbox is useless here. no reason. at all. just language limitations
	private static ConditionalWeakTable<AbstractCreature, StrongBox<ushort>> guilt = new();

	private static void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionandmomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
	{
		bool wasDeadBeforeOrig = self.dead;
		orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
		if (wasDeadBeforeOrig != self.dead && self.dead)
		{
			if (!guilt.TryGetValue(self.abstractCreature, out _))
			{
				BlackPath(source);
			}
			else
			{
				
			}
		}
	}

	static void BlackPath(BodyChunk source)
	{
		Creature? villain = null;
		switch (source.owner)
		{
			case Weapon w:
				villain = w.thrownBy;
				break;
			case Creature c:
				villain = c;
				break;
		}

		if (villain is not null)
		{
			guilt.GetOrCreateValue(villain.abstractCreature);
		}
	}

	static void WhitePath(BodyChunk source)
	{
		//if done by player
		if (source.owner is Player p && p.abstractCreature.world.game.Players.Exists(x => x == p.abstractCreature))
		{
			
		}
	}
}