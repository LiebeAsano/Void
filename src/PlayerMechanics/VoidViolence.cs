using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics
{
    public static class VoidViolence
    {
        public static void Hook()
        {
            On.Creature.Violence += Creature_Violence;
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player player && player.IsVoid())
            {
                if (type == Creature.DamageType.Explosion)
                {
                    int Karma = player.KarmaCap;
                    if (Karma == 10)
                        if (Karma11Update.VoidKarma11)
                            Karma = 9;
                        else
                            Karma = 0;
                    if (player.room?.game?.IsVoidStoryCampaign() != true && Karma11Update.VoidKarma11)
                        Karma = 9;
                    
                    stunBonus *= 1f - 0.066f * (Karma + 1); 
                    damage *= 1f - 0.066f * (Karma + 1);
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
                float Damage = damage / self.Template.baseDamageResistance;
                float Stun = (damage * 30f + stunBonus) / self.Template.baseStunResistance;
                if (self.State is HealthState)
                {
                    Stun *= 1.5f + Mathf.InverseLerp(0.5f, 0f, (self.State as HealthState).health) * UnityEngine.Random.value;
                }
                if (type.Index != -1)
                {
                    if (self.Template.damageRestistances[type.Index, 0] > 0f)
                    {
                        Damage /= self.Template.damageRestistances[type.Index, 0];
                    }
                    if (self.Template.damageRestistances[type.Index, 1] > 0f)
                    {
                        Damage /= self.Template.damageRestistances[type.Index, 1];
                    }
                }
                if (ModManager.MSC)
                {
                    if (self.room != null && self.room.world.game.IsArenaSession && self.room.world.game.GetArenaGameSession.chMeta != null && self.room.world.game.GetArenaGameSession.chMeta.resistMultiplier > 0f && !(self is Player))
                    {
                        Damage /= self.room.world.game.GetArenaGameSession.chMeta.resistMultiplier;
                    }
                    if (self.room != null && self.room.world.game.IsArenaSession && self.room.world.game.GetArenaGameSession.chMeta != null && self.room.world.game.GetArenaGameSession.chMeta.invincibleCreatures && !(self is Player))
                    {
                        Damage = 0f;
                    }
                }
                if (source != null && source.owner is BigNeedleWorm
                    || type == Creature.DamageType.Explosion)
                {
                    player.playerState.permanentDamageTracking += Damage;
                }
                self.stunDamageType = type;
                self.Stun((int)Stun);
                self.stunDamageType = Creature.DamageType.None;
                if (self.State is HealthState)
                {
                    (self.State as HealthState).health -= Damage;
                    if (self.Template.quickDeath && (UnityEngine.Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || ((self.State as HealthState).health < 0f && UnityEngine.Random.value < 0.33f)))
                    {
                        self.Die();
                    }
                }
                if (Karma11Update.VoidKarma11)
                {
                    if (Damage >= self.Template.instantDeathDamageLimit * 1.25f || player.playerState.permanentDamageTracking >= 1.25f)
                    {
                        self.Die();
                    }
                }
                else if (Damage >= self.Template.instantDeathDamageLimit)
                {
                    self.Die();
                }

            }
            else
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            }
        }
    }
}
