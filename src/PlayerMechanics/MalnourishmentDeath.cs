using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class MalnourishmentDeath
{
	public static void Hook()
	{
		On.Player.Update += Malnourishment_Death;
	}

	private static void Malnourishment_Death(On.Player.orig_Update orig, Player self, bool eu)
	{
		orig(self, eu);
		if (self.room == null) return;
		RainWorldGame game = self.room.game;
		game.Players.ForEach(absPlayer =>
		{
			if (absPlayer.realizedCreature is Player player
			&& player.IsVoid()
			&& player.room != null
			&& player.room == self.room
			&& player.KarmaCap != 10
			&& player.Malnourished) player.Die();
		});

	}
}
