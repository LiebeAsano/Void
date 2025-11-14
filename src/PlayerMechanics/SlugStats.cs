using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics
{
    public static class SlugStats
    {
        public static void Hook()
        {
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
        }

        public static int illness;

        private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            orig(self);
            if (!VoidDreamScript.IsVoidDream)
            {
                illness = 0;
            }
            if (VoidDreamScript.IsVoidDream)
            {
                illness++;
                if (illness <= 1800)
                {
                    if (illness == 1800)
                        HunterSpasms.Spasm(self, 10f, 1f);
                    self.slugcatStats.throwingSkill = 2;
                    self.slugcatStats.corridorClimbSpeedFac = 1.25f;
                    self.slugcatStats.poleClimbSpeedFac = 1.25f;
                    self.slugcatStats.runspeedFac = 1.2f;
                    self.slugcatStats.bodyWeightFac = 1.12f;
                }
                else if (illness <= 3600)
                {
                    if (illness == 3600)
                        HunterSpasms.Spasm(self, 10f, 1f);
                    self.slugcatStats.throwingSkill = 1;
                    self.slugcatStats.corridorClimbSpeedFac = 1.1f;
                    self.slugcatStats.poleClimbSpeedFac = 1.1f;
                    self.slugcatStats.runspeedFac = 1.1f;
                    self.slugcatStats.bodyWeightFac = 1.05f;
                }
                else if (illness <= 5400)
                {
                    if (illness == 5400)
                        HunterSpasms.Spasm(self, 10f, 1f);
                    self.slugcatStats.throwingSkill = 1;
                    self.slugcatStats.corridorClimbSpeedFac = 1.0f;
                    self.slugcatStats.poleClimbSpeedFac = 1.0f;
                    self.slugcatStats.runspeedFac = 1.0f;
                    self.slugcatStats.bodyWeightFac = 1.0f;
                }
                else if (illness <= 7200)
                {
                    if (illness == 7200)
                        HunterSpasms.Spasm(self, 10f, 1f);
                    self.slugcatStats.throwingSkill = 0;
                    self.slugcatStats.corridorClimbSpeedFac = 0.9f;
                    self.slugcatStats.poleClimbSpeedFac = 0.9f;
                    self.slugcatStats.runspeedFac = 0.9f;
                    self.slugcatStats.bodyWeightFac = 0.9f;
                }
                else
                {
                    self.slugcatStats.throwingSkill = 0;
                    self.slugcatStats.corridorClimbSpeedFac = 0.8f;
                    self.slugcatStats.poleClimbSpeedFac = 0.8f;
                    self.slugcatStats.runspeedFac = 0.8f;
                    self.slugcatStats.bodyWeightFac = 0.8f;
                }
            }
            else if (self.IsVoid())
            {
                float crawlSpeed;
                if (self.KarmaCap == 10)
                {
                    if (Karma11Update.VoidKarma11)
                    {
                        self.slugcatStats.throwingSkill = 2;
                        self.slugcatStats.corridorClimbSpeedFac = 1.25f;
                        self.slugcatStats.poleClimbSpeedFac = 1.25f;
                        self.slugcatStats.runspeedFac = 1.25f;
                        self.slugcatStats.bodyWeightFac = 1.2f;
                        crawlSpeed = 2.0f;
                    }
                    else
                    {
                        self.slugcatStats.throwingSkill = 0;
                        self.slugcatStats.corridorClimbSpeedFac = 0.9f;
                        self.slugcatStats.poleClimbSpeedFac = 0.9f;
                        self.slugcatStats.runspeedFac = 0.9f;
                        self.slugcatStats.bodyWeightFac = 0.9f;
                        crawlSpeed = 1.0f;
                    }
                }
                else if (Karma11Update.VoidKarma11 || self.KarmaCap >= 4)
                {
                    self.slugcatStats.throwingSkill = 2;
                    self.slugcatStats.corridorClimbSpeedFac = 1.25f;
                    self.slugcatStats.poleClimbSpeedFac = 1.25f;
                    self.slugcatStats.runspeedFac = 1.25f;
                    self.slugcatStats.bodyWeightFac = 1.2f;
                    crawlSpeed = 2.0f;
                }
                else if (self.KarmaCap == 3)
                {
                    self.slugcatStats.throwingSkill = 1;
                    self.slugcatStats.corridorClimbSpeedFac = 1.15f;
                    self.slugcatStats.poleClimbSpeedFac = 1.15f;
                    self.slugcatStats.runspeedFac = 1.15f;
                    self.slugcatStats.bodyWeightFac = 1.1f;
                    crawlSpeed = 1.5f;
                }
                else
                {
                    self.slugcatStats.throwingSkill = 1;
                    self.slugcatStats.corridorClimbSpeedFac = 1.05f;
                    self.slugcatStats.poleClimbSpeedFac = 1.05f;
                    self.slugcatStats.runspeedFac = 1.05f;
                    self.slugcatStats.bodyWeightFac = 1f;
                    crawlSpeed = 1.0f;
                }
                if (self.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    self.dynamicRunSpeed[0] *= crawlSpeed;
                }
            }
            if (self.IsViy())
            {
                if (self.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    self.dynamicRunSpeed[0] *= 2.5f;
                }
            }
        }
    }
}
