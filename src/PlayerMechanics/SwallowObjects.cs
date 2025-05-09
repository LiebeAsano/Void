using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using VoidTemplate.Oracles;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static MonoMod.InlineRT.MonoModRule;
using static VoidTemplate.SaveManager;
namespace VoidTemplate.PlayerMechanics;

internal static class SwallowObjects
{
    public static void Hook()
    {
        On.Player.SwallowObject += Player_SwallowObject;
        On.Player.Regurgitate += Player_Regurgitate;
        On.Player.GrabUpdate += Player_GrabUpdate;
        On.SlugcatHand.Update += SlugcatHand_Update;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
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

    private static readonly HashSet<Type> TwoFullPinFoodObjects =
    [
        typeof(NeedleEgg),
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
                else if (TwoFullPinFoodObjects.Contains(grabbed.GetType()))
                {
                    HandleTwoFullPinFood(orig, self, grasp, abstractGrabbed);
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
                        self.room.updateList.Any(i => i is Oracle oracle && oracle.oracleBehavior is SSOracleBehavior))
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

    private static void HandleTwoFullPinFood(On.Player.orig_SwallowObject orig, Player self, int grasp, AbstractPhysicalObject abstractGrabbed)
    {
        orig(self, grasp);

        if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
        {
            self.objectInStomach.Destroy();
            self.objectInStomach = null;
            if (OptionInterface.OptionAccessors.SimpleFood && !self.room.game.IsArenaSession)
                self.AddFood(4);
            else
                self.AddFood(2);
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

    private static Dictionary<int, List<string>> pearlIDsInPlayerStomaches = new();

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
    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        if (self.SlugCatClass == VoidEnums.SlugcatID.Void || self.SlugCatClass == VoidEnums.SlugcatID.Viy)
        {
            self.spearOnBack?.Update(eu);
            if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
            {
                self.slugOnBack.Update(eu);
            }
            bool flag = ((self.input[0].y != 0 && self.input[0].x == 0 && self.bodyMode == BodyModeIndexExtension.CeilCrawl)
                || (self.input[0].x != 0 && self.input[0].y == 0 && self.bodyMode == Player.BodyModeIndex.WallClimb)
                || (self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw)
                || (ModManager.MMF && self.input[0].x == 0 && self.input[0].y == 1 && !self.input[0].jmp && !self.input[0].thrw
                && (self.bodyMode != Player.BodyModeIndex.ClimbingOnBeam || self.animation == Player.AnimationIndex.BeamTip || self.animation == Player.AnimationIndex.StandOnBeam)))
                && (self.mainBodyChunk.submersion < 0.5f);
            bool flag2 = false;
            bool flag3 = false;
            self.craftingObject = false;
            int num = -1;
            int num2 = -1;
            bool flag4 = false;
            if (self.input[0].pckp && !self.input[1].pckp && self.switchHandsProcess == 0f && !self.isSlugpup)
            {
                bool flag5 = self.grasps[0] != null || self.grasps[1] != null;
                if (flag5)
                {
                    if (self.switchHandsCounter == 0)
                    {
                        self.switchHandsCounter = 15;
                    }
                    else
                    {
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.switchHandsProcess = 0.01f;
                        self.wantToPickUp = 0;
                        self.noPickUpOnRelease = 20;
                    }
                }
                else
                {
                    self.switchHandsProcess = 0f;
                }
            }
            if (self.switchHandsProcess > 0f)
            {
                float num3 = self.switchHandsProcess;
                self.switchHandsProcess += 0.083333336f;
                if (num3 < 0.5f && self.switchHandsProcess >= 0.5f)
                {
                    self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Complete, self.mainBodyChunk);
                    self.SwitchGrasps(0, 1);
                }
                if (self.switchHandsProcess >= 1f)
                {
                    self.switchHandsProcess = 0f;
                }
            }
            int num4 = -1;
            int num5 = -1;
            int num6 = -1;
            if (flag)
            {
                int num7 = -1;
                if (ModManager.MSC)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (self.grasps[i] != null)
                        {
                            if (self.grasps[i].grabbed is JokeRifle)
                            {
                                num2 = i;
                            }
                            else if (JokeRifle.IsValidAmmo(self.grasps[i].grabbed))
                            {
                                num = i;
                            }
                        }
                    }
                }
                int num8 = 0;
                while (num5 < 0 && num8 < 2 && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                {
                    if (self.grasps[num8] != null && self.grasps[num8].grabbed is IPlayerEdible && (self.grasps[num8].grabbed as IPlayerEdible).Edible)
                    {
                        num5 = num8;
                    }
                    num8++;
                }
                if ((num5 == -1 || (self.FoodInStomach >= self.MaxFoodInStomach && self.grasps[num5].grabbed is not KarmaFlower && self.grasps[num5].grabbed is not Mushroom)) && (self.objectInStomach == null || self.CanPutSpearToBack || self.CanPutSlugToBack))
                {
                    int num9 = 0;
                    while (num7 < 0 && num4 < 0 && num6 < 0 && num9 < 2)
                    {
                        if (self.grasps[num9] != null)
                        {
                            if ((self.CanPutSlugToBack && self.grasps[num9].grabbed is Player && !(self.grasps[num9].grabbed as Player).dead) || self.CanIPutDeadSlugOnBack(self.grasps[num9].grabbed as Player))
                            {
                                num6 = num9;
                            }
                            else if (self.CanPutSpearToBack && self.grasps[num9].grabbed is Spear)
                            {
                                num4 = num9;
                            }
                            else if (self.CanBeSwallowed(self.grasps[num9].grabbed))
                            {
                                num7 = num9;
                            }
                        }
                        num9++;
                    }
                }
                if (num5 > -1 && self.noPickUpOnRelease < 1)
                {
                    if (!self.input[0].pckp)
                    {
                        int num10 = 1;
                        while (num10 < 10 && self.input[num10].pckp)
                        {
                            num10++;
                        }
                        if (num10 > 1 && num10 < 10)
                        {
                            self.PickupPressed();
                        }
                    }
                }
                else if (self.input[0].pckp && !self.input[1].pckp)
                {
                    self.PickupPressed();
                }
                if (self.input[0].pckp)
                {
                    if (ModManager.MSC && (self.FreeHand() == -1 || self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) && self.GraspsCanBeCrafted())
                    {
                        self.craftingObject = true;
                        flag3 = true;
                        num5 = -1;
                    }
                    if (num6 > -1 || self.CanRetrieveSlugFromBack)
                    {
                        self.slugOnBack.increment = true;
                    }
                    else if (num4 > -1 || self.CanRetrieveSpearFromBack)
                    {
                        self.spearOnBack.increment = true;
                    }
                    else if ((num7 > -1 || self.objectInStomach != null || self.IsVoid() || self.IsViy()) && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                    {
                        flag3 = true;
                    }
                    if (num > -1 && num2 > -1)
                    {
                        flag4 = true;
                    }
                }
                if (num5 > -1 && self.wantToPickUp < 1 && (self.input[0].pckp || self.eatCounter <= 15) && self.Consious && Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 3.6f))
                {
                    if (self.graphicsModule != null)
                    {
                        (self.graphicsModule as PlayerGraphics).LookAtObject(self.grasps[num5].grabbed);
                    }
                    flag2 = true;
                    if (self.FoodInStomach < self.MaxFoodInStomach || self.grasps[num5].grabbed is KarmaFlower || self.grasps[num5].grabbed is Mushroom)
                    {
                        flag3 = false;
                        if (self.spearOnBack != null)
                        {
                            self.spearOnBack.increment = false;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                        {
                            self.slugOnBack.increment = false;
                        }
                        if (self.eatCounter < 1)
                        {
                            self.eatCounter = 15;
                            self.BiteEdibleObject(eu);
                        }
                    }
                    else if (self.eatCounter < 20 && self.room.game.cameras[0].hud != null)
                    {
                        self.room.game.cameras[0].hud.foodMeter.RefuseFood();
                    }
                }
            }
            else if (self.input[0].pckp && !self.input[1].pckp)
            {
                self.PickupPressed();
            }
            else
            {
                if (self.CanPutSpearToBack)
                {
                    for (int m = 0; m < 2; m++)
                    {
                        if (self.grasps[m] != null && self.grasps[m].grabbed is Spear)
                        {
                            num4 = m;
                            break;
                        }
                    }
                }
                if (self.CanPutSlugToBack)
                {
                    for (int n = 0; n < 2; n++)
                    {
                        if (self.grasps[n] != null && self.grasps[n].grabbed is Player && !(self.grasps[n].grabbed as Player).dead)
                        {
                            num6 = n;
                            break;
                        }
                    }
                }
                if (self.input[0].pckp && (num6 > -1 || self.CanRetrieveSlugFromBack))
                {
                    self.slugOnBack.increment = true;
                }
                if (self.input[0].pckp && (num4 > -1 || self.CanRetrieveSpearFromBack))
                {
                    self.spearOnBack.increment = true;
                }
            }
            int num11 = 0;
            if (ModManager.MMF && (self.grasps[0] == null || self.grasps[0].grabbed is not Creature) && self.grasps[1] != null && self.grasps[1].grabbed is Creature)
            {
                num11 = 1;
            }
            if (ModManager.MSC && SlugcatStats.SlugcatCanMaul(self.SlugCatClass))
            {
                if (self.input[0].pckp && self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.CanMaulCreature(self.grasps[num11].grabbed as Creature) || self.maulTimer > 0))
                {
                    self.maulTimer++;
                    (self.grasps[num11].grabbed as Creature).Stun(60);
                    self.MaulingUpdate(num11);
                    if (self.spearOnBack != null)
                    {
                        self.spearOnBack.increment = false;
                        self.spearOnBack.interactionLocked = true;
                    }
                    if (self.slugOnBack != null)
                    {
                        self.slugOnBack.increment = false;
                        self.slugOnBack.interactionLocked = true;
                    }
                    if (self.grasps[num11] != null && self.maulTimer % 40 == 0)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                        self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        Custom.Log(
                        [
                        "Mauled target"
                        ]);
                        if (!(self.grasps[num11].grabbed as Creature).dead)
                        {
                            for (int num12 = UnityEngine.Random.Range(8, 14); num12 >= 0; num12--)
                            {
                                self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[num11].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[num11].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(self.grasps[num11].grabbed.firstChunk.pos, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
                            }
                            Creature creature = self.grasps[num11].grabbed as Creature;
                            BodyChunk hitChunk = self.grasps[num11].grabbedChunk;
                            float damage = 2.5f;
                            creature.SetKillTag(self.abstractCreature);
                            if (creature is Lizard && self.IsViy())
                            {
                                if (hitChunk != null && hitChunk.index == 0)
                                {
                                    creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, damage * 20, 50f);
                                }
                                else
                                 {
                                    creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, damage * 4, 50f);
                                }
                            }
                            if (creature is Lizard && self.IsVoid())
                            {
                                if (hitChunk != null && hitChunk.index == 0)
                                {
                                    creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, damage * 10, 50f);
                                }
                                else
                                {
                                    creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, damage * 2, 50f);
                                }
                            }
                            if (creature is not Lizard && self.IsVoid())
                            {
                                creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, damage, 50f);
                            }
                            creature.stun = 5;
                            if (creature.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.Inspector)
                            {
                                creature.Die();
                            }
                        }
                        self.maulTimer = 0;
                        self.wantToPickUp = 0;
                        if (self.grasps[num11] != null)
                        {
                            self.TossObject(num11, eu);
                            self.ReleaseGrasp(num11);
                        }
                        self.standing = true;
                    }
                    return;
                }
                if (self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.grasps[num11].grabbed as Creature).Consious && !self.IsCreatureLegalToHoldWithoutStun(self.grasps[num11].grabbed as Creature))
                {
                    Custom.Log(
                    [
                    "Lost hold of live mauling target"
                    ]);
                    self.maulTimer = 0;
                    self.wantToPickUp = 0;
                    self.ReleaseGrasp(num11);
                    return;
                }
            }
            if (self.input[0].pckp && self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && self.CanEatMeat(self.grasps[num11].grabbed as Creature) && (self.grasps[num11].grabbed as Creature).Template.meatPoints > 0)
            {
                self.eatMeat++;
                self.EatMeatUpdate(num11);
                if (!ModManager.MMF)
                {
                }
                if (self.spearOnBack != null)
                {
                    self.spearOnBack.increment = false;
                    self.spearOnBack.interactionLocked = true;
                }
                if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                {
                    self.slugOnBack.increment = false;
                    self.slugOnBack.interactionLocked = true;
                }
                if (self.grasps[num11] != null && self.eatMeat % 80 == 0 && ((self.grasps[num11].grabbed as Creature).State.meatLeft <= 0 || self.FoodInStomach >= self.MaxFoodInStomach))
                {
                    self.eatMeat = 0;
                    self.wantToPickUp = 0;
                    self.TossObject(num11, eu);
                    self.ReleaseGrasp(num11);
                    self.standing = true;
                }
                return;
            }
            if (!self.input[0].pckp && self.grasps[num11] != null && self.eatMeat > 60)
            {
                self.eatMeat = 0;
                self.wantToPickUp = 0;
                self.TossObject(num11, eu);
                self.ReleaseGrasp(num11);
                self.standing = true;
                return;
            }
            self.eatMeat = Custom.IntClamp(self.eatMeat - 1, 0, 50);
            self.maulTimer = Custom.IntClamp(self.maulTimer - 1, 0, 20);
            if (!ModManager.MMF || self.input[0].y == 0 || self.input[0].y != 0 && self.bodyMode == BodyModeIndexExtension.CeilCrawl)
            {
                if (flag2 && self.eatCounter > 0)
                {
                    if (ModManager.MSC)
                    {
                        if (num5 <= -1 || self.grasps[num5] == null || self.grasps[num5].grabbed is not GooieDuck || (self.grasps[num5].grabbed as GooieDuck).bites != 6 || self.timeSinceSpawned % 2 == 0)
                        {
                            self.eatCounter--;
                        }
                        if (num5 > -1 && self.grasps[num5] != null && self.grasps[num5].grabbed is GooieDuck && (self.grasps[num5].grabbed as GooieDuck).bites == 6 && self.FoodInStomach < self.MaxFoodInStomach)
                        {
                            (self.graphicsModule as PlayerGraphics).BiteStruggle(num5);
                        }
                    }
                    else
                    {
                        self.eatCounter--;
                    }
                }
                else if (!flag2 && self.eatCounter < 40)
                {
                    self.eatCounter++;
                }
            }
            if (flag4 && self.input[0].y == 0)
            {
                self.reloadCounter++;
                if (self.reloadCounter > 40)
                {
                    (self.grasps[num2].grabbed as JokeRifle).ReloadRifle(self.grasps[num].grabbed);
                    BodyChunk mainBodyChunk = self.mainBodyChunk;
                    mainBodyChunk.vel.y += 4f;
                    self.room.PlaySound(SoundID.Gate_Clamp_Lock, self.mainBodyChunk, false, 0.5f, 3f + UnityEngine.Random.value);
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[num].grabbed.abstractPhysicalObject;
                    self.ReleaseGrasp(num);
                    abstractPhysicalObject.realizedObject.RemoveFromRoom();
                    abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject);
                    self.reloadCounter = 0;
                }
            }
            else
            {
                self.reloadCounter = 0;
            }
            if (ModManager.MMF && (self.mainBodyChunk.submersion >= 0.5f || ExternalSaveData.ViyLungExtended && self.IsViy()))
            {
                flag3 = false;
            }
            if (flag3)
            {
                if (self.craftingObject)
                {
                    self.swallowAndRegurgitateCounter++;
                    if (self.swallowAndRegurgitateCounter > 105)
                    {
                        self.SpitUpCraftedObject();
                        self.swallowAndRegurgitateCounter = 0;
                    }
                }
                else if (!ModManager.MMF || self.input[0].y == 0 || self.input[0].y != 0 && self.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {

                    self.swallowAndRegurgitateCounter++;
                    for (int num14 = 0; num14 < 2; num14++)
                    {
                        if ((self.IsViy() || self.IsVoid()) && self.swallowAndRegurgitateCounter > 110 && (self.objectInStomach != null || pearlIDsInPlayerStomaches[self.playerState.playerNumber].Count > 0))
                        {
                            if (self.abstractCreature.world.game.IsStorySession && self.abstractCreature.world.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 7)
                            {
                                if (self.KarmaCap == 10 || Karma11Update.VoidKarma11 || (self.KarmaCap != 10 && !Karma11Update.VoidKarma11 && self.FoodInStomach >= 3))
                                {
                                    if (self.KarmaCap != 10 && !Karma11Update.VoidKarma11)
                                    {
                                        self.SubtractFood(1);
                                        self.SaintStagger(240);
                                    }
                                    self.Regurgitate();
                                }
                                else
                                {
                                    self.firstChunk.vel += new Vector2(UnityEngine.Random.Range(-1f, 1f), 0f);
                                    self.Stun(60);
                                }
                            }
                            else
                            {
                                if (self.KarmaCap == 10 || Karma11Update.VoidKarma11 || (self.KarmaCap != 10 && !Karma11Update.VoidKarma11 && self.FoodInStomach >= 3))
                                {
                                    if (self.KarmaCap != 10 && !Karma11Update.VoidKarma11)
                                    {
                                        self.SubtractFood(3);
                                        self.SaintStagger(720);
                                    }
                                    self.Regurgitate();

                                }
                                else
                                {
                                    self.firstChunk.vel += new Vector2(UnityEngine.Random.Range(-1f, 1f), 0f);
                                    self.Stun(60);
                                }
                            }
                            if (self.spearOnBack != null)
                            {
                                self.spearOnBack.interactionLocked = true;
                            }
                            if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                            {
                                self.slugOnBack.interactionLocked = true;
                            }
                            self.swallowAndRegurgitateCounter = 0;
                        }
                        else if (self.swallowAndRegurgitateCounter > 90)
                        {
                            for (int graspIndex = 0; graspIndex < 2; graspIndex++)
                            {
                                if (self.grasps[graspIndex] != null && self.CanBeSwallowed(self.grasps[graspIndex].grabbed))
                                {
                                    if (self.grasps[graspIndex].grabbed is DataPearl pearl
                                        && self.swallowAndRegurgitateCounter == 91
                                        && pearl.abstractPhysicalObject is DataPearl.AbstractDataPearl abstractPearl
                                        && self.abstractCreature.world.game.GetStorySession is not null
                                        && !Karma11Update.VoidKarma11)
                                    {
                                            pearlIDsInPlayerStomaches[self.playerState.playerNumber].Add(abstractPearl.dataPearlType.value);
                                            self.abstractCreature.world.game.GetStorySession.saveState.SetStomachPearls(pearlIDsInPlayerStomaches);
                                    }
                                    self.bodyChunks[0].pos += Custom.DirVec(self.grasps[graspIndex].grabbed.firstChunk.pos, self.bodyChunks[0].pos) * 2f;
                                    self.SwallowObject(graspIndex);
                                    if (self.spearOnBack != null)
                                    {
                                        self.spearOnBack.interactionLocked = true;
                                    }
                                    if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                                    {
                                        self.slugOnBack.interactionLocked = true;
                                    }
                                    self.swallowAndRegurgitateCounter = 0;
                                    (self.graphicsModule as PlayerGraphics).swallowing = 20;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (self.swallowAndRegurgitateCounter > 0)
                    {
                        self.swallowAndRegurgitateCounter--;
                    }
                    if (self.eatCounter > 0)
                    {
                        self.eatCounter--;
                    }
                }
            }
            else
            {
                self.swallowAndRegurgitateCounter = 0;
            }
            for (int num14 = 0; num14 < self.grasps.Length; num14++)
            {
                if (self.grasps[num14] != null && self.grasps[num14].grabbed.slatedForDeletetion)
                {
                    self.ReleaseGrasp(num14);
                }
            }
            if (self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands)
            {
                self.pickUpCandidate = null;
            }
            else
            {
                PhysicalObject physicalObject = (self.dontGrabStuff < 1) ? self.PickupCandidate(20f) : null;
                if (self.pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
                {
                    (physicalObject as PlayerCarryableItem).Blink();
                }
                self.pickUpCandidate = physicalObject;
            }
            if (self.switchHandsCounter > 0)
            {
                self.switchHandsCounter--;
            }
            if (self.wantToPickUp > 0)
            {
                self.wantToPickUp--;
            }
            if (self.wantToThrow > 0)
            {
                self.wantToThrow--;
            }
            if (self.noPickUpOnRelease > 0)
            {
                self.noPickUpOnRelease--;
            }
            if (self.input[0].thrw && !self.input[1].thrw && (!ModManager.MSC || !self.monkAscension))
            {
                self.wantToThrow = 5;
            }
            if (self.wantToThrow > 0)
            {
                if (ModManager.MSC && MMF.cfgOldTongue.Value && self.grasps[0] == null && self.grasps[1] == null && self.SaintTongueCheck())
                {
                    Vector2 vector2 = new((float)self.flipDirection, 0.7f);
                    Vector2 normalized = vector2.normalized;
                    if (self.input[0].y > 0)
                    {
                        normalized = new Vector2(0f, 1f);
                    }
                    normalized = (normalized + self.mainBodyChunk.vel.normalized * 0.2f).normalized;
                    self.tongue.Shoot(normalized);
                    self.wantToThrow = 0;
                }
                else
                {
                    for (int num15 = 0; num15 < 2; num15++)
                    {
                        if (self.grasps[num15] != null && self.IsObjectThrowable(self.grasps[num15].grabbed))
                        {
                            self.ThrowObject(num15, eu);
                            self.wantToThrow = 0;
                            break;
                        }
                    }
                }
                if ((ModManager.MSC || ModManager.CoopAvailable) && self.wantToThrow > 0 && self.slugOnBack != null && self.slugOnBack.HasASlug)
                {
                    Player slugcat = self.slugOnBack.slugcat;
                    self.slugOnBack.SlugToHand(eu);
                    self.ThrowObject(0, eu);
                    float num16 = (self.ThrowDirection >= 0) ? Mathf.Max(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x) : Mathf.Min(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x);
                    for (int num17 = 0; num17 < slugcat.bodyChunks.Length; num17++)
                    {
                        slugcat.bodyChunks[num17].pos.y = self.firstChunk.pos.y + 20f;
                        if (self.ThrowDirection < 0)
                        {
                            if (slugcat.bodyChunks[num17].pos.x > num16 - 8f)
                            {
                                slugcat.bodyChunks[num17].pos.x = num16 - 8f;
                            }
                            if (slugcat.bodyChunks[num17].vel.x > 0f)
                            {
                                slugcat.bodyChunks[num17].vel.x = 0f;
                            }
                        }
                        else if (self.ThrowDirection > 0)
                        {
                            if (slugcat.bodyChunks[num17].pos.x < num16 + 8f)
                            {
                                slugcat.bodyChunks[num17].pos.x = num16 + 8f;
                            }
                            if (slugcat.bodyChunks[num17].vel.x < 0f)
                            {
                                slugcat.bodyChunks[num17].vel.x = 0f;
                            }
                        }
                    }
                }
            }
            if (self.wantToPickUp > 0)
            {
                bool flag7 = true;
                if (self.animation == Player.AnimationIndex.DeepSwim)
                {
                    if (self.grasps[0] == null && self.grasps[1] == null)
                    {
                        flag7 = false;
                    }
                    else
                    {
                        for (int num18 = 0; num18 < 10; num18++)
                        {
                            if (self.input[num18].y > -1 || self.input[num18].x != 0)
                            {
                                flag7 = false;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int num19 = 0; num19 < 5; num19++)
                    {
                        if (self.input[num19].y > -1)
                        {
                            flag7 = false;
                            break;
                        }
                    }
                }
                if (ModManager.MSC)
                {
                    if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.mainBodyChunk.submersion > 0f)
                    {
                        flag7 = false;
                    }
                    else if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.canJump <= 0 && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                    {
                        (self.grasps[0].grabbed as EnergyCell).Use(false);
                    }
                }
                if (!ModManager.MMF && self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed))
                {
                    flag7 = true;
                }
                if (flag7)
                {
                    int num20 = -1;
                    for (int num21 = 0; num21 < 2; num21++)
                    {
                        if (self.grasps[num21] != null)
                        {
                            num20 = num21;
                            break;
                        }
                    }
                    if (num20 > -1)
                    {
                        self.wantToPickUp = 0;
                        if (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || self.grasps[num20].grabbed is not Scavenger)
                        {
                            self.pyroJumpDropLock = 0;
                        }
                        if (self.pyroJumpDropLock == 0 && (!ModManager.MSC || self.wantToJump == 0))
                        {
                            self.ReleaseObject(num20, eu);
                            return;
                        }
                    }
                    else
                    {
                        if (self.spearOnBack != null && self.spearOnBack.spear != null && self.mainBodyChunk.ContactPoint.y < 0)
                        {
                            self.room.socialEventRecognizer.CreaturePutItemOnGround(self.spearOnBack.spear, self);
                            self.spearOnBack.DropSpear();
                            return;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null && self.slugOnBack.slugcat != null && self.mainBodyChunk.ContactPoint.y < 0)
                        {
                            self.room.socialEventRecognizer.CreaturePutItemOnGround(self.slugOnBack.slugcat, self);
                            self.slugOnBack.DropSlug();
                            self.wantToPickUp = 0;
                            return;
                        }
                        if (ModManager.MSC && self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.wearingCloak && self.AI == null)
                        {
                            self.room.game.GetStorySession.saveState.wearingCloak = false;
                            AbstractConsumable abstractConsumable = new(self.room.game.world, MoreSlugcatsEnums.AbstractObjectType.MoonCloak, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), -1, -1, null);
                            self.room.abstractRoom.AddEntity(abstractConsumable);
                            abstractConsumable.pos = self.abstractCreature.pos;
                            abstractConsumable.RealizeInRoom();
                            (abstractConsumable.realizedObject as MoonCloak).free = true;
                            for (int num22 = 0; num22 < abstractConsumable.realizedObject.bodyChunks.Length; num22++)
                            {
                                abstractConsumable.realizedObject.bodyChunks[num22].HardSetPosition(self.mainBodyChunk.pos);
                            }
                            self.dontGrabStuff = 15;
                            self.wantToPickUp = 0;
                            self.noPickUpOnRelease = 20;
                            return;
                        }
                    }
                }
                else if (self.pickUpCandidate != null)
                {
                    if (self.pickUpCandidate is Spear && self.CanPutSpearToBack && ((self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) >= Player.ObjectGrabability.BigOneHand) || (self.grasps[1] != null && self.Grabability(self.grasps[1].grabbed) >= Player.ObjectGrabability.BigOneHand) || (self.grasps[0] != null && self.grasps[1] != null)))
                    {
                        Custom.Log(
                        [
                        "spear straight to back"
                        ]);
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.spearOnBack.SpearToBack(self.pickUpCandidate as Spear);
                    }
                    else if (self.CanPutSlugToBack && self.pickUpCandidate is Player && (!(self.pickUpCandidate as Player).dead || self.CanIPutDeadSlugOnBack(self.pickUpCandidate as Player)) && ((self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[0].grabbed is Player)) || (self.grasps[1] != null && (self.Grabability(self.grasps[1].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[1].grabbed is Player)) || (self.grasps[0] != null && self.grasps[1] != null) || self.bodyMode == Player.BodyModeIndex.Crawl))
                    {
                        Custom.Log(
                        [
                        "slugpup/player straight to back"
                        ]);
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.slugOnBack.SlugToBack(self.pickUpCandidate as Player);
                    }
                    else
                    {
                        int num23 = 0;
                        for (int num24 = 0; num24 < 2; num24++)
                        {
                            if (self.grasps[num24] == null)
                            {
                                num23++;
                            }
                        }
                        if (self.Grabability(self.pickUpCandidate) == Player.ObjectGrabability.TwoHands && num23 < 4)
                        {
                            for (int num25 = 0; num25 < 2; num25++)
                            {
                                if (self.grasps[num25] != null)
                                {
                                    self.ReleaseGrasp(num25);
                                }
                            }
                        }
                        else if (num23 == 0)
                        {
                            for (int num26 = 0; num26 < 2; num26++)
                            {
                                if (self.grasps[num26] != null && self.grasps[num26].grabbed is Fly)
                                {
                                    self.ReleaseGrasp(num26);
                                    break;
                                }
                            }
                        }
                        int num27 = 0;
                        while (num27 < 2)
                        {
                            if (self.grasps[num27] == null)
                            {
                                if (self.pickUpCandidate is Creature)
                                {
                                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Creature, self.pickUpCandidate.firstChunk, false, 1f, 1f);
                                }
                                else if (self.pickUpCandidate is PlayerCarryableItem)
                                {
                                    for (int num28 = 0; num28 < self.pickUpCandidate.grabbedBy.Count; num28++)
                                    {
                                        if (self.pickUpCandidate.grabbedBy[num28].grabber.room == self.pickUpCandidate.grabbedBy[num28].grabbed.room)
                                        {
                                            self.pickUpCandidate.grabbedBy[num28].grabber.GrabbedObjectSnatched(self.pickUpCandidate.grabbedBy[num28].grabbed, self);
                                        }
                                        else
                                        {
                                            Custom.LogWarning(
                                            [
                                            string.Format("Item theft room mismatch? {0}", self.pickUpCandidate.grabbedBy[num28].grabbed.abstractPhysicalObject)
                                            ]);
                                        }
                                        self.pickUpCandidate.grabbedBy[num28].grabber.ReleaseGrasp(self.pickUpCandidate.grabbedBy[num28].graspUsed);
                                    }
                                    (self.pickUpCandidate as PlayerCarryableItem).PickedUp(self);
                                }
                                else
                                {
                                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Misc_Inanimate, self.pickUpCandidate.firstChunk, false, 1f, 1f);
                                }
                                self.SlugcatGrab(self.pickUpCandidate, num27);
                                if (self.pickUpCandidate.graphicsModule != null && self.Grabability(self.pickUpCandidate) < (Player.ObjectGrabability)5)
                                {
                                    self.pickUpCandidate.graphicsModule.BringSpritesToFront();
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                num27++;
                            }
                        }
                    }
                    self.wantToPickUp = 0;
                }
            }
        }
        else
        {
            orig(self, eu);
        }
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

