using Mono.Cecil;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.SaveManager;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

public static class GraspSave
{
	const int secondsToStunViy = 10;
    const int secondsToStunOnK10 = 20;
	const int secondsToStunBelowK10 = 30;


	public static void Hook()
	{
		On.Player.Grabbed += Player_Grabbed;
		On.Creature.Update += Creature_Update;
	}

	private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
	{
		orig(self, eu);
		if (grabbers.TryGetValue(self.abstractCreature, out var grabbedVoidsTimers) && !(self is Player player && player.AreVoidViy()))
		{
			Array.ForEach(self.grasps, grasp =>
			{
				if (grasp != null
				&& grasp.grabbed is Player playerInGrasp
				&& playerInGrasp.AreVoidViy()
                && grabbedVoidsTimers.TryGetValue(playerInGrasp.abstractCreature, out var timerOfBeingGrasped))
				{
					timerOfBeingGrasped.Value++;
					if (timerOfBeingGrasped.Value % 40 == 0)
					{
						self.SetKillTag(playerInGrasp.abstractCreature);
						if (self is not null && self is not Player)
						{
							if (self.State is HealthState)
							{
								(self.State as HealthState).health -= 0.01f;
								if (self.Template.quickDeath && (UnityEngine.Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || ((self.State as HealthState).health < 0f && UnityEngine.Random.value < 0.33f)))
								{
									self.Die();
								}
							}
						}
						else if (self is Player player && !player.AreVoidViy())
						{
							if (player.playerState is not null)
							{
								player.playerState.permanentDamageTracking += 0.01f;
								if (player.playerState.permanentDamageTracking >= 1.0f)
								{
									self.Die();
								}
							}
                        }
                    }
					if (timerOfBeingGrasped.Value > TicksUntilStun(playerInGrasp))
					{
						self.Stun(TicksPerSecond * 5);
						if (playerInGrasp.IsViy() && !playerInGrasp.dead)
						{
                            self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                            self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                            self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Bite, 2.5f, 250f);
                        }
						timerOfBeingGrasped.Value = 0;
					}
				}
			});
		} 
	}

    static int TicksUntilStun(Player p)
	{
		if (p.slugcatStats.name == VoidEnums.SlugcatID.Viy)
		{
			return TicksPerSecond * secondsToStunViy;

        }
		return TicksPerSecond * (p.KarmaCap == 10 || Karma11Update.VoidKarma11 ? secondsToStunOnK10 : secondsToStunBelowK10);
	}

	static ConditionalWeakTable<AbstractCreature, ConditionalWeakTable<AbstractCreature, StrongBox<int>>> grabbers = new();
	private static void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
	{
		orig(self, grasp);
		if (self.AreVoidViy())
		{
            if (!grabbers.TryGetValue(grasp.grabber.abstractCreature, out ConditionalWeakTable<AbstractCreature, StrongBox<int>> grabbedVoidsTimers))
            {
                grabbedVoidsTimers = new();
                grabbers.Add(grasp.grabber.abstractCreature, grabbedVoidsTimers);
            }

            if (!grabbedVoidsTimers.TryGetValue(self.abstractCreature, out _))
			{
				grabbedVoidsTimers.Add(self.abstractCreature, new(0));
			}

		}
	}
}
