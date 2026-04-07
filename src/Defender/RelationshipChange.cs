namespace VoidTemplate.Defender;

public class RelationshipChange
{
	public void Init()
	{
		On.RelationshipTracker.DynamicRelationship.Update += DynamicRelationshipOnUpdate;
		On.ArtificialIntelligence.Update += ArtificialIntelligenceOnUpdate;
	}

	void ArtificialIntelligenceOnUpdate(On.ArtificialIntelligence.orig_Update orig, ArtificialIntelligence self)
	{
		orig(self);
		if (self.utilityComparer?.HighestUtilityModule() is PreyTracker preyTracker
		    && preyTracker.MostAttractivePrey.representedCreature.abstractAI.RealAI is ArtificialIntelligence preyAI
		    && preyAI.utilityComparer?.HighestUtilityModule() is ThreatTracker threatTracker
		    && IsDefender(threatTracker.mostThreateningCreature.representedCreature))
		{
			//make aggression higher than fear?
			//research aggression tracker
			//self.agressionTracker.
		}
	}

	void DynamicRelationshipOnUpdate(On.RelationshipTracker.DynamicRelationship.orig_Update orig, RelationshipTracker.DynamicRelationship self)
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

	bool IsDefender(AbstractCreature creature) =>
		creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat
		&& creature?.realizedCreature is Player player
		&& player.slugcatStats.name == VoidEnums.SlugcatID.Defender;
}