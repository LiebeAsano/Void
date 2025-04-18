using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

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
		if(self.IsVoid()
		   && self.room is not null
		   && !self.room.game.IsVoidWorld()
		   && self.KarmaCap != 10
		   && !Karma11Update.VoidKarma11
		   && self.Malnourished) self.Die();
	}
}
