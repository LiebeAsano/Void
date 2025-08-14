using RWCustom;
using Smoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.OptionInterface;
using Watcher;

namespace VoidTemplate.CreatureInteractions;

public static class BlizzardBeamNerf
{
    public static void Hook()
    {
        On.Watcher.LizardBlizzardModule.Update += LizardBlizzardModule_Update;
    }

    private static void LizardBlizzardModule_Update(On.Watcher.LizardBlizzardModule.orig_Update orig, Watcher.LizardBlizzardModule self)
    {
        if (!OptionAccessors.NerfBlizzardLizard)
        {
            orig(self);
            return;
        }
        if (!self.lizard.Consious && UnityEngine.Random.value < 0.04f)
        {
            self.beamTimer = 900;
        }
        self.beamUpBobTime++;
        if (self.beamTimer > 0)
        {
            self.beamTimer++;
            if (!self.lizard.Consious || (self.lizard.AI != null && self.lizard.AI.preyTracker.MostAttractivePrey == null) || self.lizard.grasps[0] != null)
            {
                self.beamTimer += 5;
            }
            if (self.beamTimer >= 1000)
            {
                self.beamTimer = 0;
            }
        }
        float num = Mathf.Min((float)self.beamTimer / 85f, 1f);
        if (self.beamTimer <= 85 && self.beamTimer != 0)
        {
            num = Mathf.Pow(num, 3f) * 2f;
            if (UnityEngine.Random.value < num * 0.4f)
            {
                Vector2 pos = self.lizard.firstChunk.pos + Custom.DirVec(self.lizard.bodyChunks[1].pos, self.lizard.firstChunk.pos) * 40f;
                self.lizard.room.AddObject(new ShockWave(pos, 50f + 50f * num, 0.005f + 0.04f * num, 5, false));
            }
        }
        self.beamSound.Update();
        self.beamSound.pitch = 1f;
        self.beamSound.volume = (self.HasBeam ? 1f : 0f);
        
        self.beamSound.pos = self.lizard.bodyChunks[0].pos;

        if (self.lizard.graphicsModule != null)
        {
            if (self.breathSmoke == null)
            {
                self.breathSmoke = new BlizzardBeamSmoke(self.lizard.room);
                self.lizard.room.AddObject(self.breathSmoke);
            }
            Vector2 a = self.lizard.bodyChunks[0].pos;
            if (self.HasBeam)
            {
                Vector2 vector3 = self.RayTraceBeamHitPos();
                a = vector3;
                Vector2 a2 = Custom.DirVec(self.lizard.bodyChunks[0].pos, vector3);
                float num4 = Vector2.Distance(self.lizard.bodyChunks[0].pos, vector3);
                for (int m = 0; m < 5; m++)
                {
                    self.breathSmoke.EmitSmoke(self.lizard.firstChunk.pos, Custom.DirVec(self.lizard.bodyChunks[1].pos, self.lizard.bodyChunks[0].pos) * (30f + 20f * UnityEngine.Random.value) + UnityEngine.Random.insideUnitCircle * 10f, UnityEngine.Random.value < 0.5f, 50f);
                }
                for (int n = 0; n < 4; n++)
                {
                    self.breathSmoke.EmitSmoke(vector3, Custom.DirVec(vector3, self.lizard.bodyChunks[1].pos) * (5f + 20f * UnityEngine.Random.value) + UnityEngine.Random.insideUnitCircle * 5f, UnityEngine.Random.value < 0.5f, 30f);
                }
                for (int num5 = 0; num5 < self.lizard.room.abstractRoom.creatures.Count; num5++)
                {
                    if (self.lizard.room.abstractRoom.creatures[num5].realizedCreature != null && self.lizard.room.abstractRoom.creatures[num5].realizedCreature != self.lizard)
                    {
                        Creature realizedCreature = self.lizard.room.abstractRoom.creatures[num5].realizedCreature;
                        float num6 = 1000f;
                        int num7 = -1;
                        for (int num8 = 0; num8 < realizedCreature.bodyChunks.Length; num8++)
                        {
                            float num9 = Vector2.Distance(Custom.ClosestPointOnLineSegment(self.lizard.bodyChunks[0].pos, vector3, realizedCreature.bodyChunks[num8].pos), realizedCreature.bodyChunks[num8].pos) - realizedCreature.bodyChunks[num8].rad;
                            if (num9 < num6)
                            {
                                num6 = num9;
                                num7 = num8;
                            }
                        }
                        if (num7 != -1 && num6 < 10f && Vector2.Distance(self.lizard.bodyChunks[0].pos, realizedCreature.bodyChunks[num7].pos) < num4)
                        {
                            if (realizedCreature is Player && UnityEngine.Random.value < 0.2f)
                            {
                                realizedCreature.Stun(300);
                                realizedCreature.Hypothermia += 0.3f;
                            }
                            else
                            {
                                realizedCreature.Violence(self.lizard.mainBodyChunk, new Vector2?(a2 * 4f), realizedCreature.bodyChunks[num7], null, Creature.DamageType.Electric, 0.1f, 0f);
                                realizedCreature.Hypothermia += 0.1f;
                            }
                            for (int num10 = 0; num10 < 2; num10++)
                            {
                                self.breathSmoke.EmitSmoke(realizedCreature.bodyChunks[num7].pos, UnityEngine.Random.insideUnitCircle * 15f, UnityEngine.Random.value < 0.5f, 30f);
                            }
                            a = realizedCreature.bodyChunks[num7].pos;
                        }
                    }
                }
                for (int num11 = 0; num11 < self.lizard.room.updateList.Count; num11++)
                {
                    if (self.lizard.room.updateList[num11] is CosmeticSprite && !(self.lizard.room.updateList[num11] is SmokeSystem) && !self.IsForbiddenToPull(self.lizard.room.updateList[num11]))
                    {
                        CosmeticSprite cosmeticSprite2 = self.lizard.room.updateList[num11] as CosmeticSprite;
                        float num12 = Vector2.Distance(Custom.ClosestPointOnLineSegment(self.lizard.bodyChunks[0].pos, vector3, cosmeticSprite2.pos), cosmeticSprite2.pos);
                        if (num12 < 100f)
                        {
                            cosmeticSprite2.vel += a2 * 5f * (1f - num12 / 100f);
                        }
                    }
                }
            }
            self.beamHitSound.Update();
            self.beamHitSound.pos = Vector2.Lerp(a, self.lizard.firstChunk.pos, 0.5f);
            self.beamHitSound.pitch = 1f;
            self.beamHitSound.volume = (self.HasBeam ? 1f : 0f);
        }
        else
        {
            self.beamHitSound.Update();
            self.beamHitSound.pos = self.lizard.bodyChunks[0].pos;
            self.beamHitSound.volume = 0f;
        }
        if (self.breathSmoke != null && (self.breathSmoke.slatedForDeletetion || self.breathSmoke.room != self.lizard.room))
        {
            self.breathSmoke = null;
        }
    }
}
