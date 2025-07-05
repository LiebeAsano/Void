using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using VoidTemplate.Objects.NoodleEgg;
using VoidTemplate.Oracles;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static MonoMod.InlineRT.MonoModRule;
using static VoidTemplate.SaveManager;
namespace VoidTemplate.PlayerMechanics;

public static class SwallowObjects
{
    public static void Hook()
    {
        On.Player.SwallowObject += Player_SwallowObject;
        On.Player.Regurgitate += Player_Regurgitate;
        On.SlugcatHand.Update += SlugcatHand_Update;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
        On.Player.MaulingUpdate += Player_MaulingUpdate;
        On.Player.ctor += Player_ctor;
        On.StoryGameSession.ctor += StoryGameSession_ctor;
    }

    private static readonly HashSet<Type> HalfFoodObjects =
        [
            typeof(Hazer),
            typeof(VultureGrub),
        ];

    private static readonly HashSet<Type> QuarterFoodObjects =
    [
        typeof(WaterNut),
        typeof(FirecrackerPlant),
        typeof(FlyLure),
        typeof(FlareBomb),
        typeof(PuffBall),
        typeof(FlyLure),
        typeof(BubbleGrass),
        typeof(Lantern),
    ];

    private static readonly HashSet<Type> FullPinFoodObjects =
    [
        typeof(SporePlant),
        ];


    private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        AbstractPhysicalObject abstractGrabbed = self.grasps[grasp]?.grabbed?.abstractPhysicalObject;

        if (self.IsVoid() || self.IsViy())
        {
            var grabbed = self.grasps[grasp]?.grabbed;

            var game = self.abstractCreature.world.game;

            bool hasMark = game.IsStorySession && (game.GetStorySession.saveState.deathPersistentSaveData.theMark);

            if (grabbed != null)
            {
                if (QuarterFoodObjects.Contains(grabbed.GetType()))
                {
                    HandleQuarterFood(orig, self, grasp, abstractGrabbed);
                    return;
                }
                else if (FullPinFoodObjects.Contains(grabbed.GetType()))
                {
                    HandleFullPinFood(orig, self, grasp, abstractGrabbed);
                    return;
                }
                else if (HalfFoodObjects.Contains(grabbed.GetType()))
                {
                    HandleHalfFood(orig, self, grasp, abstractGrabbed);
                    return;
                }
                else if (self.KarmaCap != 10 && !self.IsViy() && !Karma11Update.VoidKarma11)
                {
                    if (self.room != null && self.grasps[grasp].grabbed is PebblesPearl && hasMark &&
                        self.room.updateList.Any(i => i is Oracle oracle && oracle.oracleBehavior is SSOracleBehavior behavior && behavior.action != SSOracleBehavior.Action.ThrowOut_ThrowOut && behavior.action != SSOracleBehavior.Action.ThrowOut_KillOnSight))
                    {
                        ((self.room.updateList.First(i => i is Oracle) as Oracle)
                        .oracleBehavior as SSOracleBehavior).EatPearlsInterrupt();
                    }
                }
            }
        }

        orig(self, grasp);
    }

    private static void HandleQuarterFood(On.Player.orig_SwallowObject orig, Player self, int grasp, AbstractPhysicalObject abstractGrabbed)
    {
        orig(self, grasp);

        if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
        {
            if (self.slugcatStats.foodToHibernate > self.FoodInStomach)
            {
                self.objectInStomach.Destroy();
                self.objectInStomach = null;
                if (!self.IsViy())
                    self.AddQuarterFood();
            }
            if (self.room.game.IsArenaSession && !self.IsViy())
            {
                self.objectInStomach.Destroy();
                self.objectInStomach = null;
                self.AddFood(1);
            }
        }
    }

    private static void HandleFullPinFood(On.Player.orig_SwallowObject orig, Player self, int grasp, AbstractPhysicalObject abstractGrabbed)
    {
        orig(self, grasp);

        if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
        {
            if (self.slugcatStats.foodToHibernate > self.FoodInStomach)
            {
                self.objectInStomach.Destroy();
                self.objectInStomach = null;
                if (OptionInterface.OptionAccessors.SimpleFood && !self.room.game.IsArenaSession)
                    self.AddFood(2);
                else
                    self.AddFood(1);
            }
        }
    }

    private static void HandleHalfFood(On.Player.orig_SwallowObject orig, Player self, int grasp, AbstractPhysicalObject abstractGrabbed)
    {
        orig(self, grasp);

        if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
        {
            if (self.slugcatStats.foodToHibernate > self.FoodInStomach)
            {
                self.objectInStomach.Destroy();
                self.objectInStomach = null;
                if (!self.IsViy())
                    if (OptionInterface.OptionAccessors.SimpleFood || self.room.game.IsArenaSession)
                        self.AddFood(1);
                    else
                    {
                        self.AddQuarterFood();
                        self.AddQuarterFood();
                    }
            }
        }
    }

    public static Dictionary<int, List<string>> pearlIDsInPlayerStomaches = new();

    private static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
    {
        if (self.SlugCatClass == VoidEnums.SlugcatID.Void 
            && pearlIDsInPlayerStomaches[self.playerState.playerNumber] is not null
            && pearlIDsInPlayerStomaches[self.playerState.playerNumber].Count > 0
            && !Karma11Update.VoidKarma11)
        {
                string pearlToSpit = pearlIDsInPlayerStomaches[self.playerState.playerNumber][pearlIDsInPlayerStomaches.Count - 1];
                pearlIDsInPlayerStomaches[self.playerState.playerNumber].RemoveAt(pearlIDsInPlayerStomaches.Count - 1);
                self.objectInStomach = new DataPearl.AbstractDataPearl(world: self.abstractCreature.world,
                    objType: AbstractPhysicalObject.AbstractObjectType.DataPearl, 
                    realizedObject: null,
                    pos: self.abstractCreature.pos,
                    ID: self.abstractCreature.world.game.GetNewID(),
                    originRoom: -1,
                    placedObjectIndex: -1,
                    consumableData: null,
                    dataPearlType: new DataPearl.AbstractDataPearl.DataPearlType(pearlToSpit)
                ); 
                self.abstractCreature.world.game.GetStorySession?.saveState?.SetStomachPearls(pearlIDsInPlayerStomaches);
        }
        orig(self);
    }

    private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        orig(self);

        if (self.owner.owner is Player player && (player.IsVoid() || player.IsViy()))
        {
            if (player.swallowAndRegurgitateCounter > 10)
            {
                int num3 = -1;
                int num4 = 0;

                while (num3 < 0 && num4 < 2)
                {
                    if (player.grasps[num4] != null && player.CanBeSwallowed(player.grasps[num4].grabbed))
                    {
                        num3 = num4;
                    }
                    num4++;
                }

                if (num3 == self.limbNumber || player.craftingObject)
                {
                    float num5 = Mathf.InverseLerp(10f, 90f, player.swallowAndRegurgitateCounter);

                    if (num5 < 0.5f)
                    {
                        self.relativeHuntPos *= Mathf.Lerp(0.9f, 0.7f, num5 * 2f);
                        self.relativeHuntPos.y += Mathf.Lerp(2f, 4f, num5 * 2f);
                        self.relativeHuntPos.x *= Mathf.Lerp(1f, 1.2f, num5 * 2f);
                    }
                    else
                    {
                        self.relativeHuntPos = new Vector2(0f, -4f) + Custom.RNV() * 2f * UnityEngine.Random.value * Mathf.InverseLerp(0.5f, 1f, num5);

                        (self.owner as PlayerGraphics).head.vel += Custom.RNV() * 2f * UnityEngine.Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                        player.bodyChunks[0].vel += Custom.RNV() * 0.2f * UnityEngine.Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                    }
                }
            }
        }
    }

    private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);
        if (self.player.IsVoid())
        {
            if ((self.player.objectInStomach != null || pearlIDsInPlayerStomaches[self.player.playerState.playerNumber].Count > 0) && self.swallowing <= 0 && self.player.swallowAndRegurgitateCounter > 0)
            {
                if (self.player.swallowAndRegurgitateCounter > 30)
                {
                    self.blink = 5;
                }
                float num11 = Mathf.InverseLerp(0f, 110f, (float)self.player.swallowAndRegurgitateCounter);
                float num12 = (float)self.player.swallowAndRegurgitateCounter / Mathf.Lerp(30f, 15f, num11);
                if (self.player.standing)
                {
                    self.drawPositions[0, 0].y += Mathf.Sin(num12 * 3.1415927f * 2f) * num11 * 2f;
                    self.drawPositions[1, 0].y += -Mathf.Sin((num12 + 0.2f) * 3.1415927f * 2f) * num11 * 3f;
                }
                else
                {
                    self.drawPositions[0, 0].y += Mathf.Sin(num12 * 3.1415927f * 2f) * num11 * 3f;
                    self.drawPositions[0, 0].x += Mathf.Cos(num12 * 3.1415927f * 2f) * num11 * 1f;
                    self.drawPositions[1, 0].y += Mathf.Sin((num12 + 0.2f) * 3.1415927f * 2f) * num11 * 2f;
                    self.drawPositions[1, 0].x += -Mathf.Cos(num12 * 3.1415927f * 2f) * num11 * 3f;
                }
            }
        }
    }

    private static void Player_MaulingUpdate(On.Player.orig_MaulingUpdate orig, Player self, int graspIndex)
    {
        if (self.grasps[graspIndex] != null && self.IsVoid())
        {
            if (self.maulTimer > 15)
            {
                if (self.grasps[graspIndex].grabbed is Creature && (self.grasps[graspIndex].grabbed as Creature).abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
                {
                    self.grasps[graspIndex].grabbed.bodyChunks[0].mass = 0.5f;
                    self.grasps[graspIndex].grabbed.bodyChunks[1].mass = 0.3f;
                    self.grasps[graspIndex].grabbed.bodyChunks[2].mass = 0.05f;
                }
                self.standing = false;
                self.Blink(5);
                if (self.maulTimer % 3 == 0)
                {
                    Vector2 b = Custom.RNV() * 3f;
                    self.mainBodyChunk.pos += b;
                    self.mainBodyChunk.vel += b;
                }
                Vector2 vector = self.grasps[graspIndex].grabbedChunk.pos * self.grasps[graspIndex].grabbedChunk.mass;
                float num = self.grasps[graspIndex].grabbedChunk.mass;
                for (int i = 0; i < self.grasps[graspIndex].grabbed.bodyChunkConnections.Length; i++)
                {
                    if (self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1 == self.grasps[graspIndex].grabbedChunk)
                    {
                        vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.mass;
                        num += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.mass;
                    }
                    else if (self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2 == self.grasps[graspIndex].grabbedChunk)
                    {
                        vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.mass;
                        num += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.mass;
                    }
                }
                vector /= num;
                self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.5f;
                self.bodyChunks[1].vel -= Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.6f;
                if (self.graphicsModule != null)
                {
                    if (!Custom.DistLess(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos, self.grasps[graspIndex].grabbedChunk.rad))
                    {
                        (self.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos) * (self.grasps[graspIndex].grabbedChunk.rad - Vector2.Distance(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos));
                    }
                    else if (self.maulTimer % 5 == 3)
                    {
                        (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
                    }
                    if (self.maulTimer > 10 && self.maulTimer % 8 == 3) 
                    {
                        self.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * 4f;
                        self.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector, self.mainBodyChunk.pos) * 0.9f / self.grasps[graspIndex].grabbedChunk.mass;
                        for (int j = UnityEngine.Random.Range(0, 3); j >= 0; j--)
                        {
                            self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(vector, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
                        }
                        return;
                    }
                }
            }
        }
        else
        {
            orig(self, graspIndex);
        }
    }


    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (self.IsVoid())
        {
            if (!pearlIDsInPlayerStomaches.TryGetValue(self.playerState.playerNumber, out var pearl))
            {
                pearlIDsInPlayerStomaches[self.playerState.playerNumber] = [];
            }
        }
        
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        pearlIDsInPlayerStomaches = self.saveState.GetStomachPearls();
    }
}

