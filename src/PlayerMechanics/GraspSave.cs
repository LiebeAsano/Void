using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class GraspSave
{
	
	public static void Hook()
	{
		On.Player.Grabbed += Player_Grabbed;
		On.Creature.Update += Creature_Update;
	}

	private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
	{
		orig(self, eu);
		if(grabbers.TryGetValue(self.abstractCreature, out var timer))
		{
			if(Array.Exists(self.grasps, grasp => grasp != null && grasp.grabbed is Player p && p.IsVoid()))
			{
				timer.Value++;
				Player p = Array.Find(self.grasps, grasp => grasp != null && grasp.grabbed is Player p && p.IsVoid()).grabbed as Player;
				if (timer.Value > TicksUntilDeath(p))
				{
					self.Die();
				}
			}
			else grabbers.Remove(self.abstractCreature);
		}
	}
	static int TicksUntilDeath(Player p)
	{
		return TicksPerSecond * (p.KarmaCap == 11 ? 90 : 180);
	}

	static ConditionalWeakTable<AbstractCreature, StrongBox<int>> grabbers = new();
	private static void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
	{
		orig(self, grasp);
		if (self.IsVoid()) grabbers.Add(grasp.grabber.abstractCreature, new(0));
	}
}
