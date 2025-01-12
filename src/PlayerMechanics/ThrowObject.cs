using Expedition;
using Kittehface.Framework20;
using Menu;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class ThrowObject
{
    public static void Hook()
    {
        On.Player.ThrowObject += Player_ThrowObject;
    }

    private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (self.abstractCreature.world.game.session is StoryGameSession session && IsViy(session.saveState) && self.grasps[grasp].grabbed is Spear)
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
        else if (self.slugcatStats.name == VoidEnums.SlugcatID.Void
        && self.bodyMode == BodyModeIndexExtension.CeilCrawl
        && self.input[0].jmp)
        {
            Creature.Grasp[] grasps = self.grasps;
            object obj = grasps?[grasp]?.grabbed;
            orig(self, grasp, eu);
            if (obj is Weapon weapon)
            {
                for (int i = 0; i < weapon.bodyChunks.Length; i++)
                {
                    BodyChunk bodyChunk = weapon.bodyChunks[i];
                    if (self.input[0].x == 0)
                    {
                        bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(0, -1) * 10f;
                        bodyChunk.vel = new Vector2(0, -1) * 40f;
                    }
                    else if (self.input[0].x > 0)
                    {
                        bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(1, -1) * 10f;
                        bodyChunk.vel = new Vector2(0.71f, -0.71f) * 40f;
                    }
                    else if (self.input[0].x < 0)
                    {
                        bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(-1, -1) * 10f;
                        bodyChunk.vel = new Vector2(-0.71f, -0.71f) * 40f;
                    }
                }
                if (self.input[0].x == 0)
                    weapon.setRotation = new Vector2?(new Vector2(0, -1));
                else if (self.input[0].x > 0)
                    weapon.setRotation = new Vector2?(new Vector2(1, -1));
                else if (self.input[0].x < 0)
                    weapon.setRotation = new Vector2?(new Vector2(-1, -1));
            }
        }
        else
            orig(self, grasp, eu);
    }
}
