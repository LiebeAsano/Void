using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics
{
    internal static class SlugStats
    {
        public static void Hook()
        {
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
        }

        private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            orig(self);
            if (self.IsVoid())
            {
                float crawlSpeed;
                if (self.KarmaCap >= 4 || Karma11Update.VoidKarma11)
                {
                    self.slugcatStats.throwingSkill = 2;
                    self.slugcatStats.corridorClimbSpeedFac = 1.25f;
                    self.slugcatStats.poleClimbSpeedFac = 1.25f;
                    self.slugcatStats.runspeedFac = 1.25f;
                    self.slugcatStats.bodyWeightFac = 1.2f;
                    crawlSpeed = 2f;
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
                    crawlSpeed = 1f;
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
