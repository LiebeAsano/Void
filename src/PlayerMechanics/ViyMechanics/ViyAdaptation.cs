using RWCustom;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics.ViyMechanics;

public static class ViyAdaptation
{
    public static void Hook()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
        On.DartMaggot.Update += DartNaggot_Update;
        On.Creature.Violence += Creature_Violence;
        On.RainWorldGame.Win += RainWorldGame_Win;
    }

    public static bool ViyLungExtended;
    public static bool ViyPoisonImmune;
    public static bool ViyDartMaggotImmune; 
    public static int ViyExplosiveImmune;

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (self.IsViy())
        {
            ViyLungExtended = ExternalSaveData.ViyLungExtended;
            ViyPoisonImmune = ExternalSaveData.ViyPoisonImmune;
            ViyDartMaggotImmune = ExternalSaveData.ViyDartMaggotImmune;
            ViyExplosiveImmune = ExternalSaveData.ViyExplosiveImmune;
        }
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.IsViy())
        {
            if (ViyLungExtended)            
                self.slugcatStats.lungsFac = 0.0f;          
            else if (self.room.game.IsViyStoryCampaign() && self.mainBodyChunk.submersion >= 1f)
            {
                if (Random.Range(0, 20000) == 0)
                {
                    _ = new Objects.KarmaRotator(self.abstractCreature.Room.realizedRoom);
                    ViyLungExtended = true;
                }
                self.slugcatStats.lungsFac = 0.2f;
            }
        }
    }

    private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);
        if (self.player.IsViy() && ViyLungExtended)
        {
            self.breath = 0f;
            self.lastBreath = 0f;
        }
    }

    private static void DartNaggot_Update(On.DartMaggot.orig_Update orig, DartMaggot self, bool eu)
    {
        if (self.mode == DartMaggot.Mode.StuckInChunk &&
            self.stuckInChunk != null &&
            self.stuckInChunk.owner is Player player &&
            player.IsViy())
        {
            if (!ViyPoisonImmune && player.room.game.IsViyStoryCampaign())
            {
                if (Random.Range(0, 10000) == 0)
                {
                    _ = new Objects.KarmaRotator(player.abstractCreature.Room.realizedRoom);
                    ViyPoisonImmune = true;
                }
            }

            if (!ViyDartMaggotImmune && ViyPoisonImmune && player.room.game.IsViyStoryCampaign())
            {
                if (Random.Range(0, 15000) == 0)
                {
                    _ = new Objects.KarmaRotator(player.abstractCreature.Room.realizedRoom);
                    ViyDartMaggotImmune = true;
                }
            }

            if (ViyDartMaggotImmune)
            {
                BodyChunk stuckChunk = self.stuckInChunk;

                Vector2 incomingDir = Custom.RotateAroundOrigo(self.stuckDir, Custom.VecToDeg(stuckChunk.Rotation));
                if (incomingDir == Vector2.zero)
                    incomingDir = self.needleDir;
                if (incomingDir == Vector2.zero)
                    incomingDir = Custom.RNV();

                Vector2 bounceDir = -incomingDir.normalized;

                for (int i = self.abstractPhysicalObject.stuckObjects.Count - 1; i >= 0; i--)
                {
                    if (self.abstractPhysicalObject.stuckObjects[i] is DartMaggot.DartMaggotStick stick &&
                        stick.A == self.abstractPhysicalObject)
                    {
                        self.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    }
                }

                self.stuckInChunk = null;

                float pushOut = stuckChunk.rad + self.firstChunk.rad + 2f;
                self.firstChunk.pos = stuckChunk.pos + bounceDir * pushOut;
                self.firstChunk.lastPos = self.firstChunk.pos;
                self.firstChunk.lastLastPos = self.firstChunk.pos;

                self.firstChunk.vel = stuckChunk.vel + bounceDir * 18f;

                self.needleDir = bounceDir;
                self.lastNeedleDir = bounceDir;
                self.ResetBody(bounceDir);

                self.ChangeMode(DartMaggot.Mode.Free);

                self.room.PlaySound(SoundID.Dart_Maggot_Bounce_Off_Wall, self.firstChunk);
            }
        }

        orig(self, eu);
    }

    private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player player && player.IsViy() && type == Creature.DamageType.Explosion)
        {
            if (ViyExplosiveImmune < 3 && Random.value <= 0.1f && player.room.game.IsViyStoryCampaign())
            {
                _ = new Objects.KarmaRotator(player.abstractCreature.Room.realizedRoom);
                ViyExplosiveImmune++;
            }
            stunBonus *= 1f - 0.25f * (ViyExplosiveImmune + 1);
            damage *= 1f - 0.25f * (ViyExplosiveImmune + 1);
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }

    private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
    {
        if (self.IsViyStoryCampaign())
        {
            ExternalSaveData.ViyLungExtended = ViyLungExtended;
            ExternalSaveData.ViyPoisonImmune = ViyPoisonImmune;
            ExternalSaveData.ViyDartMaggotImmune = ViyDartMaggotImmune;
            ExternalSaveData.ViyExplosiveImmune = ViyExplosiveImmune;
        }
        orig(self, malnourished, fromWarpPoint);
    }
}
