using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

internal static class DLLindigestion
{
	public static void Hook()
	{
		On.DaddyLongLegs.Eat += OnDaddyLongLegsEat;
	}
#warning todo: move from async
	private static async void OnDaddyLongLegsEat(On.DaddyLongLegs.orig_Eat orig, DaddyLongLegs self, bool eu)
	{
		foreach (var eatObject in self.eatObjects)
		{
			if (eatObject.chunk.owner is Player player
				&& player.IsVoid()
				&& player.dead)
			{
				await Task.Delay(3000);
				DestroyBody(player);
				self.Die();
				FinishEating(self);
				return;
			}
		}
		orig(self, eu);
	}
	private static void DestroyBody(Player player)
	{
		if (player != null && player.room != null)
		{
			player.room.RemoveObject(player);
		}
		player.dead = true;
	}

	private static void FinishEating(DaddyLongLegs self)
	{
		self.eatObjects.Clear();
		self.digestingCounter = 0;
		self.moving = false;
		self.tentaclesHoldOn = false;
	}
}
