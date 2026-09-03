using MoreSlugcats;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.ScavDeadZones
{
    public class ScavHooks
    {
        public static void Hook()
        {
            On.SocialEventRecognizer.Killing += SocialEventRecognizer_Killing;
            On.ScavengerAI.PlayerRelationship += ScavengerAI_PlayerRelationship;
        }

        private static void SocialEventRecognizer_Killing(On.SocialEventRecognizer.orig_Killing orig, SocialEventRecognizer self, Creature killer, Creature victim)
        {
            orig(self, killer, victim);

            if (killer is not Player || victim is not Scavenger scav || self.room.game.session is not StoryGameSession story) return;
            
            var saveState = story.saveState;
            var world = self.room.world;
            var state = saveState.GetOrCreateScavRegionState(world.name, world);

            if (state.deadCount <= 0f) return;

            float decrease = scav.Elite ? 0.1f : 0.04f;

            if (saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer || 
                saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear) decrease *= 0.5f;

            state.deadCount = Mathf.Clamp01(state.deadCount - decrease);
            state.killScavs = true;
        }

        private static CreatureTemplate.Relationship ScavengerAI_PlayerRelationship(On.ScavengerAI.orig_PlayerRelationship orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (dRelation.trackerRep.representedCreature.realizedCreature is not Player player) return orig(self, dRelation);

            if (player.IsViy()) return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f);

            var world = self.scavenger.abstractCreature.world;

            if (world.game.session is StoryGameSession story && story.saveState.TryGetScavRegionState(world.name, out var state) && state.isDeadRegion)
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f);

            return orig(self, dRelation);
        }
    }
}
