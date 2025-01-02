using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics;

internal static class CentipedeResist
{
    public static void Hook()
    {
        On.Centipede.Shock += Centipede_Shock;
    }

    private static void Centipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
    {
        if (shockObj is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as CentipedeGraphics).lightFlash = 1f;
                for (int i = 0; i < (int)Mathf.Lerp(4f, 8f, self.size); i++)
                {
                    self.room.AddObject(new Spark(self.HeadChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
                }
            }
            for (int j = 0; j < self.bodyChunks.Length; j++)
            {
                self.bodyChunks[j].vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                self.bodyChunks[j].pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            }
            for (int k = 0; k < shockObj.bodyChunks.Length; k++)
            {
                shockObj.bodyChunks[k].vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                shockObj.bodyChunks[k].pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            }
            if (self.AquaCenti)
            {
                if (shockObj is Creature)
                {
                    self.Stun(120);
                    self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                    self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                    (shockObj as Creature).Stun(240);
                    self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                    (shockObj as Creature).LoseAllGrasps();
                }
                return;
            }
            if (self.Red)
            {
                int RandomDeath = UnityEngine.Random.Range(0, 2);
                if ((shockObj as Player).KarmaCap == 10)
                {
                    if (RandomDeath == 0)
                    {
                        (shockObj as Creature).Die();
                        self.Stun(120);
                        self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                        self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 2f, 200f);
                    }
                    else
                    {
                        (shockObj as Creature).Stun(240);
                        self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                        (shockObj as Creature).LoseAllGrasps();
                        self.Stun(240);
                        self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                        self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 2f, 200f);
                    }
                }
                else
                {
                    (shockObj as Creature).Die();
                    self.Stun(120);
                    self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                    self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 2f, 200f);
                }
                return;
            }
            if (shockObj is Creature)
            {
                if (self.Small)
                {
                    self.Die();
                }
                else if (shockObj.TotalMass < self.TotalMass)
                {
                    int NumberDeath = (shockObj as Player).KarmaCap;
                    if ((shockObj as Player).KarmaCap == 10)
                        NumberDeath = 9;
                    int RandomDeath = UnityEngine.Random.Range(0, 10);
                    if (NumberDeath < 5)
                    {
                        if (RandomDeath > 4)
                        {
                            (shockObj as Creature).Die();
                            self.Stun(60);
                            self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                        else
                        {
                            (shockObj as Creature).Stun(120);
                            self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                            (shockObj as Creature).LoseAllGrasps();
                            self.Stun(120);
                            self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                    }
                    if (NumberDeath == 6)
                    {
                        if (RandomDeath > 5)
                        {
                            (shockObj as Creature).Die();
                            self.Stun(60);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                        else
                        {
                            (shockObj as Creature).Stun(120);
                            self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                            (shockObj as Creature).LoseAllGrasps();
                            self.Stun(120);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                    }
                    if (NumberDeath == 7)
                    {
                        if (RandomDeath > 6)
                        {
                            (shockObj as Creature).Die();
                            self.Stun(60);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), (shockObj as Creature).mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                        else
                        {
                            (shockObj as Creature).Stun(120);
                            self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                            (shockObj as Creature).LoseAllGrasps();
                            self.Stun(120);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                    }
                    if (NumberDeath == 8)
                    {
                        if (RandomDeath > 7)
                        {
                            (shockObj as Creature).Die();
                            self.Stun(60);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), (shockObj as Creature).mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                        else
                        {
                            (shockObj as Creature).Stun(120);
                            self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                            (shockObj as Creature).LoseAllGrasps();
                            self.Stun(120);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                    }
                    if (NumberDeath == 9)
                    {
                        if (RandomDeath > 8)
                        {
                            (shockObj as Creature).Die();
                            self.Stun(60);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), (shockObj as Creature).mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                        else
                        {
                            (shockObj as Creature).Stun(120);
                            self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                            (shockObj as Creature).LoseAllGrasps();
                            self.Stun(120);
                            self.room.AddObject(new CreatureSpasmer(self, true, (int)Mathf.Lerp(70f, 120f, self.size)));
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                        }
                    }
                    return;
                }
                else
                {
                    (shockObj as Creature).Stun(60);
                    self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                    (shockObj as Creature).LoseAllGrasps();
                    self.Stun(120);
                    self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                    self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), (shockObj as Creature).mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
                    self.shockGiveUpCounter = Math.Max(self.shockGiveUpCounter, 30);
                    self.AI.annoyingCollisions = Math.Min(self.AI.annoyingCollisions / 2, 150);
                    return;
                }
            }
        }
        else
        {
            orig(self, shockObj);
        }
    }

}
