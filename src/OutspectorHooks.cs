using On.MoreSlugcats;

namespace VoidTemplate
{
    static class OutspectorHooks
    {
        internal static void Apply()
        {
            On.MoreSlugcats.InspectorAI.IUseARelationshipTracker_CreateTrackedCreatureState += InspectorAI_IUseARelationshipTracker_CreateTrackedCreatureState;
        }
        private static RelationshipTracker.TrackedCreatureState InspectorAI_IUseARelationshipTracker_CreateTrackedCreatureState(InspectorAI.orig_IUseARelationshipTracker_CreateTrackedCreatureState orig, MoreSlugcats.InspectorAI self, RelationshipTracker.DynamicRelationship rel)
        {
            if (rel.trackerRep is Tracker.ElaborateCreatureRepresentation)
            {
                if (rel.trackerRep.representedCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Attacks;
                }
                else

                if (rel.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplateType.Outspector)
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                    self.preyTracker.AddPrey(rel.trackerRep);
                }
                else

                if (rel.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplateType.OutspectorB)
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                    self.preyTracker.AddPrey(rel.trackerRep);
                }
                else
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Uncomfortable;
                }
            }
            return rel.state;
        }

    }
}
