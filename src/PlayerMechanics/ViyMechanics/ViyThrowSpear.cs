using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.ViyMechanics
{
    internal static class ViyThrowSpear
    {
        public static void Hook()
        {
            On.Player.ThrowObject += Player_ThrowObject;
        }

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (self.slugcatStats.name == VoidEnums.SlugcatID.Viy && self.grasps[grasp].grabbed is Weapon)
            {

                IntVector2 intVector = new(self.ThrowDirection, 0);
                self.TossObject(grasp, eu);
                if (self.animation == Player.AnimationIndex.ClimbOnBeam && ModManager.MMF && MMF.cfgClimbingGrip.Value)
                {
                    self.bodyChunks[0].vel += intVector.ToVector2() * 2f;
                    self.bodyChunks[1].vel -= intVector.ToVector2() * 8f;
                }
                else
                {
                    self.bodyChunks[0].vel += intVector.ToVector2() * 8f;
                    self.bodyChunks[1].vel -= intVector.ToVector2() * 4f;
                }
                if (self.graphicsModule != null)
                {
                    (self.graphicsModule as PlayerGraphics).ThrowObject(grasp, self.grasps[grasp].grabbed);
                }
                self.Blink(15);

                self.dontGrabStuff = (self.isNPC ? 45 : 15);
                if (self.graphicsModule != null)
                {
                    (self.graphicsModule as PlayerGraphics).LookAtObject(self.grasps[grasp].grabbed);
                }
                if (self.grasps[grasp].grabbed is PlayerCarryableItem)
                {
                    (self.grasps[grasp].grabbed as PlayerCarryableItem).Forbid();
                }
                self.ReleaseGrasp(grasp);
            }
            else
            {
                orig(self, grasp, eu);
            }
        }
    }
}
