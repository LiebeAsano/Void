using CoralBrain;
using Fisobs;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class MovementUpdate
{
    public static void Hook()
    {
        On.Player.MovementUpdate += Player_MovementUpdate;
    }

    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        if (self.AreVoidViy())
        {
            if (self.bodyMode == BodyModeIndexExtension.Rot)
            {
                return;
            }
            self.DirectIntoHoles();
            if (self.rocketJumpFromBellySlide && self.animation != Player.AnimationIndex.RocketJump)
            {
                self.rocketJumpFromBellySlide = false;
            }
            if (self.flipFromSlide && self.animation != Player.AnimationIndex.Flip)
            {
                self.flipFromSlide = false;
            }
            if (self.whiplashJump && self.animation != Player.AnimationIndex.BellySlide)
            {
                self.whiplashJump = false;
            }
            int num = self.input[0].x;
            if (self.jumpStun != 0)
            {
                num = self.jumpStun / Mathf.Abs(self.jumpStun);
            }
            self.lastFlipDirection = self.flipDirection;
            if (num != self.flipDirection && num != 0)
            {
                self.flipDirection = num;
            }
            if (self.rippleActivating && self.CanLevitate)
            {
                num = 0;
                self.flipDirection = 0;
                self.input[0].x = 0;
                self.input[0].y = 0;
            }
            int num2 = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (self.IsTileSolid(j, Custom.eightDirections[i].x, Custom.eightDirections[i].y) && self.IsTileSolid(j, Custom.eightDirections[i + 4].x, Custom.eightDirections[i + 4].y))
                    {
                        num2++;
                    }
                }
            }
            bool flag = self.bodyChunks[1].onSlope == 0 && self.input[0].x == 0 && self.standing && self.stun < 1 && self.bodyChunks[1].ContactPoint.y == -1 && self.bodyChunks[1].terrainCurveNormal == default(Vector2);
            if (self.feetStuckPos != null && !flag)
            {
                self.feetStuckPos = null;
            }
            else if (self.feetStuckPos == null && flag)
            {
                self.feetStuckPos = new Vector2?(new Vector2(self.bodyChunks[1].pos.x, self.room.MiddleOfTile(self.room.GetTilePosition(self.bodyChunks[1].pos)).y + -10f + self.bodyChunks[1].rad));
            }
            if (self.feetStuckPos != null)
            {
                self.feetStuckPos = new Vector2?(self.feetStuckPos.Value + new Vector2((self.bodyChunks[1].pos.x - self.feetStuckPos.Value.x) * (1f - self.surfaceFriction), 0f));
                self.bodyChunks[1].pos = self.feetStuckPos.Value;
                if (!self.IsTileSolid(1, 0, -1))
                {
                    bool flag2 = self.IsTileSolid(1, 1, -1) && !self.IsTileSolid(1, 1, 0);
                    bool flag3 = self.IsTileSolid(1, -1, -1) && !self.IsTileSolid(1, -1, 0);
                    if (flag3 && !flag2)
                    {
                        self.feetStuckPos = new Vector2?(self.feetStuckPos.Value + new Vector2(-1.6f * self.surfaceFriction, 0f));
                    }
                    else if (flag2 && !flag3)
                    {
                        self.feetStuckPos = new Vector2?(self.feetStuckPos.Value + new Vector2(1.6f * self.surfaceFriction, 0f));
                    }
                    else
                    {
                        self.feetStuckPos = null;
                    }
                }
            }
            if ((num2 > 1 && self.bodyChunks[0].onSlope == 0 && self.bodyChunks[1].onSlope == 0 && (!self.IsTileSolid(0, 0, 0) || !self.IsTileSolid(1, 0, 0))) || (self.IsTileSolid(0, -1, 0) && self.IsTileSolid(0, 1, 0)) || (self.IsTileSolid(1, -1, 0) && self.IsTileSolid(1, 1, 0)))
            {
                self.goIntoCorridorClimb++;
            }
            else
            {
                self.goIntoCorridorClimb = 0;
                bool flag4 = self.bodyChunks[0].ContactPoint.y == -1 || self.bodyChunks[1].ContactPoint.y == -1;
                self.bodyMode = Player.BodyModeIndex.Default;
                if (flag4)
                {
                    self.canJump = 5;
                    if (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y + 3f && !self.IsTileSolid(1, 0, 1) && self.animation != Player.AnimationIndex.CrawlTurn && self.bodyChunks[0].ContactPoint.y > -1)
                    {
                        self.bodyMode = Player.BodyModeIndex.Stand;
                    }
                    else
                    {
                        self.bodyMode = Player.BodyModeIndex.Crawl;
                    }
                }
                else if (self.jumpBoost > 0f && (self.input[0].jmp || self.simulateHoldJumpButton > 0))
                {
                    self.jumpBoost -= 1.5f;
                    BodyChunk bodyChunk = self.bodyChunks[0];
                    bodyChunk.vel.y = bodyChunk.vel.y + (self.jumpBoost + 1f) * 0.3f;
                    BodyChunk bodyChunk2 = self.bodyChunks[1];
                    bodyChunk2.vel.y = bodyChunk2.vel.y + (self.jumpBoost + 1f) * 0.3f;
                }
                else
                {
                    self.jumpBoost = 0f;
                }
                if (self.bodyChunks[0].ContactPoint.x != 0 && self.bodyChunks[0].ContactPoint.x == self.input[0].x && self.bodyMode != Player.BodyModeIndex.Crawl)
                {
                    if (self.bodyChunks[0].lastContactPoint.x != self.input[0].x)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Enter_Wall_Slide, self.mainBodyChunk, false, 1f, 1f);
                    }
                    self.bodyMode = Player.BodyModeIndex.WallClimb;
                }
                if (self.input[0].x != 0 && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y && self.animation != Player.AnimationIndex.CrawlTurn && !self.IsTileSolid(0, self.input[0].x, 0) && self.IsTileSolid(1, self.input[0].x, 0) && self.bodyChunks[1].ContactPoint.x == self.input[0].x)
                {
                    self.bodyMode = Player.BodyModeIndex.Crawl;
                    self.animation = Player.AnimationIndex.LedgeCrawl;
                }
                if (self.input[0].y == 1 && self.IsTileSolid(0, 0, 1) && !self.IsTileSolid(1, 0, 1) && (self.IsTileSolid(1, -1, 1) || self.IsTileSolid(1, 1, 1)))
                {
                    self.animation = Player.AnimationIndex.None;
                    BodyChunk bodyChunk3 = self.bodyChunks[1];
                    bodyChunk3.vel.y = bodyChunk3.vel.y + 2f * self.EffectiveRoomGravity;
                    BodyChunk bodyChunk4 = self.bodyChunks[0];
                    bodyChunk4.vel.x = bodyChunk4.vel.x - (self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) * 0.25f * self.EffectiveRoomGravity;
                    BodyChunk bodyChunk5 = self.bodyChunks[0];
                    bodyChunk5.vel.y = bodyChunk5.vel.y - self.EffectiveRoomGravity;
                }
            }
            if (self.input[0].y > 0 && self.input[0].x == 0 && self.bodyMode == Player.BodyModeIndex.Default && self.firstChunk.pos.y - self.firstChunk.lastPos.y < 2f && self.bodyChunks[1].ContactPoint.y == 0 && !self.IsTileSolid(0, 0, 1) && self.IsTileSolid(0, -1, 1) && self.IsTileSolid(0, 1, 1) && !self.IsTileSolid(1, -1, 0) && !self.IsTileSolid(1, 1, 0) && self.room.GetTilePosition(self.firstChunk.pos) == self.room.GetTilePosition(self.bodyChunks[1].pos) + new IntVector2(0, 1) && Mathf.Abs(self.firstChunk.pos.x - self.room.MiddleOfTile(self.firstChunk.pos).x) < 5f && self.EffectiveRoomGravity > 0f)
            {
                self.firstChunk.pos.x = self.room.MiddleOfTile(self.firstChunk.pos).x;
                BodyChunk firstChunk = self.firstChunk;
                firstChunk.pos.y = firstChunk.pos.y + 1f;
                BodyChunk firstChunk2 = self.firstChunk;
                firstChunk2.vel.y = firstChunk2.vel.y + 1f;
                BodyChunk bodyChunk6 = self.bodyChunks[1];
                bodyChunk6.vel.y = bodyChunk6.vel.y + 1f;
                BodyChunk bodyChunk7 = self.bodyChunks[1];
                bodyChunk7.pos.y = bodyChunk7.pos.y + 1f;
            }
            if (self.input[0].y == 1 && self.input[1].y != 1)
            {
                if (self.bodyChunks[1].onSlope == 0 || !self.IsTileSolid(0, 0, 1))
                {
                    self.standing = true;
                }
            }
            else if (self.input[0].y == -1 && self.input[1].y != -1)
            {
                if (self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
                {
                    self.room.PlaySound(SoundID.Slugcat_Down_On_Fours, self.mainBodyChunk);
                }
                self.standing = false;
            }
            if (self.EffectiveRoomGravity > 0f && self.animation == Player.AnimationIndex.ZeroGPoleGrab)
            {
                self.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                if (self.room.GetTile(self.mainBodyChunk.pos).horizontalBeam)
                {
                    self.animation = Player.AnimationIndex.HangFromBeam;
                }
                else
                {
                    self.animation = Player.AnimationIndex.ClimbOnBeam;
                }
            }
            if (self.goIntoCorridorClimb > 2 && !self.corridorDrop)
            {
                self.bodyMode = Player.BodyModeIndex.CorridorClimb;
                self.animation = ((self.corridorTurnDir != null) ? Player.AnimationIndex.CorridorTurn : Player.AnimationIndex.None);
            }
            if (self.corridorDrop)
            {
                self.bodyMode = Player.BodyModeIndex.Default;
                self.animation = Player.AnimationIndex.None;
                if (self.input[0].y >= 0 || self.goIntoCorridorClimb < 2)
                {
                    self.corridorDrop = false;
                }
                if (self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y)
                {
                    for (int k = 0; k < Custom.IntClamp((int)(self.bodyChunks[0].vel.y * -0.3f), 1, 10); k++)
                    {
                        if (self.IsTileSolid(0, 0, -k))
                        {
                            self.corridorDrop = false;
                            break;
                        }
                    }
                }
            }
            if (self.bodyMode != Player.BodyModeIndex.WallClimb || self.bodyChunks[0].submersion == 1f)
            {
                bool flag5 = self.input[0].y < 0 || self.input[0].downDiagonal != 0;
                if (ModManager.DLCShared && self.room.waterInverted)
                {
                    flag5 = (self.input[0].y > 0);
                }
                if ((self.bodyChunks[0].submersion > 0.2f || self.bodyChunks[1].submersion > 0.2f) && self.bodyMode != Player.BodyModeIndex.CorridorClimb)
                {
                    bool flag6;
                    bool flag7;
                    if (ModManager.DLCShared)
                    {
                        flag6 = self.room.PointSubmerged(self.bodyChunks[0].pos, 80f);
                        flag7 = self.room.PointSubmerged(self.bodyChunks[0].pos, (!flag5) ? 30f : 10f);
                    }
                    else
                    {
                        flag6 = (self.bodyChunks[0].pos.y < self.room.FloatWaterLevel(self.bodyChunks[0].pos) - 80f);
                        flag7 = (self.bodyChunks[0].pos.y < self.room.FloatWaterLevel(self.bodyChunks[0].pos) - (flag5 ? 10f : 30f));
                    }
                    if ((self.animation != Player.AnimationIndex.SurfaceSwim || flag5 || flag6) && flag7 && self.bodyChunks[1].submersion > (flag5 ? -1f : 0.6f))
                    {
                        self.bodyMode = Player.BodyModeIndex.Swimming;
                        self.animation = Player.AnimationIndex.DeepSwim;
                    }
                    else if ((!self.IsTileSolid(1, 0, -1) || self.bodyChunks[1].submersion == 1f) && self.animation != Player.AnimationIndex.BeamTip && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.GetUpOnBeam && self.animation != Player.AnimationIndex.GetUpToBeamTip && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.StandOnBeam && self.animation != Player.AnimationIndex.LedgeGrab && self.animation != Player.AnimationIndex.HangUnderVerticalBeam)
                    {
                        self.bodyMode = Player.BodyModeIndex.Swimming;
                        self.animation = Player.AnimationIndex.SurfaceSwim;
                    }
                }
            }
            if (self.EffectiveRoomGravity == 0f && (!ModManager.MMF || !self.submerged) && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.animation != Player.AnimationIndex.VineGrab)
            {
                self.bodyMode = Player.BodyModeIndex.ZeroG;
                if (self.animation != Player.AnimationIndex.ZeroGSwim && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                {
                    self.animation = ((self.room.GetTile(self.mainBodyChunk.pos).horizontalBeam || self.room.GetTile(self.mainBodyChunk.pos).verticalBeam) ? Player.AnimationIndex.ZeroGPoleGrab : Player.AnimationIndex.ZeroGSwim);
                }
            }
            if (self.playerInAntlers != null)
            {
                self.animation = Player.AnimationIndex.AntlerClimb;
            }
            if (self.tubeWorm != null)
            {
                bool flag8 = true;
                int num3 = 0;
                while (num3 < self.grasps.Length && flag8)
                {
                    if (self.grasps[num3] != null && self.grasps[num3].grabbed as TubeWorm == self.tubeWorm)
                    {
                        flag8 = false;
                    }
                    num3++;
                }
                if (flag8)
                {
                    self.tubeWorm = null;
                }
            }
            if (self.tubeWorm != null && self.tubeWorm.tongues[0].Attached && self.bodyMode == Player.BodyModeIndex.Default && self.bodyChunks[1].ContactPoint.y >= 0 && (self.animation == Player.AnimationIndex.GrapplingSwing || self.animation == Player.AnimationIndex.None))
            {
                self.animation = Player.AnimationIndex.GrapplingSwing;
            }
            else if (self.animation == Player.AnimationIndex.GrapplingSwing)
            {
                self.animation = Player.AnimationIndex.None;
            }
            if (self.vineGrabDelay > 0)
            {
                self.vineGrabDelay--;
            }
            if (self.animation != Player.AnimationIndex.VineGrab && self.vineGrabDelay == 0 && self.room.climbableVines != null && (!ModManager.MMF || self.animation != Player.AnimationIndex.ClimbOnBeam))
            {
                if (self.EffectiveRoomGravity > 0f && (self.wantToGrab > 0 || self.input[0].y > 0))
                {
                    int num4 = Custom.IntClamp((int)(Vector2.Distance(self.mainBodyChunk.lastPos, self.mainBodyChunk.pos) / 5f), 1, 10);
                    for (int l = 0; l < num4; l++)
                    {
                        Vector2 pos = Vector2.Lerp(self.mainBodyChunk.lastPos, self.mainBodyChunk.pos, (num4 > 1) ? ((float)l / (float)(num4 - 1)) : 0f);
                        ClimbableVinesSystem.VinePosition vinePosition = self.room.climbableVines.VineOverlap(pos, self.mainBodyChunk.rad);
                        if (vinePosition != null)
                        {
                            if (self.room.climbableVines.GetVineObject(vinePosition) is CoralNeuron)
                            {
                                self.room.PlaySound(SoundID.Grab_Neuron, self.mainBodyChunk);
                            }
                            else if (self.room.climbableVines.GetVineObject(vinePosition) is CoralStem)
                            {
                                self.room.PlaySound(SoundID.Grab_Coral_Stem, self.mainBodyChunk);
                            }
                            else if (self.room.climbableVines.GetVineObject(vinePosition) is DaddyCorruption.ClimbableCorruptionTube)
                            {
                                self.room.PlaySound(SoundID.Grab_Corruption_Tube, self.mainBodyChunk);
                            }
                            else if (self.room.climbableVines.GetVineObject(vinePosition) is ClimbableVine)
                            {
                                self.room.PlaySound(SoundID.Leaves, self.mainBodyChunk, false, 1f, 0.75f + UnityEngine.Random.value * 0.5f);
                            }
                            self.animation = Player.AnimationIndex.VineGrab;
                            self.vinePos = vinePosition;
                            self.wantToGrab = 0;
                            break;
                        }
                    }
                }
                else if (self.animation != Player.AnimationIndex.VineGrab && (self.input[0].x != 0 || self.input[0].y != 0) && self.EffectiveRoomGravity == 0f)
                {
                    ClimbableVinesSystem.VinePosition vinePosition2 = self.room.climbableVines.VineOverlap(self.mainBodyChunk.pos, self.mainBodyChunk.rad);
                    if (vinePosition2 != null)
                    {
                        if (self.room.climbableVines.GetVineObject(vinePosition2) is CoralNeuron)
                        {
                            self.room.PlaySound(SoundID.Grab_Neuron, self.mainBodyChunk);
                        }
                        else if (self.room.climbableVines.GetVineObject(vinePosition2) is CoralStem)
                        {
                            self.room.PlaySound(SoundID.Grab_Coral_Stem, self.mainBodyChunk);
                        }
                        else if (self.room.climbableVines.GetVineObject(vinePosition2) is DaddyCorruption.ClimbableCorruptionTube)
                        {
                            self.room.PlaySound(SoundID.Grab_Corruption_Tube, self.mainBodyChunk);
                        }
                        else if (self.room.climbableVines.GetVineObject(vinePosition2) is ClimbableVine)
                        {
                            self.room.PlaySound(SoundID.Leaves, self.mainBodyChunk, false, 1f, 0.75f + UnityEngine.Random.value * 0.5f);
                        }
                        self.animation = Player.AnimationIndex.VineGrab;
                        self.vinePos = vinePosition2;
                        self.wantToGrab = 0;
                    }
                }
            }
            if (self.animation == Player.AnimationIndex.VineGrab && (self.room.climbableVines == null || !self.room.climbableVines.vines.Contains(self.vinePos.vine)))
            {
                self.animation = Player.AnimationIndex.None;
            }
            self.dynamicRunSpeed[0] = 3.6f;
            self.dynamicRunSpeed[1] = 3.6f;
            float num5 = 2.4f;
            self.UpdateAnimation();
            if (self.bodyChunks[0].ContactPoint.x == self.input[0].x && self.input[0].x != 0 && self.bodyChunks[0].pos.y > self.room.MiddleOfTile(self.room.GetTilePosition(self.bodyChunks[0].pos)).y && (self.bodyMode == Player.BodyModeIndex.Default || self.bodyMode == Player.BodyModeIndex.WallClimb) && !self.IsTileSolid(0, -self.input[0].x, 0) && !self.IsTileSolid(0, 0, -2) && !self.IsTileSolid(0, self.input[0].x, 1))
            {
                self.animation = Player.AnimationIndex.LedgeGrab;
                self.bodyMode = Player.BodyModeIndex.Default;
            }
            if (self.bodyMode == Player.BodyModeIndex.Crawl)
            {
                self.crawlTurnDelay++;
            }
            else
            {
                self.crawlTurnDelay = 0;
            }
            if (self.standing && self.IsTileSolid(1, 0, 1))
            {
                self.standing = false;
            }
            if (self.input[0].y > 0 && self.input[1].y == 0 && !self.room.GetTile(self.bodyChunks[1].pos).verticalBeam && self.room.GetTile(self.bodyChunks[1].pos + new Vector2(0f, -20f)).verticalBeam)
            {
                self.animation = Player.AnimationIndex.BeamTip;
                self.bodyChunks[1].vel.x = 0f;
                self.bodyChunks[1].vel.y = 0f;
                self.wantToGrab = -1;
            }
            self.UpdateBodyMode();
            int num6 = (self.isSlugpup && self.playerState.isPup) ? 12 : 17;
            if (self.rollDirection != 0)
            {
                self.rollCounter++;
                num = self.rollDirection;
                self.bodyChunkConnections[0].distance = 10f;
                if (self.bodyMode != Player.BodyModeIndex.Default || self.rollCounter > 200)
                {
                    self.rollCounter = 0;
                    self.rollDirection = 0;
                }
            }
            else
            {
                self.bodyChunkConnections[0].distance = (float)num6;
            }
            self.bodyChunkConnections[0].type = ((self.corridorTurnDir != null) ? PhysicalObject.BodyChunkConnection.Type.Pull : PhysicalObject.BodyChunkConnection.Type.Normal);
            self.wantToGrab = ((self.input[0].y <= 0 || (ModManager.MSC && self.monkAscension) || self.Submersion > 0.9f) ? 0 : 1);
            if (self.wantToGrab > 0 && self.noGrabCounter == 0 && (self.bodyMode == Player.BodyModeIndex.Default || self.bodyMode == Player.BodyModeIndex.WallClimb || self.bodyMode == Player.BodyModeIndex.Stand || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || self.bodyMode == Player.BodyModeIndex.Swimming) && (self.timeSinceInCorridorMode >= 20 || self.bodyChunks[1].pos.y <= self.firstChunk.pos.y || self.room.GetTilePosition(self.bodyChunks[0].pos).x != self.room.GetTilePosition(self.bodyChunks[1].pos).x) && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.GetUpOnBeam && self.animation != Player.AnimationIndex.DeepSwim && self.animation != Player.AnimationIndex.HangUnderVerticalBeam && self.animation != Player.AnimationIndex.GetUpToBeamTip && self.animation != Player.AnimationIndex.VineGrab)
            {
                int x = self.room.GetTilePosition(self.bodyChunks[0].pos).x;
                int num7 = self.room.GetTilePosition(self.bodyChunks[0].lastPos).y;
                int num8 = self.room.GetTilePosition(self.bodyChunks[0].pos).y;
                if (num8 > num7)
                {
                    int num9 = num7;
                    num7 = num8;
                    num8 = num9;
                }
                for (int m = num7; m >= num8; m--)
                {
                    if (self.room.GetTile(x, m).horizontalBeam)
                    {
                        self.animation = Player.AnimationIndex.HangFromBeam;
                        self.room.PlaySound(SoundID.Slugcat_Grab_Beam, self.mainBodyChunk, false, 1f, 1f);
                        self.bodyChunks[0].vel.y = 0f;
                        BodyChunk bodyChunk8 = self.bodyChunks[1];
                        bodyChunk8.vel.y = bodyChunk8.vel.y * 0.25f;
                        self.bodyChunks[0].pos.y = self.room.MiddleOfTile(new IntVector2(x, m)).y;
                        break;
                    }
                }
                self.GrabVerticalPole();
                if (self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.room.GetTile(self.bodyChunks[0].pos + new Vector2(0f, 20f)).verticalBeam && !self.room.GetTile(self.bodyChunks[0].pos).verticalBeam)
                {
                    self.bodyChunks[0].pos = self.room.MiddleOfTile(self.bodyChunks[0].pos) + new Vector2(0f, 5f);
                    self.bodyChunks[0].vel *= 0f;
                    self.bodyChunks[1].vel = Vector2.ClampMagnitude(self.bodyChunks[1].vel, 9f);
                    self.animation = Player.AnimationIndex.HangUnderVerticalBeam;
                }
            }
            bool flag9 = false;
            if (self.bodyMode != Player.BodyModeIndex.CorridorClimb)
            {
                flag9 = true;
            }
            if (self.animation == Player.AnimationIndex.ClimbOnBeam || self.animation == Player.AnimationIndex.HangFromBeam || self.animation == Player.AnimationIndex.GetUpOnBeam || self.animation == Player.AnimationIndex.LedgeGrab || self.animation == Player.AnimationIndex.GrapplingSwing || self.animation == Player.AnimationIndex.AntlerClimb)
            {
                flag9 = false;
            }
            if (self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed))
            {
                float num10 = 1f + Mathf.Max(0f, self.grasps[0].grabbed.TotalMass - 0.2f);
                if (self.grasps[0].grabbed is Cicada)
                {
                    if (self.bodyMode == Player.BodyModeIndex.Default && self.animation == Player.AnimationIndex.None)
                    {
                        BodyChunk mainBodyChunk = self.mainBodyChunk;
                        mainBodyChunk.vel.y = mainBodyChunk.vel.y + (self.grasps[0].grabbed as Cicada).LiftPlayerPower * 1.2f;
                        BodyChunk bodyChunk9 = self.bodyChunks[1];
                        bodyChunk9.vel.y = bodyChunk9.vel.y + (self.grasps[0].grabbed as Cicada).LiftPlayerPower * 0.25f;
                        (self.grasps[0].grabbed as Cicada).currentlyLiftingPlayer = true;
                        if ((self.grasps[0].grabbed as Cicada).LiftPlayerPower > 0.6666667f)
                        {
                            self.standing = false;
                        }
                    }
                    else
                    {
                        BodyChunk mainBodyChunk2 = self.mainBodyChunk;
                        mainBodyChunk2.vel.y = mainBodyChunk2.vel.y + (self.grasps[0].grabbed as Cicada).LiftPlayerPower * 0.5f;
                        (self.grasps[0].grabbed as Cicada).currentlyLiftingPlayer = false;
                    }
                    if (self.bodyChunks[1].ContactPoint.y < 0 && self.bodyChunks[1].lastContactPoint.y == 0 && (self.grasps[0].grabbed as Cicada).LiftPlayerPower > 0.33333334f)
                    {
                        self.standing = true;
                    }
                    num10 = 1f + Mathf.Max(0f, self.grasps[0].grabbed.TotalMass - 0.2f) * 1.5f;
                    num10 = Mathf.Lerp(num10, 1f, Mathf.Pow(Mathf.InverseLerp(0.1f, 0.5f, (self.grasps[0].grabbed as Cicada).LiftPlayerPower), 0.2f));
                }
                else if (self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.Drag)
                {
                    if (self.bodyMode == Player.BodyModeIndex.Default || self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.Stand || self.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        num10 = 1f;
                    }
                    if (self.room.aimap != null)
                    {
                        if (self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace)
                        {
                            self.grasps[0].grabbedChunk.vel += self.input[0].IntVec.ToVector2().normalized * self.slugcatStats.corridorClimbSpeedFac * 4f / Mathf.Max(0.75f, self.grasps[0].grabbed.TotalMass);
                        }
                        for (int n = 0; n < self.grasps[0].grabbed.bodyChunks.Length; n++)
                        {
                            if (self.room.aimap.getAItile(self.grasps[0].grabbed.bodyChunks[n].pos).narrowSpace)
                            {
                                self.grasps[0].grabbed.bodyChunks[n].vel *= 0.8f;
                                BodyChunk bodyChunk10 = self.grasps[0].grabbed.bodyChunks[n];
                                bodyChunk10.vel.y = bodyChunk10.vel.y + self.EffectiveRoomGravity * self.grasps[0].grabbed.gravity * 0.85f;
                                self.grasps[0].grabbed.bodyChunks[n].vel += self.input[0].IntVec.ToVector2().normalized * self.slugcatStats.corridorClimbSpeedFac * 1.5f / ((float)self.grasps[0].grabbed.bodyChunks.Length * Mathf.Max(1f, (self.grasps[0].grabbed.TotalMass + 1f) / 2f));
                                self.grasps[0].grabbed.bodyChunks[n].pos += self.input[0].IntVec.ToVector2().normalized * self.slugcatStats.corridorClimbSpeedFac * 1.1f / ((float)self.grasps[0].grabbed.bodyChunks.Length * Mathf.Max(1f, (self.grasps[0].grabbed.TotalMass + 2f) / 3f));
                            }
                        }
                    }
                }
                if (self.shortcutDelay < 1 && self.enteringShortCut == null && (self.input[0].x == 0 || self.input[0].y == 0) && (self.input[0].x != 0 || self.input[0].y != 0))
                {
                    for (int num11 = 0; num11 < self.grasps[0].grabbed.bodyChunks.Length; num11++)
                    {
                        if (self.room.GetTile(self.room.GetTilePosition(self.grasps[0].grabbed.bodyChunks[num11].pos) + self.input[0].IntVec).Terrain == Room.Tile.TerrainType.ShortcutEntrance && self.room.ShorcutEntranceHoleDirection(self.room.GetTilePosition(self.grasps[0].grabbed.bodyChunks[num11].pos) + self.input[0].IntVec) == new IntVector2(-self.input[0].x, -self.input[0].y))
                        {
                            ShortcutData.Type shortCutType = self.room.shortcutData(self.room.GetTilePosition(self.grasps[0].grabbed.bodyChunks[num11].pos) + self.input[0].IntVec).shortCutType;
                            if (shortCutType == ShortcutData.Type.RoomExit || shortCutType == ShortcutData.Type.Normal)
                            {
                                self.enteringShortCut = new IntVector2?(self.room.GetTilePosition(self.grasps[0].grabbed.bodyChunks[num11].pos) + self.input[0].IntVec);
                                Custom.Log(new string[]
                                {
                                "player pulled into shortcut by carried object"
                                });
                                if (ModManager.MSC && self.tongue != null && self.tongue.Attached)
                                {
                                    self.tongue.Release();
                                    break;
                                }
                                break;
                            }
                        }
                    }
                }
                self.dynamicRunSpeed[0] /= num10;
                self.dynamicRunSpeed[1] /= num10;
            }
            self.dynamicRunSpeed[0] *= Mathf.Lerp(1f, 1.5f, self.Adrenaline);
            self.dynamicRunSpeed[1] *= Mathf.Lerp(1f, 1.5f, self.Adrenaline);
            num5 *= Mathf.Lerp(1f, 1.2f, self.Adrenaline);
            if (flag9 && (self.dynamicRunSpeed[0] > 0f || self.dynamicRunSpeed[1] > 0f))
            {
                if (self.slowMovementStun > 0)
                {
                    self.dynamicRunSpeed[0] *= 0.5f + 0.5f * Mathf.InverseLerp(10f, 0f, (float)self.slowMovementStun);
                    self.dynamicRunSpeed[1] *= 0.5f + 0.5f * Mathf.InverseLerp(10f, 0f, (float)self.slowMovementStun);
                    num5 *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0f, (float)self.slowMovementStun);
                }
                if (self.bodyMode == Player.BodyModeIndex.Default && self.bodyChunks[0].ContactPoint.x == 0 && self.bodyChunks[0].ContactPoint.y == 0 && self.bodyChunks[1].ContactPoint.x == 0 && self.bodyChunks[1].ContactPoint.y == 0)
                {
                    num5 *= self.EffectiveRoomGravity;
                }
                for (int num12 = 0; num12 < 2; num12++)
                {
                    if (num < 0)
                    {
                        float num13 = num5 * self.surfaceFriction;
                        if (self.bodyChunks[num12].vel.x - num13 < -self.dynamicRunSpeed[num12])
                        {
                            num13 = self.dynamicRunSpeed[num12] + self.bodyChunks[num12].vel.x;
                        }
                        if (num13 > 0f)
                        {
                            BodyChunk bodyChunk11 = self.bodyChunks[num12];
                            bodyChunk11.vel.x = bodyChunk11.vel.x - num13;
                        }
                    }
                    else if (num > 0)
                    {
                        float num14 = num5 * self.surfaceFriction;
                        if (self.bodyChunks[num12].vel.x + num14 > self.dynamicRunSpeed[num12])
                        {
                            num14 = self.dynamicRunSpeed[num12] - self.bodyChunks[num12].vel.x;
                        }
                        if (num14 > 0f)
                        {
                            BodyChunk bodyChunk12 = self.bodyChunks[num12];
                            bodyChunk12.vel.x = bodyChunk12.vel.x + num14;
                        }
                    }
                    if (self.bodyChunks[0].ContactPoint.y != 0 || self.bodyChunks[1].ContactPoint.y != 0)
                    {
                        float num15 = 0f;
                        if (self.input[0].x != 0)
                        {
                            num15 = Mathf.Clamp(self.bodyChunks[num12].vel.x, -self.dynamicRunSpeed[num12], self.dynamicRunSpeed[num12]);
                        }
                        BodyChunk bodyChunk13 = self.bodyChunks[num12];
                        bodyChunk13.vel.x = bodyChunk13.vel.x + (num15 - self.bodyChunks[num12].vel.x) * Mathf.Pow(self.surfaceFriction, 1.5f);
                    }
                }
            }
            int num16 = 0;
            if (self.superLaunchJump > 0 && self.killSuperLaunchJumpCounter < 1)
            {
                num16 = 1;
            }
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.bodyChunks[0].ContactPoint.y < 0 && self.bodyChunks[1].ContactPoint.y < 0)
            {
                if (self.input[0].y == 0)
                {
                    num16 = 0;
                    self.wantToJump = 0;
                    if (self.input[0].jmp)
                    {
                        if (self.superLaunchJump < 20)
                        {
                            if (self.KarmaCap >= 4 || Karma11Update.VoidKarma11)
                            {
                                self.superLaunchJump += 2;
                            }
                            else
                            {
                                self.superLaunchJump++;
                            }
                        }
                        else
                        {
                            self.killSuperLaunchJumpCounter = 15;
                        }
                    }
                }
                if (!self.input[0].jmp && self.input[1].jmp)
                {
                    self.wantToJump = 1;
                }
            }
            if (self.killSuperLaunchJumpCounter > 0)
            {
                self.killSuperLaunchJumpCounter--;
            }
            if (self.simulateHoldJumpButton > 0)
            {
                self.simulateHoldJumpButton--;
            }
            if (self.canJump > 0 && self.wantToJump > 0)
            {
                self.canJump = 0;
                self.wantToJump = 0;
                self.Jump();
            }
            else if (self.canWallJump != 0 && self.wantToJump > 0 && self.input[0].x != -Math.Sign(self.canWallJump))
            {
                self.WallJump(Math.Sign(self.canWallJump));
                self.wantToJump = 0;
            }
            else if (self.jumpChunkCounter > 0 && self.wantToJump > 0)
            {
                self.jumpChunkCounter = -5;
                self.wantToJump = 0;
                self.JumpOnChunk();
            }
            if (self.Adrenaline > 0f)
            {
                float num17 = (self.isRivulet ? 16f : 8f) * self.Adrenaline;
                if (self.input[0].x < 0)
                {
                    if (!self.IsTileSolid(0, -1, 0) && self.directionBoosts[0] == 1f)
                    {
                        self.directionBoosts[0] = 0f;
                        BodyChunk mainBodyChunk3 = self.mainBodyChunk;
                        mainBodyChunk3.vel.x = mainBodyChunk3.vel.x - num17;
                        BodyChunk bodyChunk14 = self.bodyChunks[1];
                        bodyChunk14.vel.x = bodyChunk14.vel.x + num17 / 3f;
                    }
                }
                else if (self.directionBoosts[0] == 0f)
                {
                    self.directionBoosts[0] = 0.01f;
                }
                if (self.input[0].x > 0)
                {
                    if (!self.IsTileSolid(0, 1, 0) && self.directionBoosts[1] == 1f)
                    {
                        self.directionBoosts[1] = 0f;
                        BodyChunk mainBodyChunk4 = self.mainBodyChunk;
                        mainBodyChunk4.vel.x = mainBodyChunk4.vel.x + num17;
                        BodyChunk bodyChunk15 = self.bodyChunks[1];
                        bodyChunk15.vel.x = bodyChunk15.vel.x - num17 / 3f;
                    }
                }
                else if (self.directionBoosts[1] == 0f)
                {
                    self.directionBoosts[1] = 0.01f;
                }
                if (self.input[0].y < 0)
                {
                    if (!self.IsTileSolid(0, 0, -1) && self.directionBoosts[2] == 1f)
                    {
                        self.directionBoosts[2] = 0f;
                        BodyChunk mainBodyChunk5 = self.mainBodyChunk;
                        mainBodyChunk5.vel.y = mainBodyChunk5.vel.y - num17;
                        BodyChunk bodyChunk16 = self.bodyChunks[1];
                        bodyChunk16.vel.y = bodyChunk16.vel.y + num17 / 3f;
                    }
                }
                else if (self.directionBoosts[2] == 0f)
                {
                    self.directionBoosts[2] = 0.01f;
                }
                if (self.input[0].y > 0)
                {
                    if (!self.IsTileSolid(0, 0, 1) && self.directionBoosts[3] == 1f)
                    {
                        self.directionBoosts[3] = 0f;
                        BodyChunk mainBodyChunk6 = self.mainBodyChunk;
                        mainBodyChunk6.vel.y = mainBodyChunk6.vel.y + num17;
                        BodyChunk bodyChunk17 = self.bodyChunks[1];
                        bodyChunk17.vel.y = bodyChunk17.vel.y - num17;
                    }
                }
                else if (self.directionBoosts[3] == 0f)
                {
                    self.directionBoosts[3] = 0.01f;
                }
            }
            self.superLaunchJump -= num16;
            if (self.shortcutDelay < 1 && (!ModManager.MSC || (self.onBack == null && (self.grabbedBy.Count == 0 || !(self.grabbedBy[0].grabber is Player)))))
            {
                for (int num18 = 0; num18 < 2; num18++)
                {
                    if (self.enteringShortCut == null && self.room.GetTile(self.bodyChunks[num18].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance && self.room.shortcutData(self.room.GetTilePosition(self.bodyChunks[num18].pos)).shortCutType != ShortcutData.Type.DeadEnd && self.room.shortcutData(self.room.GetTilePosition(self.bodyChunks[num18].pos)).shortCutType != ShortcutData.Type.CreatureHole && self.room.shortcutData(self.room.GetTilePosition(self.bodyChunks[num18].pos)).shortCutType != ShortcutData.Type.NPCTransportation)
                    {
                        IntVector2 intVector = self.room.ShorcutEntranceHoleDirection(self.room.GetTilePosition(self.bodyChunks[num18].pos));
                        if (self.input[0].x == -intVector.x && self.input[0].y == -intVector.y)
                        {
                            self.enteringShortCut = new IntVector2?(self.room.GetTilePosition(self.bodyChunks[num18].pos));
                            if (ModManager.MSC && self.tongue != null && self.tongue.Attached)
                            {
                                self.tongue.Release();
                            }
                        }
                    }
                }
            }
            self.GrabUpdate(eu);
        }
        else
        {
            orig(self, eu);
        }
    }
}
