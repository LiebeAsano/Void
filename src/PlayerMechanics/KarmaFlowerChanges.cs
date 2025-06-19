using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public class KarmaFlowerChanges
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
		if (grasp.grabber is Player player && (player.IsVoid() && self.bites < 2 || player.IsViy()))
		{
			self.room.PlaySound((self.bites == 1) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk.pos);
			self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
			int random = UnityEngine.Random.Range(0, 3);
			if (random == 0)
			{
                player.abstractCreature.world.game.GetStorySession.saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidNSH);
            }
            if (self.bites == 1 && player.KarmaCap == 10 && !player.IsViy())
			{
				var savestate = player.abstractCreature.world.game.GetStorySession.saveState;
				var amountOfTokens = Math.Min(10, savestate.GetKarmaToken() + 2);
                savestate.SetKarmaToken(amountOfTokens);
				bool needBumpTokenAnim = Karma11Foundation.Karma11Symbol.currentKarmaTokens != 10; 
				Karma11Foundation.Karma11Symbol.currentKarmaTokens = (ushort)amountOfTokens;
				if(needBumpTokenAnim) self.room.game.cameras[0].hud.karmaMeter.reinforceAnimation = 0;
			}
			grasp.Release();
			self.Destroy();
			return;
		}
		orig(self, grasp, eu);
	}
}
