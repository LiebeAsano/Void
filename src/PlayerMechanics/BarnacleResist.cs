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

public class BarnacleResist
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
        return crit is Barnacle { dead: false } && self != null && self.IsVoid() || orig(self, crit);
    }

    private static void Barnacle_BitByPlayer(On.Watcher.Barnacle.orig_BitByPlayer orig, Barnacle self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && player.IsVoid())
        {
            self.bites--;
        }
        orig(self, grasp, eu);
    }

    private static void Barnacle_Colide(On.Watcher.Barnacle.orig_Collide orig, Watcher.Barnacle self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        if (!self.Consious) return;

        if (otherObject is not Barnacle &&
            otherObject is Creature { Consious: true } creature &&
            creature is Player player &&
            player.IsVoid() &&
            self.hasShell &&
            self.shakeCooldown <= 0 &&
            (self.AI.behavior == BarnacleAI.Behavior.Idle || self.AI.behavior == BarnacleAI.Behavior.Travelling))
        {
            HandleVoidCollision(self, otherObject, myChunk, otherChunk);
            return;
        }

        orig(self, otherObject, myChunk, otherChunk);
    }

    private static void HandleVoidCollision(Watcher.Barnacle self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        self.room.PlaySound(WatcherEnums.WatcherSoundID.Barnacle_Push_Away_Nearby_Creatures, self.mainBodyChunk);
        self.shakeTime = UnityEngine.Random.Range(10, 20);
        self.shakeCooldown = 80;

        Vector2 a = Vector2.Lerp(otherObject.bodyChunks[otherChunk].pos, self.bodyChunks[myChunk].pos, 0.5f);

        foreach (var chunk in otherObject.bodyChunks)
        {
            chunk.vel = Custom.DirVec(self.mainBodyChunk.pos, chunk.pos) * UnityEngine.Random.Range(3f, 8f);
        }

        for (int k = 0; k < 5; k++)
        {
            self.room.AddObject(new Spark(
                a + Custom.RNV() * 10f,
                otherObject.bodyChunks[otherChunk].vel * -0.1f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.2f, 0.4f, UnityEngine.Random.value) * otherObject.bodyChunks[otherChunk].vel.magnitude,
                new Color(1f, 1f, 1f),
                null, 20, 80));
        }
    }

    private static void Barnacle_Violence(On.Watcher.Barnacle.orig_Violence orig, Barnacle self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        if (!self.RippleViolenceCheck(source)) return;

        if (source?.owner is Player player && player.IsVoid())
        {
            VoidLoseShell(self);
        }
        orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
    }

    private static void VoidLoseShell(Barnacle self)
    {
        if (!self.hasShell) return;

        self.hasShell = false;
        self.temporaryDamageImmunity = 20;

        foreach (var chunk in self.bodyChunks)
        {
            chunk.mass /= 8f;
            chunk.rad *= 0.75f;
        }

        self.bodyChunkConnections[0].distance *= 0.75f;
        self.AI.OnLoseShell();

        if (self.room == null) return;

        SpawnShellFragments(self);
        self.room.PlaySound(SoundID.Coral_Circuit_Jump_Explosion, self.mainBodyChunk);
    }

    private static void SpawnShellFragments(Barnacle self)
    {
        int fragmentCount = (int)Mathf.Lerp(1f, 3f, self.creatureParams.sizeMultiplier);
        for (int i = 0; i < fragmentCount; i++)
        {
            var abstractObj = new AbstractPhysicalObject(
                self.room.world,
                AbstractPhysicalObject.AbstractObjectType.Rock,
                null,
                self.room.GetWorldCoordinate(self.mainBodyChunk.pos + Custom.RNV() * self.mainBodyChunk.rad),
                self.room.game.GetNewID());

            self.room.abstractRoom.entities.Add(abstractObj);
            abstractObj.RealizeInRoom();
            abstractObj.realizedObject.firstChunk.vel = Custom.RNV() * UnityEngine.Random.Range(8f, 16f);
        }

        if (self.graphicsModule == null) return;

        int graphicFragmentCount = (int)Mathf.Lerp(4f, 7f, self.creatureParams.sizeMultiplier);
        for (int i = 0; i < graphicFragmentCount; i++)
        {
            var fragment = CreateShellFragment(self);
            self.room.AddObject(fragment);
        }
    }

    private static CentipedeShell CreateShellFragment(Barnacle self)
    {
        Vector2 vector = Custom.RNV() * 4f;
        vector = new Vector2(vector.x, Mathf.Abs(vector.y));

        string overrideSprite = GetRandomShellSprite();
        Color shellColor = (self.graphicsModule as BarnacleGraphics).GetShellColor(UnityEngine.Random.value);

        return new CentipedeShell(
            self.mainBodyChunk.pos + Custom.RNV() * self.mainBodyChunk.rad,
            vector,
            shellColor,
            self.creatureParams.sizeMultiplier * 1.5f,
            self.creatureParams.sizeMultiplier * 1.5f,
            overrideSprite)
        {
            impactSound = WatcherEnums.WatcherSoundID.Barnacle_Shell_Fragment_Hit_Terrain
        };
    }

    private static string GetRandomShellSprite()
    {
        return UnityEngine.Random.Range(0, 4) switch
        {
            1 => "KrakenShield0",
            2 => "Cicada5body",
            3 => "Cicada1body",
            _ => "RootBall1"
        };
    }
}
