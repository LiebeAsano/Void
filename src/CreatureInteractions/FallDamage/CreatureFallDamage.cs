using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.CreatureInteractions.FallDamage
{
    public class CreatureFallDamage
    {
        public static void Hook()
        {
            On.Creature.TerrainImpact += Creature_TerrainImpact;
        }

        private static void Creature_TerrainImpact(On.Creature.orig_TerrainImpact orig, Creature self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            orig(self, chunk, direction, speed, firstContact);
            if (firstContact)
            {
                float softSpeed = 3;
                float mediumSpeed = 16;
                float hardSpeed = 35;
                float deathSpeed = 60;

                if (self is not Player && self is not StowawayBug)
                {
                    BodyChunk bodyChunk = self.bodyChunks[chunk];
                    if (speed > deathSpeed && direction.y < 0 && self.grabbedBy.Count == 0)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                        self.Die();
                    }
                    else if (speed > hardSpeed)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
                        float stunDamage = Custom.LerpMap(speed, hardSpeed, deathSpeed, 40, 140, 2.5f);
                        self.Violence(null, direction.ToVector2(), bodyChunk, null, Creature.DamageType.Blunt, stunDamage / 100, stunDamage);
                    }
                    else if (speed < softSpeed)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Light, self.mainBodyChunk, false, Mathf.InverseLerp(0f, 2f, speed), 3f);
                    }
                    else if (speed < mediumSpeed)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk);
                    }
                    else
                    {
                        self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
                    }
                }
            }
        }
    }
}
