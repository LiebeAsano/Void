using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

public static class MalnourishmentDeath
{
	public static void Hook()
	{
		On.Player.Update += Malnourishment_Death;
	}

	public static int Malnourished = 0;

    private static void Malnourishment_Death(On.Player.orig_Update orig, Player self, bool eu)
	{
		orig(self, eu);
		if (self.IsVoid()
		   && self.room is not null
		   && self.room.game.IsVoidWorld()
		   && self.Malnourished)
		{
			Malnourished++;
		}

		if (Malnourished >= 440)
		{
			self.slugcatStats.malnourished = false;
			Malnourished = 0;
		}

        if (self.IsVoid()
		   && self.room is not null
		   && !self.room.game.IsVoidWorld()
		   && self.Malnourished) self.Die();
    }
}
