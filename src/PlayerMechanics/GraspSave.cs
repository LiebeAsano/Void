using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class GraspSave
{
	const int secondsToStunViy = 10;
    const int secondsToStunOnK10 = 30;
	const int secondsToStunBelowK10 = 45;


	public static void Hook()
	{
		On.Player.Grabbed += Player_Grabbed;
		On.Creature.Update += Creature_Update;
	}

	private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
	{
		orig(self, eu);
		if (grabbers.TryGetValue(self.abstractCreature, out var grabbedVoidsTimers))
		{
			Array.ForEach(self.grasps, grasp =>
			{
				if (grasp != null
				&& grasp.grabbed is Player playerInGrasp
				&& (playerInGrasp.IsVoid() || playerInGrasp.IsViy())
                && grabbedVoidsTimers.TryGetValue(playerInGrasp.abstractCreature, out var timerOfBeingGrasped))
				{
					timerOfBeingGrasped.Value++;
					if (timerOfBeingGrasped.Value > TicksUntilStun(playerInGrasp))
					{
						self.Stun(TicksPerSecond * 5);
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
		return TicksPerSecond * (p.KarmaCap == 10 ? secondsToStunOnK10 : secondsToStunBelowK10);
	}

	static ConditionalWeakTable<AbstractCreature, ConditionalWeakTable<AbstractCreature, StrongBox<int>>> grabbers = new();
	private static void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
	{
		orig(self, grasp);
		if (self.IsVoid())
		{
			ConditionalWeakTable<AbstractCreature, StrongBox<int>> grabbedVoidsTimers;
			if (!grabbers.TryGetValue(grasp.grabber.abstractCreature, out grabbedVoidsTimers))
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
