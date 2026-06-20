using MoreSlugcats;
using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class AnglerEaten
{
    public static void Hook()
    {
        On.Watcher.Angler.JawsSlamShut += Angler_JawsSlamShut;
        On.Watcher.Angler.Update += Angler_Update;
    }

    private sealed class AnglerState
    {
        public int killTimer = 0;
        public bool voidPoison = false;
    }

    private static readonly ConditionalWeakTable<Watcher.Angler, AnglerState> anglerStates = new();

    private static void Angler_JawsSlamShut(On.Watcher.Angler.orig_JawsSlamShut orig, Watcher.Angler self)
    {
        Vector2 a = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
        bool flag = false;
        for (int i = 0; i < self.room.physicalObjects.Length; i++)
        {
            for (int j = self.room.physicalObjects[i].Count - 1; j >= 0; j--)
            {
                if (self.room.physicalObjects[i][j] is not EnergyCell && (self.room.physicalObjects[i][j].abstractPhysicalObject.rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.physicalObjects[i][j].abstractPhysicalObject.rippleBothSides || self.abstractPhysicalObject.rippleBothSides))
                {
                    int k = 0;
                    while (k < self.room.physicalObjects[i][j].bodyChunks.Length)
                    {
                        BodyChunk bodyChunk = self.room.physicalObjects[i][j].bodyChunks[k];
                        Vector2 vector = Custom.ClosestPointOnLineSegment(self.firstChunk.pos, self.firstChunk.pos + a * 60f, bodyChunk.pos);
                        float num = Vector2.Distance(self.firstChunk.pos, vector) / 60f;
                        if (Vector2.Distance(vector, bodyChunk.pos) < bodyChunk.rad + num * 25f)
                        {
                            float num2 = 0f;
                            for (int l = 0; l < self.room.physicalObjects[i][j].bodyChunks.Length; l++)
                            {
                                num2 += self.room.physicalObjects[i][j].bodyChunks[l].rad;
                            }
                            if (num2 < 30f)
                            {
                                flag = true;
                                if (self.room.physicalObjects[i][j] is Creature creature)
                                {
                                    if (creature is Player player && player.AreVoidViy())
                                    {
                                        var state = anglerStates.GetOrCreateValue(self);
                                        state.voidPoison = true;
                                    }

                                    self.AI.tracker.ForgetCreature(creature.abstractCreature);
                                    creature.Die();
                                }
                                self.room.physicalObjects[i][j].Destroy();
                                break;
                            }
                            break;
                        }
                        else
                        {
                            k++;
                        }
                    }
                }
            }
        }
        int num3 = Random.Range(5, 11) * (flag ? 3 : 1);
        for (int m = 0; m < num3; m++)
        {
            Vector2 pos = self.firstChunk.pos + a * (80f * Random.value) + Random.insideUnitCircle * 20f;
            Vector2 vel = Random.insideUnitCircle * 10f;
            if (self.room.PointSubmerged(pos))
            {
                self.room.AddObject(new Bubble(pos, vel, false, false, false));
            }
            else
            {
                self.room.AddObject(new WaterDrip(pos, vel, false));
            }
        }
        if (flag)
        {
            self.room.AddObject(new ShockWave(self.firstChunk.pos + a * 40f, 170f, 0.015f, 7, false));
        }
    }

    private static void Angler_Update(On.Watcher.Angler.orig_Update orig, Watcher.Angler self, bool eu)
    {
        orig(self, eu);
        var state = anglerStates.GetOrCreateValue(self);
        if (state.voidPoison)
        {
            state.killTimer++;
            if (state.killTimer >= Random.Range(240, 440))
            {
                state.voidPoison = false;
                state.killTimer = 0;
                self.Die();
            }
        }
    }
}
