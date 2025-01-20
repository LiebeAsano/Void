using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class PunishNonPermaDeath
{
	public static void Hook()
	{
		On.ShelterDoor.Close += SavePanish;
	}

	private static void SavePanish(On.ShelterDoor.orig_Close orig, ShelterDoor self)
	{
		orig(self);
		RainWorldGame game = self.room.game;
		game.Players.ForEach(absPlayer =>
		{
			if (absPlayer.realizedCreature is Player player
			&& player.IsVoid())
			{
				var savestate = player.abstractCreature.world.game.GetStorySession.saveState;
				if (!OptionAccessors.PermaDeath || OptionAccessors.PermaDeathCycle > 15 || OptionAccessors.EchoDeathCycle > 5)
				{
					savestate.SetPunishNonPermaDeath(true);
				}
			}
		});
	}
}
