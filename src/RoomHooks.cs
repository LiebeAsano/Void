using System.Linq;
using VoidTemplate.Objects;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate
{
	static class RoomHooks
	{
		public static void Hook()
		{
			On.RainWorldGame.ctor += RainWorldGame_ctor;
			On.Room.Loaded += RoomSpeficScript;
			On.TempleGuardAI.Update += TempleGuardAI_Update;
			On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
		}

		private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
		{
			if (crit.representedCreature.realizedCreature is Player player && player.IsVoid())
			{
				return 500f;
			}
			return orig(self, crit);
		}

		private static void TempleGuardAI_Update(On.TempleGuardAI.orig_Update orig, TempleGuardAI self)
		{
			orig(self);
			if (self.guard.room.PlayersInRoom.Any(i => i.IsVoid()))
			{
				self.patience = 9999;
			}
		}

		private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
		{
			orig(self, manager);
		}

		private static void RoomSpeficScript(On.Room.orig_Loaded orig, Room self)
		{
			orig(self);
			if (self.game != null && self.game.session.characterStats.name == VoidEnums.SlugcatID.TheVoid)
			{
				SaveState saveState = self.game.GetStorySession.saveState;
				if (!saveState.GetMessageShown() && self.game.Players.Exists(x => x.realizedCreature is Player p && p.IsVoid() && p.KarmaCap == 4))
				{
					self.AddObject(new CeilingClimbTutorial(self,
					[
						new("Your body is strong enough to climb on any surface.", 0, 444)
					]));
				}
				switch (self.abstractRoom.name)
				{
					case "SB_E05":
						self.AddObject(new ClimbTutorial(self));
						break;
					case VoidEnums.RoomNames.EndingRoomName:
						self.AddObject(new Ending(self));
						break;
					case "SL_AI" when !saveState.GetDreamData(SaveManager.Dream.Moon).WasShown:
						self.AddObject(new EnqueueMoonDream(self));
						break;
				}

			}
		}

	}
}
