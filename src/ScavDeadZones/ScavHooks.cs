using MoreSlugcats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (killer is Player && victim is Scavenger scav
                && self.room.game.session is StoryGameSession story)
            {
                var state = story.saveState.GetOrCreateScavRegionState(self.room.world.name, self.room.world);
                if (state.deadCount > 0)
                {
                    state.deadCount = Mathf.Clamp01(state.deadCount - 
                        (story.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer ||
                         story.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear ? 
                         scav.Elite ? 0.05f : 0.02f :
                         scav.Elite ? 0.1f : 0.04f));
                    state.killScavs = true;
                }
            }
        }

        private static CreatureTemplate.Relationship ScavengerAI_PlayerRelationship(On.ScavengerAI.orig_PlayerRelationship orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if ((dRelation.trackerRep.representedCreature.realizedCreature is Player player && player.IsViy()) ||
                (self.scavenger.abstractCreature.world.game.session is StoryGameSession story &&
                story.saveState.TryGetScavRegionState(self.scavenger.abstractCreature.world.name, out var state) && state.isDeadRegion))
            {
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f);
            }
            return orig(self, dRelation);
        }
    }
}
