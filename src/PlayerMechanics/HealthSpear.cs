using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;
using static VoidTemplate.SaveManager;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics
{
    internal static class HealthSpear
    {
        public static void Hook()
        {
            //IL.Spear.HitSomething += Spear_HitSomething;
            On.Spear.HitSomething += Spear_HitSomething;
            On.Player.Update += Player_Update;
        }

        /*public static void Spear_HitSomething(ILContext il)
		{
			ILCursor c = new(il);
			//Creature.Violence(1 <OR .9 IF VOID>);
			if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(typeof(Creature.DamageType).GetField(nameof(Creature.DamageType.Stab))),
				x => x.MatchLdloc(2)))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_1);
				c.EmitDelegate<Func<float, Spear, SharedPhysics.CollisionResult, float>>((float orig, Spear self, SharedPhysics.CollisionResult result) =>
				{
					if ((self.thrownBy is Scavenger || self.thrownBy is Player) && result.obj is Player p && p.IsVoid() && (p.KarmaCap == 10 || ExternalSaveData.VoidKarma11)) return 0.9f;
                    return orig;
				});
			}
			else
				logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(HealthSpear)}.{nameof(Spear_HitSomething)}: match for dealing damage to player failed");

			//if (player.playerState.permanentDamageTracking >= 1.0 < OR 1.25 IF VOID>)
			if (c.TryGotoNext(MoveType.After,
				x => x.MatchLdfld<global::PlayerState>(nameof(global::PlayerState.permanentDamageTracking)),
				x => x.MatchLdcR8(1d)))
			{
				c.Emit(OpCodes.Ldloc_3);
				c.EmitDelegate<Func<double, Player, double>>((double orig, Player p) =>
				{
					if (p.IsVoid() && (p.KarmaCap == 10 || ExternalSaveData.VoidKarma11))
						return 1.25;
					else
						return orig;
				});
			}
			else
				logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(HealthSpear)}.{nameof(Spear_HitSomething)}: match for permanent damage tracking failed");
		}*/

        const float SecondsForDelayedDeath = 1f;
        static int TicksForDelayedDeath => (int)(SecondsForDelayedDeath * TicksPerSecond);
        static readonly ConditionalWeakTable<Player, StrongBox<int>> deathMarks = new();

        private static bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj is Player player && (player.IsViy() || player.IsVoid()))
            {
                if (result.obj == null)
                {
                    return false;
                }
                if ((player.IsVoid() || player.IsViy())
                    && self.Spear_NeedleCanFeed()
                    && self.thrownBy is Player thrower)
                {
                    if (!deathMarks.TryGetValue(thrower, out _))
                    {
                        deathMarks.Add(thrower, new(self.room.game.clock));
                    }
                }
                bool flag = false;
                if (self.abstractPhysicalObject.world.game.IsArenaSession && self.abstractPhysicalObject.world.game.GetArenaGameSession.GameTypeSetup.spearHitScore != 0 && self.thrownBy != null && self.thrownBy is Player && result.obj is Creature)
                {
                    flag = true;
                    if ((result.obj as Creature).State is HealthState && ((result.obj as Creature).State as HealthState).health <= 0f)
                    {
                        flag = false;
                    }
                    else if ((result.obj as Creature).State is not HealthState && (result.obj as Creature).State.dead)
                    {
                        flag = false;
                    }
                }
                if (result.obj is Creature)
                {
                    if (!ModManager.MSC || result.obj is not Player || (result.obj as Creature).SpearStick(self, Mathf.Lerp(0.55f, 0.62f, UnityEngine.Random.value), result.chunk, result.onAppendagePos, self.firstChunk.vel))
                    {
                        float num = self.spearDamageBonus;
                        if (self.bugSpear)
                        {
                            num *= 3f;
                        }
                        if (player.IsViy())
                        {
                            (result.obj as Creature).Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, num, 0f);
                        }
                        if (player.IsVoid())
                        {
                            (result.obj as Creature).Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, num, 20f);
                        }
                        if (ModManager.MSC && result.obj is Player)
                        {
                            if (player.IsVoid())
                            {
                                player.playerState.permanentDamageTracking += num / player.Template.baseDamageResistance;
                            }
                            if (player.playerState.permanentDamageTracking >= 1.0 && player.IsVoid() && player.KarmaCap != 10 && !ExternalSaveData.VoidKarma11)
                            {
                                player.Die();
                            }
                            else if (player.playerState.permanentDamageTracking >= 1.25 && player.IsVoid() && (player.KarmaCap == 10 || ExternalSaveData.VoidKarma11))
                            {
                                player.Die();
                            }
                            else if (player.playerState.permanentDamageTracking >= 2.5 && player.IsViy())
                            {
                                player.Die();
                            }
                        }
                    }
                }
                else if (result.chunk != null)
                {
                    result.chunk.vel += self.firstChunk.vel * self.firstChunk.mass / result.chunk.mass;
                }
                else if (result.onAppendagePos != null)
                {
                    (result.obj as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, self.firstChunk.vel * self.firstChunk.mass);
                }
                if (result.obj is Creature && (result.obj as Creature).SpearStick(self, Mathf.Lerp(0.55f, 0.62f, UnityEngine.Random.value), result.chunk, result.onAppendagePos, self.firstChunk.vel))
                {
                    Creature creature = result.obj as Creature;
                    self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.firstChunk);
                    self.LodgeInCreature(result, eu);
                    if (flag)
                    {
                        self.abstractPhysicalObject.world.game.GetArenaGameSession.PlayerLandSpear(self.thrownBy as Player, self.stuckInObject as Creature);
                    }
                    return true;
                }
                self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk);
                self.vibrate = 20;
                self.ChangeMode(Weapon.Mode.Free);
                self.firstChunk.vel = (self.firstChunk.vel * -0.5f) + (Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * self.firstChunk.vel.magnitude);
                self.SetRandomSpin();
                return false;
            }
            return orig(self, result, eu);
        }
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (deathMarks.TryGetValue(self, out var deathMark))
            {
                if (self.room?.game is RainWorldGame game && (game.clock - deathMark.Value) > TicksForDelayedDeath)
                    self.Die();
            }
            if (self.IsViy())
            {
                self.playerState.permanentDamageTracking -= 0.0025f;
                if (self.playerState.permanentDamageTracking < 0)
                {
                    self.playerState.permanentDamageTracking = 0;
                }
            }
        }
    }
}
