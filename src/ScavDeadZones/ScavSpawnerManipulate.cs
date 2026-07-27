using MoreSlugcats;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.ScavDeadZones
{
    public class ScavSpawnerManipulate
    {
        public static void Hook()
        {
            On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;
        }

        private static void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            if (self.game.session is StoryGameSession story && story.saveState.TryGetScavRegionState(self.worldName, out var state))
            {
                for (int i = 0; i < self.spawners.Count; i++)
                {
                    if (self.spawners[i] is World.SimpleSpawner spawner && spawner.den.room == self.world.offScreenDen.index)
                    {
                        if (spawner.creatureType == CreatureTemplate.Type.Scavenger)
                        {
                            if (state.isDeadRegion)
                            {
                                spawner.amount = 0;
                            }
                            else
                            {
                                spawner.amount += state.migrationScavs;
                            }

                        }
                        else if (spawner.creatureType == DLCSharedEnums.CreatureTemplateType.ScavengerElite)
                        {
                            if (state.isDeadRegion)
                            {
                                spawner.amount = 0;
                            }
                            else
                            {
                                spawner.amount += state.migrationEleteScavs;
                            }
                        }
                    }
                }
                state.UpdateRegions(self.world);
            }
            orig(self, fresh);
        }
    }
}
