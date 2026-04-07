#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MoreSlugcats;
using UnityEngine;
using static VoidTemplate.Defender.PunishmentExtensions;

namespace VoidTemplate.Defender;

public static class ViolenceTracking
{
	public static void Init()
	{
		On.Creature.Violence += CreatureOnViolence;
	}
	
	//every creature stores health as number between 0 and 1
	//its effective hp is decided by how much damage is mitigated by baseDamageResistance of CreatureTemplate
	//DLL - 200f
	//BLL - 100f
	//miros - 7f
	//centipede - 0.9f
	//slugcat - 1f
	//vulture - 8.5f
	//king vulture - 12.5f
	//leviathan - 1000f
	//deer - 200f
	const float UnkillableTreshold = 20f;

	//sadly there's no such thing as weak reference hashset
	//using CWT as hashset instead
	//strongbox is useless here. no reason. at all. just language limitations
	private static ConditionalWeakTable<AbstractCreature, StrongBox<ushort>> isGuiltyOfBadDeed = new();

	private static void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk damageDealer, Vector2? directionandmomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
	{
		bool wasDeadBeforeOrig = self.dead;
		orig(self, damageDealer, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
		//if creature was killed
		if (wasDeadBeforeOrig != self.dead && self.dead)
		{
			//creature was not bad
			if (!isGuiltyOfBadDeed.TryGetValue(self.abstractCreature, out _))
			{
				BlackPath(damageDealer);
			}
			//creature was bad
			else if (self.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.BigNeedleWorm)
			{
				WhitePath(damageDealer, self);
			}
		}
		//if saint or creature is too tough and it's carrying someone stunned, it counts as punished
		else if ((damageDealer.owner is Weapon { thrownBy: Player p } && p.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint 
		          || self.abstractCreature.creatureTemplate.baseDamageResistance >= UnkillableTreshold)
		         && self.grasps.Any(x => x?.grabbed is Creature { Stunned: true }))
		{
			WhitePath(damageDealer, self);
		}
	}

	static void BlackPath(BodyChunk whatDealtDamage)
	{
		Creature? villain = whatDealtDamage.owner switch
		{
			Weapon w => w.thrownBy,
			Creature c => c,
			_ => null
		};

		if (villain is not null)
		{
			isGuiltyOfBadDeed.GetOrCreateValue(villain.abstractCreature);
		}
	}

	static void WhitePath(BodyChunk whatDealtDamage, Creature whoWasHit)
	{
		Creature? hero = whatDealtDamage.owner switch
		{
			Weapon w => w.thrownBy,
			Creature c => c,
			_ => null
		};
		
		//if done by player
		if (hero is Player p 
		    && p.abstractCreature.world.game.Players.Exists(x => x == p.abstractCreature))
		{
			whoWasHit.abstractCreature.world.Punish(whoWasHit.abstractCreature.creatureTemplate.type);
		}
	}
}