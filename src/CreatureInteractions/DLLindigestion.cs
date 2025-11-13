using System;
using System.Threading.Tasks;
using VoidTemplate.Objects;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class DLLindigestion
{
	public static void Hook()
	{
		On.DaddyLongLegs.Eat += OnDaddyLongLegsEat;
        On.MoreSlugcats.StowawayBug.Eat += StowawayBugEat;
    }

//#warning todo: move from async

    private static async void StowawayBugEat(On.MoreSlugcats.StowawayBug.orig_Eat orig, MoreSlugcats.StowawayBug self, bool eu)
    {
        foreach (var eatObject in self.eatObjects)
        {
            if (eatObject.chunk.owner is Player player
                && (player.IsVoid() || player.IsViy())
                && player.dead)
            {
                DestroyBody(player);
                await Task.Delay(3000);
                self.Die();
                SBFinishEating(self);
                return;
            }
        }
        orig(self, eu);
    }

    private static void SBFinishEating(MoreSlugcats.StowawayBug self)
    {
		self.eatObjects.Clear();
    }
    private static async void OnDaddyLongLegsEat(On.DaddyLongLegs.orig_Eat orig, DaddyLongLegs self, bool eu)
	{
		foreach (var eatObject in self.eatObjects)
		{
			if (eatObject.chunk.owner is Player player
				&& player.IsVoid()
				&& player.dead
				&& !self.HDmode)
			{
                DestroyBody(player);
                await Task.Delay(3000);
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
