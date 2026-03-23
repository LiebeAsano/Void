using RWCustom;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class ImmuneToFallDamage
{
    private sealed class LandingData
    {
        public int PerfectLandingBuffer;
    }

    private static readonly ConditionalWeakTable<Player, LandingData> landingData = new();

    private const int PerfectLandingWindow = 15;

    private const float SafeFallSpeed = 20f;
    private const float MediumFallSpeed = 40f;
    private const float HeavyFallSpeed = 60f;

    public static void Hook()
    {
        On.Player.Update += Player_Update;
        On.Player.TerrainImpact += Player_TerrainImpact;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (!self.AreVoidViy() || self.room == null)
            return;

        LandingData data = landingData.GetOrCreateValue(self);

        if (data.PerfectLandingBuffer > 0)
            data.PerfectLandingBuffer--;

        if (!CanPreparePerfectLanding(self))
            return;

        if (self.input[0].y < 0 && self.input[1].y >= 0 && data.PerfectLandingBuffer == 0)
        {
            data.PerfectLandingBuffer = PerfectLandingWindow;
        }
    }

    private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        if (!self.AreVoidViy() || self.room == null || HasTempleGuard(self.room))
        {
            orig(self, chunk, direction, speed, firstContact);
            return;
        }

        if (!firstContact || direction.y >= 0)
        {
            orig(self, chunk, direction, speed, firstContact);
            return;
        }

        LandingData data = landingData.GetOrCreateValue(self);
        bool perfectLanding = data.PerfectLandingBuffer > 0;

        int oldImmune = self.immuneToFallDamage;
        self.immuneToFallDamage = Math.Max(oldImmune, 1);

        orig(self, chunk, direction, speed, firstContact);

        self.immuneToFallDamage = oldImmune;

        ApplyLandingOutcome(self, speed, perfectLanding);

        if (perfectLanding)
        {
            PlayPerfectLandingFeedback(self, speed);
        }
        else
        {
            PlayNormalLandingFeedback(self, speed);
        }

        data.PerfectLandingBuffer = 0;
    }

    private static bool CanPreparePerfectLanding(Player self)
    {
        if (self.dead || !self.Consious || self.stun > 0)
            return false;

        if (self.room == null || self.bodyChunks == null || self.bodyChunks.Length == 0)
            return false;

        if (self.bodyMode == Player.BodyModeIndex.Crawl)
            return false;

        if (self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
            return false;

        if (self.bodyMode == BodyModeIndexExtension.CeilCrawl)
            return false;

        if (self.animation == Player.AnimationIndex.HangFromBeam)
            return false;

        if (self.Submersion > 0.5f)
            return false;

        if (self.grabbedBy.Count > 0)
            return false;

        return self.firstChunk.vel.y < -6f;
    }

    private static bool HitGroundSoon(Player self)
    {
        if (self.room == null)
            return false;

        Vector2 pos = self.firstChunk.pos;

        for (int i = 1; i <= 6; i++)
        {
            IntVector2 tile = self.room.GetTilePosition(pos + new Vector2(0f, -20f * i));
            if (self.room.GetTile(tile).Solid)
                return true;
        }

        return false;
    }

    private static void ApplyLandingOutcome(Player self, float speed, bool perfectLanding)
    {
        if (speed < SafeFallSpeed)
        {
            return;
        }

        if (speed < MediumFallSpeed)
        {
            if (!perfectLanding)
            {
                self.Stun(25);
            }
            return;
        }

        if (speed < HeavyFallSpeed)
        {
            if (!perfectLanding)
            {
                self.playerState.permanentDamageTracking += 0.75f;
                self.Stun(75);
            }
            return;
        }

        if (perfectLanding)
        {
            self.playerState.permanentDamageTracking += 0.5f;
            self.Stun(50);
        }
        else
            self.Die();
        
        
    }

    private static void PlayPerfectLandingFeedback(Player self, float speed)
    {
        if (self.room == null || speed < MediumFallSpeed)
            return;

        float volume = Mathf.InverseLerp(SafeFallSpeed, HeavyFallSpeed + 8f, speed);

        self.room.PlaySound(SoundID.Slugcat_Roll_Init, self.mainBodyChunk, false, Mathf.Lerp(0.5f, 1.1f, volume), Mathf.Lerp(1.2f, 0.9f, volume));

        self.room.AddObject(new ExplosionSpikes(self.room, self.mainBodyChunk.pos, 4, 3f, 5f, 4f, 4, Color.white));
    }

    private static void PlayNormalLandingFeedback(Player self, float speed)
    {
        if (self.room == null || speed < MediumFallSpeed)
            return;

        float volume = Mathf.InverseLerp(MediumFallSpeed, HeavyFallSpeed + 8f, speed);

        self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk, false, Mathf.Lerp(0.7f, 1.2f, volume), Mathf.Lerp(1.05f, 0.85f, volume));
    }

    private static bool HasTempleGuard(Room room)
    {
        if (room?.abstractRoom?.creatures == null)
            return false;

        var creatures = room.abstractRoom.creatures;
        for (int i = 0; i < creatures.Count; i++)
        {
            AbstractCreature creature = creatures[i];
            if (creature?.creatureTemplate?.type == CreatureTemplate.Type.TempleGuard)
                return true;
        }

        return false;
    }
}
