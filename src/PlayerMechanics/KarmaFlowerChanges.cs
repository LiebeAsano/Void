using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal class KarmaFlowerChanges
{
	public static void Initiate()
	{
		On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
		On.Player.FoodInRoom_Room_bool += Player_FoodInRoom_Room_bool;
	}

	private static int Player_FoodInRoom_Room_bool(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
	{
		var res = orig(self, checkRoom, eatAndDestroy);
		if (self.IsVoid() && checkRoom.game.IsStorySession) checkRoom.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = false;
		return res;
	}

	private static void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
	{
		if (self.bites < 2 && grasp.grabber is Player player && player.IsVoid())
		{
			self.bites--;
			self.room.PlaySound((self.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk.pos);
			self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
			if (self.bites == 0 && player.KarmaCap == 10)
			{
				var savestate = player.abstractCreature.world.game.GetStorySession.saveState;
				savestate.SetKarmaToken(Math.Min(10, savestate.GetKarmaToken() + 2));
			}
			grasp.Release();
			self.Destroy();
			return;
		}
		orig(self, grasp, eu);
	}
}
