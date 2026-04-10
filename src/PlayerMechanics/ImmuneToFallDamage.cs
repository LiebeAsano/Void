using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Useful;
using VoidTemplate.Objects;

namespace VoidTemplate.PlayerMechanics;

public static class ImmuneToFallDamage
{
    private sealed class LandingData
    {
        public int PerfectLandingBuffer;
    }

    private static readonly ConditionalWeakTable<Player, LandingData> landingData = new();

    private const int PerfectLandingWindow = 40;

    private const float SafeFallSpeed = 40f;
    private const float MediumFallSpeed = 60f;
    private const float HeavyFallSpeed = 80f;

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

        if (!CanPreparePerfectLanding(self))
            return;

        LandingData data = landingData.GetOrCreateValue(self);

        if (data.PerfectLandingBuffer > 0)
            data.PerfectLandingBuffer--;

        if (self.input[0].y < 0)
        {
            data.PerfectLandingBuffer = PerfectLandingWindow;
        }
    }

    private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        if (!self.IsVoid() || self.room == null || HasTempleGuard(self.room))
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

        self.immuneToFallDamage = 1;

        orig(self, chunk, direction, speed, firstContact);

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
        if (self == null || self.dead || !self.Consious || self.room == null)
            return false;

        if (self.stun > 0 || self.grabbedBy.Count > 0)
            return false;

        if (self.Submersion > 0.5f)
            return false;

        if (self.bodyMode == Player.BodyModeIndex.ZeroG)
            return false;

        return true;
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
                if (self.abstractCreature.world.game.IsVoidStoryCampaign())
                {
                    if (!self.abstractCreature.world.game.GetStorySession.saveState.GetFallMessageShown())
                    {
                        self.room.AddObject(new Tutorial(self.room,
                        [
                            new("Press the 'Down' button before landing to reduce the fall damage.", 33, 333)
                        ]));
                        self.abstractCreature.world.game.GetStorySession.saveState.SetFallMessageShown(true);
                    }
                }
            }
            return;
        }

        if (speed < HeavyFallSpeed)
        {
            if (!perfectLanding)
            {
                self.playerState.permanentDamageTracking += 0.75f;
                self.Stun(75);
                if (self.abstractCreature.world.game.IsVoidStoryCampaign())
                {
                    if (!self.abstractCreature.world.game.GetStorySession.saveState.GetKarmaFlowerMessageShown())
                    {
                        self.room.AddObject(new Tutorial(self.room,
                        [
                            new("Press the 'Down' button before landing to reduce the fall damage.", 33, 333)
                        ]));
                        self.abstractCreature.world.game.GetStorySession.saveState.SetKarmaFlowerMessageShown(true);
                    }
                }
            }
            else
            {
                self.Stun(25);
                self.playerState.permanentDamageTracking += 0.25f;
            }
            return;
        }

        if (perfectLanding)
        {
            self.playerState.permanentDamageTracking += 0.75f;
            self.Stun(75);
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
