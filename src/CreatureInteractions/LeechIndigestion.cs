using System;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

internal static class LeechIndigestion
{
	public static void Hook()
	{
		On.Leech.Attached += OnLeechAttached;
	}

//#warning todo: move from async
	private static async void OnLeechAttached(On.Leech.orig_Attached orig, Leech self)
	{
		orig(self);

		if (Array.Exists(self.grasps, grasp => grasp is not null 
		&& grasp.grabbed is Player player
		&& (player.IsVoid() || player.IsViy()) 
		&& self != null 
		&& self.room != null))
		{
			await Task.Delay(6000);
			self.Die();
		}
	}
}
