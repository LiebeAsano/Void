using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

public static class JumpUnderWater
{
    public static void Hook()
    {
        On.Player.UpdateAnimation += Player_UpdateAnimation;
    }

    private static void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
    {
        if (self.animation == Player.AnimationIndex.DeepSwim && (self.slugcatStats.name == VoidEnums.SlugcatID.Void || self.slugcatStats.name == VoidEnums.SlugcatID.Viy))
        {
            int karmaCap = self.KarmaCap;
            if (self.IsViy() || Karma11Update.VoidKarma11)
            {
                karmaCap = 10;
            }
            self.dynamicRunSpeed[0] = 0f;
            self.dynamicRunSpeed[1] = 0f;
            if (self.grasps[0] != null && self.grasps[0].grabbed is JetFish && (self.grasps[0].grabbed as JetFish).Consious)
            {
                self.waterFriction = 1f;
                return;
            }
            self.canJump = 0;
            self.standing = false;
            self.GoThroughFloors = true;
            float num = (Mathf.Abs(Vector2.Dot(self.bodyChunks[0].vel.normalized, (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized)) + Mathf.Abs(Vector2.Dot(self.bodyChunks[1].vel.normalized, (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized))) / 2f;
            if (self.input[0].jmp && !self.input[1].jmp && self.airInLungs > 0.1f)
            {
                if (self.waterJumpDelay == 0)
                {
                    self.swimCycle = 2.7f;
                    float num2 = 1f;
                    if (ModManager.MMF && MMF.cfgFreeSwimBoosts.Value)
                    {
                        num2 = 0f;
                    }
                    self.swimCycle = 2.7f;
                    Vector2 vector = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
                    self.bodyChunks[0].vel += vector * 3f * 0.2f * karmaCap;
                    self.airInLungs -= (0.2f - 0.02f * karmaCap) * num2;
                }
                else
                {
                    self.swimCycle = 0f;
                }
                self.waterJumpDelay = 20 - karmaCap;
            }
            self.swimCycle += 0.01f;
            if (self.input[0].ZeroGGamePadIntVec.x != 0 || self.input[0].ZeroGGamePadIntVec.y != 0)
            {
                float value = Vector2.Angle(self.bodyChunks[0].lastPos - self.bodyChunks[1].lastPos, self.bodyChunks[0].pos - self.bodyChunks[1].pos);
                float num3 = 0.2f + Mathf.InverseLerp(0f, 12f, value) * 0.8f;
                if (self.slowMovementStun > 0)
                {
                    num3 *= 0.5f;
                }
                if (num3 > self.swimForce)
                {
                    self.swimForce = Mathf.Lerp(self.swimForce, num3, 0.7f);
                }
                else
                {
                    self.swimForce = Mathf.Lerp(self.swimForce, num3, 0.05f);
                }
                self.swimCycle += Mathf.Lerp(self.swimForce, 1f, 0.5f) / 10f;
                if (self.airInLungs < 0.5f && self.airInLungs > 0.16666667f)
                {
                    self.swimCycle += 0.05f;
                }
                if (self.bodyChunks[0].ContactPoint.x != 0 || self.bodyChunks[0].ContactPoint.y != 0)
                {
                    self.swimForce *= 0.5f;
                }
                if (self.swimCycle > 4f)
                {
                    self.swimCycle = 0f;
                }
                else if (self.swimCycle > 3f)
                {
                    self.bodyChunks[0].vel += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 0.7f * Mathf.Lerp(self.swimForce, 1f, 0.5f) * self.bodyChunks[0].submersion;
                }
                Vector2 vector3 = self.SwimDir(true);
                if (self.airInLungs < 0.3f)
                {
                    float num4 = self.airInLungs;
                    if (ModManager.MMF && MMF.cfgSwimBreathLeniency.Value && num4 > 0f)
                    {
                        if (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                        {
                            num4 = Mathf.Lerp(self.airInLungs, self.airInLungs, 0.9f + Mathf.Log(self.airInLungs + 0.12f));
                        }
                    }
                    vector3 = Vector3.Slerp(vector3, new Vector2(0f, 1f), Mathf.InverseLerp(0.3f, 0f, num4));
                }
                if (ModManager.MSC && self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && (self.grasps[0].grabbed as EnergyCell).usingTime > 0f && self.grasps[0].grabbed.Submersion != 0f)
                {
                    self.bodyChunks[0].vel += vector3 * 0.5f * self.swimForce * Mathf.Lerp(num, 1f, 0.5f) * self.bodyChunks[0].submersion * 3f;
                    self.bodyChunks[1].vel -= vector3 * 0.1f * self.bodyChunks[0].submersion;
                    self.bodyChunks[0].vel += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 0.4f * self.swimForce * num * self.bodyChunks[0].submersion * 3f;
                    if (self.bodyChunks[0].vel.magnitude > 75f)
                    {
                        self.bodyChunks[0].vel = self.bodyChunks[0].vel.normalized * 75f;
                    }
                }
                else
                {
                    self.bodyChunks[0].vel += vector3 * 0.5f * self.swimForce * Mathf.Lerp(num, 1f, 0.5f) * self.bodyChunks[0].submersion;
                    self.bodyChunks[1].vel -= vector3 * 0.1f * self.bodyChunks[0].submersion;
                    self.bodyChunks[0].vel += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 0.4f * self.swimForce * num * self.bodyChunks[0].submersion;
                }
                if (self.bodyChunks[0].vel.magnitude < 3f)
                {
                    self.bodyChunks[0].vel += vector3 * 0.2f * Mathf.InverseLerp(3f, 1.5f, self.bodyChunks[0].vel.magnitude);
                    self.bodyChunks[1].vel -= vector3 * 0.1f * Mathf.InverseLerp(3f, 1.5f, self.bodyChunks[0].vel.magnitude);
                }
            }
            else
            {
                self.waterFriction = Mathf.Lerp(0.92f, 0.96f, num);
            }
            if (self.bodyMode != Player.BodyModeIndex.Swimming)
            {
                self.animation = Player.AnimationIndex.None;
                return;
            }
        }
        else
        {
            orig(self);
        }
    }
}
