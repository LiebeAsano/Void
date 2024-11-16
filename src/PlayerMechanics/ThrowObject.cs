using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics;

internal static class ThrowObject
{
    public static void Hook()
    {
        On.Player.ThrowObject += Player_ThrowObject;
    }

    private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void
        && self.bodyMode == BodyModeIndexExtension.CeilCrawl
        && self.input[0].x == 0
        && !self.input[0].jmp)
        {
            Creature.Grasp[] grasps = self.grasps;
            object obj = grasps?[grasp]?.grabbed;
            orig(self, grasp, eu);
            if (obj is Weapon weapon)
            {
                for (int i = 0; i < weapon.bodyChunks.Length; i++)
                {
                    BodyChunk bodyChunk = weapon.bodyChunks[i];
                    bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(0, -1) * 10f;
                    bodyChunk.vel = new Vector2(0, -1) * 40f;
                }
                weapon.setRotation = new Vector2?(new Vector2(0, -1));
            }
        }
        else
            orig(self, grasp, eu);
    }
}
