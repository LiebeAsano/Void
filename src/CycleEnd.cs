using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;

namespace VoidTemplate;

public static class CycleEnd
{
	private static void log(object e) => _Plugin.logger.LogInfo(e);
	public static void Hook()
	{
		On.ShelterDoor.Close += CycleEndLogic;
		//On.RainWorldGame.Update += RainWorldGame_Update;
		//IL.ShelterDoor.Update += ShelterDoor_Update;
        On.RainWorldGame.Win += RainWorldGame_Win;
	}

    private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
    {
		if (self.manager.upcomingProcess == null)
		{
			if (self.IsVoidWorld() && malnourished && !self.GetStorySession.saveState.GetVoidMarkV3())
			{
				self.GoToDeathScreen();
				return;
			}
			if (self.IsVoidStoryCampaign() && self.GetStorySession.saveState.cycleNumber >= VoidCycleLimit.GetVoidCycleLimit(self.GetStorySession.saveState) && OptionAccessors.PermaDeath && self.Players[0].realizedCreature is Player p2 && p2.KarmaCap != 10 && !self.GetStorySession.saveState.GetVoidMarkV3())
			{
				self.GoToRedsGameOver();
				return;
			}
		}
        orig(self, malnourished, fromWarpPoint);

    }

	private static void ShelterDoor_Update(MonoMod.Cil.ILContext il)
	{
		ILCursor c = new(il);
		var bubblestart = c.DefineLabel();
		var pastbubble = c.DefineLabel();
		// this.room.game <if Void campaign, go to bubblestart>.GoToStarveScreen();
		// < go to bubbleend 
		// bubblestart
		// pop
		// go to death screen
		// bubbleend >
		if (c.TryGotoNext(MoveType.Before,
			x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToStarveScreen))))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<ShelterDoor, bool>>((self) => self.room.game.IsVoidWorld());
			c.Emit(OpCodes.Brtrue_S, bubblestart);
		}
		else
			_Plugin.logger.LogError($"IL hook starting at CycleEnd:23, shelter door update, starve logic tinker, failed to apply");
		if (c.TryGotoNext(MoveType.After,
			 x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToStarveScreen))))
		{
			c.Emit(OpCodes.Br, pastbubble);
			c.MarkLabel(bubblestart);
			c.EmitDelegate((RainWorldGame game) => game.GoToDeathScreen());
			c.MarkLabel(pastbubble);
		}
		else _Plugin.logger.LogError($"IL hook starting at CycleEnd:41, shelter door update, starve logic tinker, failed to apply");
	}

	private const int timeToWait = Utils.TicksPerSecond * 3;

	private static void CycleEndLogic(On.ShelterDoor.orig_Close orig, ShelterDoor self)
	{
		orig(self);
		RainWorldGame game = self.room.game;
		if (game.IsVoidWorld())
		{
			game.Players.ForEach(absPlayer =>
			{
				if (absPlayer.realizedCreature is Player player
				&& player.IsVoid())
				{
					var savestate = player.abstractCreature.world.game.GetStorySession.saveState;

					if (player.room != null
					&& player.room == self.room
					&& player.FoodInStomach < player.slugcatStats.foodToHibernate
					&& self.room.game.session is StoryGameSession session
					&& session.characterStats.name == VoidEnums.SlugcatID.Void
					&& (!ModManager.Expedition || !self.room.game.rainWorld.ExpeditionMode))
					{
						if (((session.saveState.cycleNumber >= VoidCycleLimit.GetVoidCycleLimit(session.saveState) || session.saveState.deathPersistentSaveData.karma == 0) && OptionAccessors.PermaDeath) || savestate.GetKarmaToken() == 0) game.GoToRedsGameOver();
					}

				}
			});
		}
	}

}
