using System.Linq;

namespace VoidTemplate.Defender;

public static class RelationshipChange
{
	public static void Init()
	{
		On.RelationshipTracker.DynamicRelationship.Update += DynamicRelationshipOnUpdate;
		On.ArtificialIntelligence.Update += ArtificialIntelligenceOnUpdate;
	}

	static void ArtificialIntelligenceOnUpdate(On.ArtificialIntelligence.orig_Update orig, ArtificialIntelligence self)
	{
		orig(self);
		//if hunting at someone who is afraid of defender:
		if (self.utilityComparer?.HighestUtilityModule() is PreyTracker preyTracker
		    && preyTracker.MostAttractivePrey.representedCreature.abstractAI.RealAI is ArtificialIntelligence preyAI
		    && preyAI.utilityComparer?.HighestUtilityModule() is ThreatTracker threatTracker
		    && IsDefender(threatTracker.mostThreateningCreature.representedCreature))
		{
			//EVEN UTILITY COMPARER IS NOT GRANTED
			//some simple creatures like ripple spider don't even compare utilities
			AIModule? highestUtilityModule = self.utilityComparer?.HighestUtilityModule();
			float? highestAmount = self.utilityComparer?.HighestUtility();
			
			if(highestUtilityModule is AgressionTracker t && IsDefender(t.highestAgressionTarget.crit.representedCreature)) return;
			
			if(highestUtilityModule is null) return;
			
			if (self.modules.OfType<AgressionTracker>().FirstOrDefault() is { } aggressionTracker
			    && aggressionTracker.creatures.FirstOrDefault(x => IsDefender(x.crit.representedCreature)) is {} trackedDefender)
			{
				trackedDefender.anger =
					(float)highestAmount! * (highestUtilityModule is AgressionTracker ? 1f : 1.2f);
			}
			//
			//make aggression higher than fear?
			//research aggression tracker
			//self.agressionTracker.
		}
	}

	static void DynamicRelationshipOnUpdate(On.RelationshipTracker.DynamicRelationship.orig_Update orig, RelationshipTracker.DynamicRelationship self)
	{
		orig(self);
		//case: creature tracks defender, needs to be afraid of it
		if (IsDefender(self.trackerRep.representedCreature)
		    && self.currentRelationship.type != CreatureTemplate.Relationship.Type.Afraid && self.trackedByModule.AI.creature.IsAfraidOfDefender())
		{
			//lizards start with strength of 1.8 for normal vulture mask and 4.2 for king vulture and fall to 0 within next 700 (KV) to 1200 (V) ticks
			self.currentRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f);
		}
	}

	static bool IsDefender(AbstractCreature creature) =>
		creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat
		&& creature?.realizedCreature is Player player
		&& player.slugcatStats.name == VoidEnums.SlugcatID.Defender;
}