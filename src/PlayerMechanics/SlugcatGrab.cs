using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;
using Watcher;

namespace VoidTemplate.PlayerMechanics;
public static class SlugcatGrab
{
    public static void Hook()
    {
        //On.Player.SlugcatGrab += Player_SlugcatGrab;
    }

    private static void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
    {
        if (!self.AreVoidViy())
        {
            orig(self, obj, graspUsed);
            return;
        }
        if (ModManager.MSC && obj is MoonCloak && self.AI == null && self.room != null && self.room.game.IsStorySession && self.room.abstractRoom.name != "SL_AI")
        {
            AbstractPhysicalObject abstractPhysicalObject = obj.abstractPhysicalObject;
            abstractPhysicalObject.realizedObject.RemoveFromRoom();
            abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject);
            self.switchHandsCounter = 0;
            self.wantToPickUp = 0;
            self.noPickUpOnRelease = 20;
            self.room.game.GetStorySession.saveState.wearingCloak = true;
            return;
        }
        if (obj is IPlayerEdible && (!ModManager.MMF || (obj is Creature && (obj as Creature).dead) || !(obj is Centipede) || (obj is Centipede && (obj as Centipede).Small)))
        {
            self.Grab(obj, graspUsed, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
        }
        int chunkGrabbed = 0;
        if (self.Grabability(obj) == Player.ObjectGrabability.Drag)
        {
            float dst = float.MaxValue;
            for (int i = 0; i < obj.bodyChunks.Length; i++)
            {
                if (Custom.DistLess(self.mainBodyChunk.pos, obj.bodyChunks[i].pos, dst))
                {
                    dst = Vector2.Distance(self.mainBodyChunk.pos, obj.bodyChunks[i].pos);
                    chunkGrabbed = i;
                }
            }
        }
        self.switchHandsCounter = 0;
        self.wantToPickUp = 0;
        self.noPickUpOnRelease = 20;
        if (self.isSlugpup)
        {
            Custom.Log(new string[]
            {
                "Player slugpup grab limiter"
            });
            if (self.grasps[0] != null)
            {
                self.ReleaseGrasp(0);
            }
            if (self.grasps[1] != null)
            {
                self.ReleaseGrasp(1);
            }
            graspUsed = 0;
        }
        bool flag = true;
        if (obj is Creature)
        {
            if (self.IsCreatureImmuneToPlayerGrabStun(obj as Creature))
            {
                flag = false;
            }
            else if (!(obj as Creature).dead && !self.IsCreatureLegalToHoldWithoutStun(obj as Creature))
            {
                flag = false;
            }
        }
        if (obj is SandGrub)
        {
            chunkGrabbed = 1;
        }
        else if (obj is Tardigrade)
        {
            chunkGrabbed = 2;
        }
        self.Grab(obj, graspUsed, chunkGrabbed, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true, (ModManager.MMF || ModManager.CoopAvailable) ? flag : (!(obj is Cicada) && !(obj is JetFish)));
    }
}
