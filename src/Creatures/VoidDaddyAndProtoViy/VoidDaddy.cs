using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics.Karma11Features;
using Watcher;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.Creatures.VoidDaddyAndProtoViy

{
    public static class VoidDaddy
    {
        private static readonly ConditionalWeakTable<DaddyLongLegs.DaddyState, DaddyExt> daddyCWT = new();

        public static DaddyExt GetDaddyExt(this DaddyLongLegs.DaddyState daddyState) => daddyCWT.GetOrCreateValue(daddyState);

        public static DaddyExt GetDaddyExt(this DaddyLongLegs daddy) => daddyCWT.GetOrCreateValue(daddy.State as DaddyLongLegs.DaddyState);

        public static void Hook()
        {
            On.DaddyLongLegs.ctor += DaddyLongLegs_ctor;
            On.DaddyLongLegs.Update += DaddyLongLegs_Update;
            On.DaddyLongLegs.Violence += DaddyLongLegs_Violence;
            On.DaddyTentacle.ctor += DaddyTentacle_ctor;
            On.DaddyLongLegs.Collide += DaddyLongLegs_Collide;
            On.DaddyTentacle.Update += DaddyTentacle_Update;
            On.ArtificialIntelligence.TrackerToDiscardDeadCreature += ArtificialIntelligence_TrackerToDiscardDeadCreature;
        }

        private static bool ArtificialIntelligence_TrackerToDiscardDeadCreature(On.ArtificialIntelligence.orig_TrackerToDiscardDeadCreature orig, ArtificialIntelligence self, AbstractCreature crit)
        {
            if (self is DaddyAI daddyAI && daddyAI.daddy.GetDaddyExt().IsProtoViy)
                return true;
            return orig(self, crit);
        }

        private static void DaddyTentacle_Update(On.DaddyTentacle.orig_Update orig, DaddyTentacle self)
        {
            orig(self);
            if (self.owner is DaddyLongLegs daddy && daddy.GetDaddyExt().IsProtoViy && self.grabChunk != null && self.grabChunk.owner is Creature crit && crit.dead)
            {
                self.grabChunk = null;
            }
        }

        private static void DaddyLongLegs_Collide(On.DaddyLongLegs.orig_Collide orig, DaddyLongLegs self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (self.GetDaddyExt().IsProtoViy)
            {
                if (myChunk == 0 && otherObject is Creature creature && !creature.dead && self.GetDaddyExt().biteCooldown <= 0)
                {
                    self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                    self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                    BodyChunk otherBodyChunk = creature.bodyChunks[otherChunk];
                    for (int num12 = UnityEngine.Random.Range(8, 14); num12 >= 0; num12--)
                    {
                        self.room.AddObject(new WaterDrip(Vector2.Lerp(otherBodyChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + otherBodyChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(otherObject.firstChunk.pos, (self.mainBodyChunk.pos + (self.graphicsModule as DaddyGraphics).dummy.head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
                    }
                    creature.SetKillTag(self.abstractCreature);
                    creature.Violence(self.bodyChunks[0], new Vector2(0f, 0f), otherBodyChunk, null, Creature.DamageType.Bite, 2.5f, 15f);
                    creature.stun = 5;
                    self.GetDaddyExt().biteCooldown = 40;
                }
                return;
            }
            orig(self, otherObject, myChunk, otherChunk);
        }

        private static void DaddyTentacle_ctor(On.DaddyTentacle.orig_ctor orig, DaddyTentacle self, Creature daddy, DaddyLongLegs.IHaveRotParts rotOwner, BodyChunk chunk, float length, int tentacleNumber, Vector2 tentacleDir)
        {
            if (daddy is DaddyLongLegs dll)
            {
                if (dll.GetDaddyExt().IsVoidDaddy)
                {
                    length *= 2;
                }
                else if (dll.GetDaddyExt().IsProtoViy)
                {
                    length = 160;
                }
            }
            orig(self, daddy, rotOwner, chunk, length, tentacleNumber, tentacleDir);
        }

        private static void DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            orig(self, eu);

            if (self.GetDaddyExt().biteCooldown > 0)
            {
                self.GetDaddyExt().biteCooldown--;
            }
            if (self.GetDaddyExt().HaveType && self.room != null && UnityEngine.Random.Range(0, 3400) == 0)
            {
                self.room.PlaySound(UnityEngine.Random.Range(0, 3) switch
                {
                    0 => WatcherEnums.WatcherSoundID.RotLiz_Vocalize,
                    1 => WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_A,
                    _ => WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_B
                }, self.firstChunk.pos, self.abstractPhysicalObject);
            }
        }

        private static void DaddyLongLegs_ctor(On.DaddyLongLegs.orig_ctor orig, DaddyLongLegs self, AbstractCreature abstractCreature, World world)
        {
            if (abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy && VoidDreamScript.IsVoidDream)
            {
                (abstractCreature.state as DaddyLongLegs.DaddyState).GetDaddyExt().daddyType = DaddyExt.VoidDaddyType.VoidDaddy;
            }
            orig(self, abstractCreature, world);
            if (self.GetDaddyExt().IsProtoViy)
            {
                var chunks = self.bodyChunks;
                Array.Resize(ref chunks, 2);
                self.bodyChunks = chunks;
                foreach (var tentacle in self.tentacles)
                {
                    tentacle.connectedChunk = self.bodyChunks[0];
                }
                Array.Resize(ref self.bodyChunkConnections, 1);
            }
            if (self.GetDaddyExt().HaveType)
            {
                self.effectColor = self.eyeColor = self.GetDaddyExt().daddyColor;
            }
        }

        private static void DaddyLongLegs_Violence(On.DaddyLongLegs.orig_Violence orig, DaddyLongLegs self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self.GetDaddyExt().HaveType)
            {
                damage *= 2f;
                if (hitAppendage != null)
                {
                    damage /= self.SizeClass ? 2.2f : 1.7f;
                    (self.State as DaddyLongLegs.DaddyState).tentacleHealth[hitAppendage.appendage.appIndex] -= damage;
                    damage = 0f;
                }
                damage /= ModManager.DLCShared && self.abstractCreature.superSizeMe ? 4f : 1f;
                if (!self.RippleViolenceCheck(source))
                {
                    return;
                }
                if (source != null && source.owner is Creature)
                {
                    self.SetKillTag((source.owner as Creature).abstractCreature);
                }
                if (directionAndMomentum != null)
                {
                    if (hitChunk != null)
                    {
                        hitChunk.vel += Vector2.ClampMagnitude(directionAndMomentum.Value / hitChunk.mass, 10f);
                    }
                    else if (hitAppendage != null && self is PhysicalObject.IHaveAppendages)
                    {
                        (self as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(hitAppendage, directionAndMomentum.Value);
                    }
                }
                float num = damage / self.Template.baseDamageResistance;
                if (type.Index != -1)
                {
                    if (self.Template.damageRestistances[type.Index, 0] > 0f)
                    {
                        num /= self.Template.damageRestistances[type.Index, 0];
                    }
                }
                self.stunDamageType = type;
                self.stunDamageType = Creature.DamageType.None;
                if (damage >= 4f && type == Creature.DamageType.Explosion)
                {
                    self.room.PlaySound(UnityEngine.Random.Range(0, 3) switch
                    {
                        0 => WatcherEnums.WatcherSoundID.RotLiz_Vocalize,
                        1 => WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_A,
                        _ => WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_B
                    }, self.firstChunk.pos, self.abstractPhysicalObject);
                }
                if (self.State is HealthState)
                {
                    (self.State as HealthState).health -= num;
                    if (self.Template.quickDeath && (UnityEngine.Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || (self.State as HealthState).health < 0f && UnityEngine.Random.value < 0.33f))
                    {
                        if (self.GetDaddyExt().IsVoidDaddy)
                        {
                            Karma11Update.VoidPermaNightmare = false;
                            ExternalSaveData.VoidPermaNightmare = 1;
                        }
                        self.Die();
                    }
                }
                if (num >= self.Template.instantDeathDamageLimit)
                {
                    if (self.GetDaddyExt().IsVoidDaddy)
                    {
                        Karma11Update.VoidPermaNightmare = false;
                        ExternalSaveData.VoidPermaNightmare = 1;
                    }
                    self.Die();
                }
            }
            else
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

    }

    public class DaddyExt
    {
        public VoidDaddyType daddyType = VoidDaddyType.None;

        public int biteCooldown;

        public Color? myColor;

        public bool HaveType
        {
            get => daddyType != VoidDaddyType.None;
        }

        public bool IsProtoViy
        {
            get => daddyType == VoidDaddyType.ProtoViy;
        }

        public bool IsVoidDaddy
        {
            get => daddyType == VoidDaddyType.VoidDaddy;
        }

        public Color daddyColor
        {
            get
            {
                if (myColor != null) return myColor.Value;
                if (IsProtoViy) return DrawSprites.voidFluidColor;
                return Color.red;
            }
        }

        public enum VoidDaddyType
        {
            None,
            VoidDaddy,
            ProtoViy
        }
    }
}
