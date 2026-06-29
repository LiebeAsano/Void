using MoreSlugcats;
using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

public static class Grabability
{
    public static void Hook()
    {
        On.Player.Grabability += Player_Grabability;
        On.Creature.Update += Creature_Update;
        On.Player.CanIPickThisUp += Player_CanIPickselfUp;
        On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
        On.SlugcatHand.Update += SlugcatHand_Update;
        On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
        On.Creature.Grasp.Release += Grasp_Release;
        On.Player.IsCreatureImmuneToPlayerGrabStun += Player_IsCreatureImmuneToPlayerGrabStun;
        On.Player.TerrainImpact += Player_TerrainImpact;
    }

    private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player && player.AreVoidViy() && !self.Consious)
            speed = 0;
        orig(self, chunk, direction, speed, firstContact);
    }

    private static bool Player_IsCreatureImmuneToPlayerGrabStun(On.Player.orig_IsCreatureImmuneToPlayerGrabStun orig, Player self, Creature grabCheck)
    {
        if (self.AreVoidViy() && grabCheck is Player) return false;
        return orig(self, grabCheck);
    }

    private static void Grasp_Release(On.Creature.Grasp.orig_Release orig, Creature.Grasp self)
    {
        orig(self);
        if (self.grabber is Player p && p.AreVoidViy() && self.grabbed is Creature)
        {
            self.grabbed.CollideWithObjects = true;
        }
    }

    private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        if (self.owner.owner is Player player && player.AreVoidViy())
        {
            self.lastPos = self.pos;
            if (self.retract && self.mode != Limb.Mode.Retracted)
            {
                self.mode = Limb.Mode.HuntAbsolutePosition;
                self.absoluteHuntPos = self.connection.pos;
                if (Custom.DistLess(self.absoluteHuntPos, self.pos, self.huntSpeed))
                {
                    self.mode = Limb.Mode.Retracted;
                }
            }
            if (self.mode == Limb.Mode.HuntRelativePosition)
            {
                self.absoluteHuntPos = self.connection.pos + Custom.RotateAroundOrigo(self.relativeHuntPos, Custom.AimFromOneVectorToAnother(self.connection.rotationChunk.pos, self.connection.pos));
            }
            if (self.mode == Limb.Mode.HuntRelativePosition || self.mode == Limb.Mode.HuntAbsolutePosition)
            {
                if (Custom.DistLess(self.absoluteHuntPos, self.pos, self.huntSpeed))
                {
                    self.vel = self.absoluteHuntPos - self.pos;
                    self.reachedSnapPosition = true;
                }
                else
                {
                    self.vel = Vector2.Lerp(self.vel, Custom.DirVec(self.pos, self.absoluteHuntPos) * self.huntSpeed, self.quickness);
                    self.reachedSnapPosition = false;
                }
            }
            else if (self.mode == Limb.Mode.Retracted)
            {
                self.vel = self.connection.vel;
                self.pos = self.connection.pos;
                self.reachedSnapPosition = true;
            }
            else if (self.mode == Limb.Mode.Dangle)
            {
                self.reachedSnapPosition = false;
            }
            self.quickness = self.defaultQuickness;
            self.huntSpeed = self.defaultHuntSpeed;
            if (self.mode != Limb.Mode.Retracted)
            {
                self.pos += self.vel;
                if (self.mode == Limb.Mode.HuntRelativePosition)
                {
                    self.pos += self.connection.vel;
                }
                self.vel *= self.airFriction;
                if (self.pushOutOfTerrain)
                {
                    self.PushOutOfTerrain(self.owner.owner.room, self.connection.pos);
                }
            }
            self.ConnectToPoint(self.connection.pos, 20f, false, 0f, self.connection.vel, 0f, 0f);

            bool flag;
            if (self.reachingForObject)
            {
                self.mode = Limb.Mode.HuntAbsolutePosition;
                flag = false;
                self.reachingForObject = false;
            }
            else
            {
                flag = self.EngageInMovement();
            }

            var grasp = player.grasps[self.limbNumber];

            if (grasp?.grabbed is Player grabbedPlayer &&
                grabbedPlayer != player &&
                player.Grabability(grasp.grabbed) == Player.ObjectGrabability.OneHand)
            {
                if (flag)
                {
                    if (((self.owner.owner as Player).grasps[0] != null && (self.owner.owner as Player).HeavyCarry((self.owner.owner as Player).grasps[0].grabbed)) || (ModManager.MMF && (self.owner.owner as Player).grasps[1] != null && (self.owner.owner as Player).HeavyCarry((self.owner.owner as Player).grasps[1].grabbed)))
                    {
                        self.mode = Limb.Mode.HuntAbsolutePosition;
                        BodyChunk bodyChunk;
                        if (ModManager.MMF)
                        {
                            bodyChunk = (((self.owner.owner as Player).grasps[0] != null && (self.owner.owner as Player).HeavyCarry((self.owner.owner as Player).grasps[0].grabbed)) ? (self.owner.owner as Player).grasps[0].grabbedChunk : (self.owner.owner as Player).grasps[1].grabbedChunk);
                        }
                        else
                        {
                            bodyChunk = (self.owner.owner as Player).grasps[0].grabbedChunk;
                        }
                        self.absoluteHuntPos = bodyChunk.pos + Custom.PerpendicularVector((self.connection.pos - bodyChunk.pos).normalized) * (bodyChunk.rad * 0.8f * ((self.limbNumber == 0) ? -1f : 1f));
                        self.huntSpeed = 20f;
                        self.quickness = 1f;
                        flag = false;
                    }
                    else if ((self.owner.owner as Player).grasps[self.limbNumber] != null)
                    {
                        self.mode = Limb.Mode.HuntRelativePosition;
                        if (ModManager.MSC && (self.owner.owner as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                        {
                            self.relativeHuntPos.x = (self.owner.owner as Player).ThrowDirection * 3;
                        }
                        else
                        {
                            self.relativeHuntPos.x = -20f + 40f * self.limbNumber;
                        }
                        self.relativeHuntPos.y = -12f;
                        if ((self.owner.owner as Player).eatCounter < 40)
                        {
                            int num = -1;
                            int num2 = 0;
                            while (num < 0 && num2 < 2)
                            {
                                if ((self.owner.owner as Player).grasps[num2] != null && (self.owner.owner as Player).grasps[num2].grabbed is IPlayerEdible && ((self.owner.owner as Player).grasps[num2].grabbed as IPlayerEdible).Edible)
                                {
                                    num = num2;
                                }
                                num2++;
                            }
                            if (num == self.limbNumber)
                            {
                                self.relativeHuntPos *= Custom.LerpMap((self.owner.owner as Player).eatCounter, 40f, 20f, 0.9f, 0.7f);
                                self.relativeHuntPos.y += Custom.LerpMap((self.owner.owner as Player).eatCounter, 40f, 20f, 2f, 4f);
                                self.relativeHuntPos.x *= Custom.LerpMap((self.owner.owner as Player).eatCounter, 40f, 20f, 1f, 1.2f);
                            }
                        }
                        if (((self.owner.owner as Player).swallowAndRegurgitateCounter > 10 && (self.owner.owner as Player).objectInStomach == null) || (self.owner.owner as Player).craftingObject)
                        {
                            int num3 = -1;
                            int num4 = 0;
                            while (num3 < 0 && num4 < 2)
                            {
                                if ((self.owner.owner as Player).grasps[num4] != null && (self.owner.owner as Player).CanBeSwallowed((self.owner.owner as Player).grasps[num4].grabbed))
                                {
                                    num3 = num4;
                                }
                                num4++;
                            }
                            if (num3 == self.limbNumber || (self.owner.owner as Player).craftingObject)
                            {
                                float num5 = Mathf.InverseLerp(10f, 90f, (float)(self.owner.owner as Player).swallowAndRegurgitateCounter);
                                if (num5 < 0.5f)
                                {
                                    self.relativeHuntPos *= Mathf.Lerp(0.9f, 0.7f, num5 * 2f);
                                    self.relativeHuntPos.y += Mathf.Lerp(2f, 4f, num5 * 2f);
                                    self.relativeHuntPos.x *= Mathf.Lerp(1f, 1.2f, num5 * 2f);
                                }
                                else
                                {
                                    (self.owner as PlayerGraphics).blink = 5;
                                    self.relativeHuntPos = new Vector2(0f, -4f) + Custom.RNV() * (2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5));
                                    (self.owner as PlayerGraphics).head.vel += Custom.RNV() * (2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5));
                                    self.owner.owner.bodyChunks[0].vel += Custom.RNV() * (0.2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5));
                                }
                            }
                        }
                        self.relativeHuntPos.x *= (1f - Mathf.Sin((self.owner.owner as Player).switchHandsProcess * 3.1415927f));
                        if ((self.owner as PlayerGraphics).spearDir != 0f && (self.owner.owner as Player).bodyMode == Player.BodyModeIndex.Stand)
                        {
                            Vector2 b = Custom.DegToVec(180f + ((self.limbNumber == 0) ? -1f : 1f) * 8f + (self.owner.owner as Player).input[0].x * 4f) * 12f;
                            b.y += Mathf.Sin((self.owner.owner as Player).animationFrame / 6f * 2f * 3.1415927f) * 2f;
                            b.x -= Mathf.Cos(((self.owner.owner as Player).animationFrame + ((self.owner.owner as Player).leftFoot ? 0 : 6)) / 12f * 2f * 3.1415927f) * 4f * (self.owner.owner as Player).input[0].x;
                            b.x += (self.owner.owner as Player).input[0].x * 2f;
                            self.relativeHuntPos = Vector2.Lerp(self.relativeHuntPos, b, Mathf.Abs((self.owner as PlayerGraphics).spearDir));
                            if ((self.owner.owner as Player).grasps[self.limbNumber].grabbed is Weapon)
                            {
                                ((self.owner.owner as Player).grasps[self.limbNumber].grabbed as Weapon).ChangeOverlap(((self.owner as PlayerGraphics).spearDir > -0.4f && self.limbNumber == 0) || ((self.owner as PlayerGraphics).spearDir < 0.4f && self.limbNumber == 1));
                            }
                        }
                        flag = false;
                        if ((self.owner.owner as Creature).grasps[self.limbNumber].grabbed is Fly && !((self.owner.owner as Creature).grasps[self.limbNumber].grabbed as Fly).dead)
                        {
                            self.huntSpeed = Random.value * 5f;
                            self.quickness = Random.value * 0.3f;
                            self.vel += Custom.DegToVec(Random.value * 360f) * (Random.value * Random.value * (Custom.DistLess(self.absoluteHuntPos, self.pos, 7f) ? 4f : 1.5f));
                            self.pos += Custom.DegToVec(Random.value * 360f) * (Random.value * 4f);
                            (self.owner as PlayerGraphics).NudgeDrawPosition(0, Custom.DirVec((self.owner.owner as Creature).mainBodyChunk.pos, self.pos) * (3f * Random.value));
                            (self.owner as PlayerGraphics).head.vel += Custom.DirVec((self.owner.owner as Creature).mainBodyChunk.pos, self.pos) * (2f * Random.value);
                        }
                        else if ((self.owner.owner as Creature).grasps[self.limbNumber].grabbed is VultureMask)
                        {
                            self.relativeHuntPos *= 1f - ((self.owner.owner as Creature).grasps[self.limbNumber].grabbed as VultureMask).donned;
                        }
                    }
                }
                if (flag && self.mode != Limb.Mode.Retracted)
                {
                    self.retractCounter++;
                    if (self.retractCounter > 5f)
                    {
                        self.mode = Limb.Mode.HuntAbsolutePosition;
                        self.pos = Vector2.Lerp(self.pos, self.owner.owner.bodyChunks[0].pos, Mathf.Clamp((self.retractCounter - 5f) * 0.05f, 0f, 1f));
                        if (Custom.DistLess(self.pos, self.owner.owner.bodyChunks[0].pos, 2f) && self.reachedSnapPosition)
                        {
                            self.mode = Limb.Mode.Retracted;
                        }
                        self.absoluteHuntPos = self.owner.owner.bodyChunks[0].pos;
                        self.huntSpeed = 1f + self.retractCounter * 0.2f;
                        self.quickness = 1f;
                        return;
                    }
                }
                else
                {
                    self.retractCounter -= 10;
                    if (self.retractCounter < 0)
                    {
                        self.retractCounter = 0;
                    }
                }
                return;

            }
        }
        orig(self);
    }

    private static void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
    {
        if (!self.IsViy())
        {
            orig(self, actuallyViewed, eu);
        }
        self.spearOnBack?.GraphicsModuleUpdated(actuallyViewed, eu);
        for (int i = 0; i < 2; i++)
        {
            if (self.grasps[i] != null)
            {
                if (self.HeavyCarry(self.grasps[i].grabbed))
                {
                    Vector2 a2 = Custom.DirVec(self.mainBodyChunk.pos, self.grasps[i].grabbedChunk.pos);
                    float num4 = Vector2.Distance(self.mainBodyChunk.pos, self.grasps[i].grabbedChunk.pos);
                    float num5 = 5f + self.grasps[i].grabbedChunk.rad;
                    if (self.grasps[i].grabbed is Cicada)
                    {
                        num5 = 30f;
                    }
                    num5 *= Mathf.InverseLerp(25f, 15f, self.eatMeat);
                    float num6 = self.grasps[i].grabbedChunk.mass / (self.mainBodyChunk.mass + self.grasps[i].grabbedChunk.mass);
                    if (self.enteringShortCut != null)
                    {
                        num6 = 0f;
                    }
                    else if (self.grasps[i].grabbed.TotalMass < self.TotalMass)
                    {
                        num6 /= 2f;
                    }
                    if (self.enteringShortCut == null || num4 > num5)
                    {
                        Vector2 b3 = a2 * ((num4 - num5) * num6);
                        self.mainBodyChunk.pos += b3;
                        self.mainBodyChunk.vel += b3;
                        Vector2 b4 = a2 * ((num4 - num5) * (1f - num6));
                        self.grasps[i].grabbedChunk.pos -= b4;
                        self.grasps[i].grabbedChunk.vel -= b4;
                    }
                    if (self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam && self.animation != Player.AnimationIndex.BeamTip && self.animation != Player.AnimationIndex.StandOnBeam)
                    {
                        BodyChunk grabbedChunk2 = self.grasps[i].grabbedChunk;
                        grabbedChunk2.vel.y += self.grasps[i].grabbed.gravity * (1f - self.grasps[i].grabbedChunk.submersion) * 0.75f;
                    }
                    if (self.Grabability(self.grasps[i].grabbed) == Player.ObjectGrabability.Drag && num4 > num5 * 2f + 30f)
                    {
                        self.ReleaseGrasp(i);
                    }
                }
                else if (actuallyViewed)
                {
                    if (self.graphicsModule != null)
                    {
                        self.grasps[i].grabbedChunk.vel = (self.graphicsModule as PlayerGraphics).hands[i].vel;
                        self.grasps[i].grabbedChunk.MoveFromOutsideMyUpdate(eu, (self.graphicsModule as PlayerGraphics).hands[i].pos);
                    }
                    if (self.grasps[i].grabbed is Weapon)
                    {
                        Vector2 heldItemDirection = self.GetHeldItemDirection(i);
                        (self.grasps[i].grabbed as Weapon).setRotation = new Vector2?(heldItemDirection);
                        (self.grasps[i].grabbed as Weapon).rotationSpeed = 0f;
                    }
                }
                else
                {
                    self.grasps[i].grabbedChunk.pos = self.bodyChunks[0].pos;
                    self.grasps[i].grabbedChunk.vel = self.mainBodyChunk.vel;
                }
            }
        }
    }

    public static bool CanOneHandGrabVoidViy(Player self, PhysicalObject obj)
    {
        if (self == null || obj == null)
            return false;

        return self.AreVoidViy() && (obj is LanternMouse || obj is EggBug || obj is Watcher.Frog || obj is Watcher.Rat ||
               (obj is Watcher.Barnacle barnacle && !barnacle.hasShell));
    }
    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self == null || obj == null)
            return orig(self, obj);

        if (CanOneHandGrabVoidViy(self, obj))
            return Player.ObjectGrabability.OneHand;

        if (obj is PoleMimic || obj is TentaclePlant)
            return Player.ObjectGrabability.CantGrab;

        if (self.AreVoidViy())
        {
            if (obj is Cicada || obj is JellyFish || obj is Yeek)
                return Player.ObjectGrabability.Drag;

            if (obj is Player player && player != self && !player.AreVoidViy())
            {
                if (player.room?.game?.IsArenaSession != true)
                return player.IsVoid() ? Player.ObjectGrabability.Drag : Player.ObjectGrabability.OneHand;
            }

            if (obj is Watcher.BigMoth bigMoth && bigMoth.Small)
                return Player.ObjectGrabability.Drag;
        }

        return orig(self, obj);
    }

    private static readonly ConditionalWeakTable<Creature, float[]> OriginalMasses = new();

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        if (self == null || self.slatedForDeletetion || self.room == null)
        {
            orig(self, eu);
            return;
        }

        orig(self, eu);

        bool isGrabbedByVoidViy = false;

        if (self.grabbedBy != null)
        {
            foreach (var grasp in self.grabbedBy)
            {
                if (grasp?.grabber is Player grabberPlayer && grabberPlayer.AreVoidViy())
                {
                    isGrabbedByVoidViy = true;
                    if (self is Player player && !player.AreVoidViy())
                    {
                        if (player.playerState is not null)
                        {
                            if (player.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher &&
                            player.room?.game?.GetStorySession.saveState.miscWorldSaveData.hasVoidWeaverAbility == true)
                            {
                                player.SetHaloDisplayTime(20);
                            }
                            else
                            {
                                player.SetKillTag(grabberPlayer.abstractCreature);
                                player.playerState.permanentDamageTracking += 0.000125f;
                                if (player.playerState.permanentDamageTracking >= 1.0f)
                                {
                                    player.Die();
                                }
                            }
                        }
                    }
                    else if (self is not Player && (self is not TubeWorm tubeWorm || !tubeWorm.dead))
                    {
                        if (self.State is HealthState)
                        {
                            self.SetKillTag(grabberPlayer.abstractCreature);
                            (self.State as HealthState).health -= 0.000125f;
                            if (self.Template.quickDeath && (Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || ((self.State as HealthState).health < 0f && Random.value < 0.33f)))
                            {
                                self.Die();
                            }
                        }
                    }
                    if (grabberPlayer.Grabability(self) == Player.ObjectGrabability.OneHand && (!(self.Template.smallCreature || (self is Centipede centi && centi.Small))))
                    {
                        self.CollideWithObjects = false;
                    }
                    break;
                }
            }
        }
        if (!OriginalMasses.TryGetValue(self, out float[] origChunkMasses) && isGrabbedByVoidViy)
        {
            origChunkMasses = new float[self.bodyChunks.Length];
            OriginalMasses.Add(self, origChunkMasses);
            foreach (var chunk in self.bodyChunks)
            {
                origChunkMasses[chunk.index] = chunk.mass;
            }
        }

        if (origChunkMasses != null)
        {
            foreach (var chunk in self.bodyChunks)
            {
                float originalMass = origChunkMasses[chunk.index];
                if (self is Cicada || self is JetFish)
                {
                    chunk.mass = isGrabbedByVoidViy && self.dead ? 0.05f : originalMass;
                }
                else if (self is Watcher.BigMoth bigMoth && bigMoth.Small)
                {
                    chunk.mass = isGrabbedByVoidViy ? originalMass * 0.25f : originalMass;
                }
                else if (self is Lizard || self is Centipede || self is DropBug || self is BigNeedleWorm || self is BigSpider || self is Scavenger)
                {
                    chunk.mass = isGrabbedByVoidViy ? originalMass * 0.5f : originalMass;
                }
                else if (self is Player)
                {
                    chunk.mass = isGrabbedByVoidViy ? originalMass * 0.25f : originalMass;
                }
            }
        }

        if (!isGrabbedByVoidViy)
        {
            OriginalMasses.Remove(self);
        }
    }

    public static bool Player_CanIPickselfUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (obj is Player player && player.IsViy() && player.Consious)
        {
            return false;
        }
        if (obj is Player player2 && player2.IsVoid() && player2.Consious && player2.bodyMode != Player.BodyModeIndex.Crawl)
        {
            return false;
        }
        return orig(self, obj);
    }

    private static bool Player_IsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabCheck)
    {
        return grabCheck is Watcher.BigMoth bigMoth && bigMoth.Small || orig(self, grabCheck);
    }

}
