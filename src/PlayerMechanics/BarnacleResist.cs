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

internal class BarnacleResist
{
    public static void Hook()
    {
        On.Player.CanMaulCreature += Player_CanMaulCreature;
        On.Watcher.Barnacle.BitByPlayer += Barnacle_BitByPlayer;
        On.Watcher.Barnacle.Collide += Barnacle_Colide;
        On.Watcher.Barnacle.Violence += Barnacle_Violence;
    }

    private static bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (crit is Barnacle && !crit.dead && self != null && (self.slugcatStats.name == VoidEnums.SlugcatID.Void || self.slugcatStats.name == VoidEnums.SlugcatID.Viy))
        {
            return true;
        }
        return orig(self, crit);
    }

    private static void Barnacle_BitByPlayer(On.Watcher.Barnacle.orig_BitByPlayer orig, Barnacle self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && player.IsVoid())
        {
            self.bites -= 1;
        }
        orig(self, grasp, eu);
    }

    private static void Barnacle_Colide(On.Watcher.Barnacle.orig_Collide orig, Watcher.Barnacle self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        if (!self.Consious)
        {
            return;
        }
        Barnacle barnacle = otherObject as Barnacle;
        if (barnacle == null)
        {
            if (self.hasShell && otherObject is Creature && (otherObject as Creature).Consious && otherObject is Player player && (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy) && self.shakeCooldown <= 0 && (self.AI.behavior == BarnacleAI.Behavior.Idle || self.AI.behavior == BarnacleAI.Behavior.Travelling))
            {
                self.room.PlaySound(WatcherEnums.WatcherSoundID.Barnacle_Push_Away_Nearby_Creatures, self.mainBodyChunk);
                self.shakeTime = UnityEngine.Random.Range(10, 20);
                self.shakeCooldown = 80;
                Vector2 a = Vector2.Lerp(otherObject.bodyChunks[otherChunk].pos, self.bodyChunks[myChunk].pos, 0.5f);
                for (int j = 0; j < otherObject.bodyChunks.Length; j++)
                {
                    otherObject.bodyChunks[j].vel = Custom.DirVec(self.mainBodyChunk.pos, otherObject.bodyChunks[j].pos) * UnityEngine.Random.Range(3f, 8f);
                }
                for (int k = 0; k < 5; k++)
                {
                    self.room.AddObject(new Spark(a + Custom.RNV() * 10f, otherObject.bodyChunks[otherChunk].vel * -0.1f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.2f, 0.4f, UnityEngine.Random.value) * otherObject.bodyChunks[otherChunk].vel.magnitude, new Color(1f, 1f, 1f), null, 20, 80));
                }
                return;
            }
        }
        orig(self, otherObject, myChunk, otherChunk);
    }

    private static void Barnacle_Violence(On.Watcher.Barnacle.orig_Violence orig, Barnacle self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        if (!self.RippleViolenceCheck(source))
        {
            return;
        }
        if (source?.owner is Player player && (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy))
        {
            VoidLoseShell(self);
        }
        orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
    }

    private static void VoidLoseShell(Barnacle self)
    {
        if (self.hasShell)
        {
            int num = 0;
            while ((float)num < Mathf.Lerp(2f, 5f, self.creatureParams.sizeMultiplier))
            {
                AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(self.room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos + Custom.RNV() * self.mainBodyChunk.rad), self.room.game.GetNewID());
                self.room.abstractRoom.entities.Add(abstractPhysicalObject);
                abstractPhysicalObject.RealizeInRoom();
                abstractPhysicalObject.realizedObject.firstChunk.vel = Custom.RNV() * UnityEngine.Random.Range(8f, 16f);
                num++;
            }
            int num2 = 0;
            while ((float)num2 < Mathf.Lerp(4f, 7f, self.creatureParams.sizeMultiplier))
            {
                Vector2 vector = Custom.RNV() * 4f;
                vector = new Vector2(vector.x, Mathf.Abs(vector.y));
                int num3 = UnityEngine.Random.Range(0, 4);
                string overrideSprite = "RootBall1";
                if (num3 == 1)
                {
                    overrideSprite = "KrakenShield0";
                }
                else if (num3 == 2)
                {
                    overrideSprite = "Cicada5body";
                }
                else if (num3 == 3)
                {
                    overrideSprite = "Cicada1body";
                }
                Color shellColor = (self.graphicsModule as BarnacleGraphics).GetShellColor(UnityEngine.Random.value);
                CentipedeShell centipedeShell = new CentipedeShell(self.mainBodyChunk.pos + Custom.RNV() * self.mainBodyChunk.rad, vector, shellColor, self.creatureParams.sizeMultiplier * 1.5f, self.creatureParams.sizeMultiplier * 1.5f, overrideSprite);
                centipedeShell.impactSound = WatcherEnums.WatcherSoundID.Barnacle_Shell_Fragment_Hit_Terrain;
                self.room.AddObject(centipedeShell);
                num2++;
            }
            self.temporaryDamageImmunity = 20;
            self.hasShell = false;
            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                self.bodyChunks[i].mass /= 8f;
                self.bodyChunks[i].rad *= 0.75f;
            }
            self.bodyChunkConnections[0].distance *= 0.75f;
            self.AI.OnLoseShell();
            self.room.PlaySound(SoundID.Coral_Circuit_Jump_Explosion, self.mainBodyChunk);
        }
    }
}
