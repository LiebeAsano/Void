using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;
using static VoidTemplate.OptionInterface.OptionAccessors;
using static VoidTemplate.Useful.Utils;
using Object = UnityEngine.Object;


namespace VoidTemplate;

static class PermadeathConditions
{
	public static void Hook()
	{
		On.RainWorldGame.GameOver += GenericGameOver;
		On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
		On.Menu.KarmaLadder.KarmaSymbol.Update += PulsateKarmaSymbol;
		//On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
		On.RainWorldGame.ExitToMenu += ExitToMenuGameOver;
		Application.quitting += ApplicationQuitGameOver;

		IL.Menu.KarmaLadderScreen.GetDataFromGame += KarmaLadderScreen_GetDataFixMSCStupidBug;
		IL.HUD.TextPrompt.Update += TextPrompt_Update;
	}

	private static void TextPrompt_Update(ILContext il)
	{
		ILCursor c = new(il);
		var bubbleStart = c.DefineLabel();
		var bubbleEnd = c.DefineLabel();
		// this code makes it so that exiting game with void in prepermadeath conditions leads you to game over screen
		if (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToDeathScreen))))
		{
			c.Emit(OpCodes.Dup);
			c.EmitDelegate<Func<RainWorldGame, bool>>(VoidSpecificGameOverCondition);
			c.Emit(OpCodes.Brtrue, bubbleStart);
		}
		else logerr("IL failed to match.\n" + new StackTrace().ToString());
		if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToDeathScreen))))
		{
			c.Emit(OpCodes.Br, bubbleEnd);
			c.MarkLabel(bubbleStart);
			c.EmitDelegate((RainWorldGame game) => game.GoToRedsGameOver());
			c.MarkLabel(bubbleEnd);
		}
		else logerr("IL failed to match.\n" + new StackTrace().ToString());
	}

	private static void KarmaLadderScreen_GetDataFixMSCStupidBug(ILContext il)
	{
		ILCursor c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After, i => i.MatchLdarg(0),
			i => i.MatchLdcI4(4)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<int, KarmaLadderScreen, KarmaLadderScreen.SleepDeathScreenDataPackage, int>>(
			(re, self, package) =>
			{
				if (package.saveState != null && package.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
					if (self.ID == ProcessManager.ProcessID.GhostScreen)
						return self.preGhostEncounterKarmaCap;
					else
						return self.karma.y;
				return re;
			});
		}

	}

	private static void PulsateKarmaSymbol(On.Menu.KarmaLadder.KarmaSymbol.orig_Update orig, KarmaLadder.KarmaSymbol self)
	{

		var flag = ModManager.MSC
			&& self.parent.displayKarma.x == self.parent.moveToKarma
			&& (self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.KarmaToMinScreen || self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.VengeanceGhostScreen || (ModManager.Expedition
				&& self.menu.manager.rainWorld.ExpeditionMode
				&& self.parent.moveToKarma == 0));
		if (!flag && ModManager.MSC && self.parent.displayKarma.x == self.parent.moveToKarma &&
			self.menu is KarmaLadderScreen screen && screen.saveState?.saveStateNumber == VoidEnums.SlugcatID.Void
			&& self.parent.moveToKarma == 0 && self.parent.menu.ID == ProcessManager.ProcessID.DeathScreen
			&& PermaDeath)
		{
			self.waitForAnimate++;
			if (self.waitForAnimate >= 50)
				if (self.displayKarma.x == 0)
					self.pulsateCounter++;
		}
		orig(self);
	}

	private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
	{
		if (self.GetStorySession.saveState.saveStateNumber == VoidEnums.SlugcatID.Void
			&& !(ModManager.Expedition && self.rainWorld.ExpeditionMode))
		{
			if (self.manager.upcomingProcess != null) return;

			self.manager.musicPlayer?.FadeOutAllSongs(20f);
            /*if (self.manager.nextSlideshow != null)
			{
				self.manager.statsAfterCredits = true;
				self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
				return;
			}*/
            if (VoidSpecificGameOverCondition(self))
			{
                self.GetStorySession.saveState.redExtraCycles = true;
                self.GetStorySession.saveState.SetVoidCatDead(true);
            }

			if (ModManager.CoopAvailable)
			{
				int num = 0;
				using IEnumerator<Player> enumerator =
					(from x in self.session.game.Players select x.realizedCreature as Player).GetEnumerator();
				while (enumerator.MoveNext())
				{
					Player player = enumerator.Current;
					self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
					num++;
				}
			}
			else
				self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);


			self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
			self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics, 10f);
			return;
		}
		orig(self);
	}
	#region GameOverConditions
	public static void SetVoidCatDeadTrue(RainWorldGame game)
	{
		if (game.StoryCharacter == VoidEnums.SlugcatID.Void
			&& game.IsStorySession
			&& game.GetStorySession.saveState is SaveState save
			&& PermaDeath)
		{
			Player player = null;
			foreach (var abstractPlayer in game.Players)
			{
				if (abstractPlayer.realizedCreature is Player mainPlayer)
				{
					player = mainPlayer;
					break;
				}
			}
			var savestate = player.abstractCreature.world.game.GetStorySession.saveState;
            if (player.KarmaCap == 10) savestate.SetKarmaToken(Math.Max(0, savestate.GetKarmaToken() - 1));
			save.SetVoidCatDead(true);
			save.redExtraCycles = true;
			game.rainWorld.progression.SaveWorldStateAndProgression(false);
		}
	}
	private static void ApplicationQuitGameOver()
	{
		RainWorld rainWorld = Object.FindObjectOfType<RainWorld>();
		if (rainWorld != null
			&& rainWorld.processManager is ProcessManager manager
			&& manager.currentMainLoop is RainWorldGame game)
		{
			if (VoidSpecificGameOverCondition(game))
			{
                SetVoidCatDeadTrue(game);
            }
			if (game.GetStorySession.saveState.GetKarmaToken() > 0
				&& game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
			{
                var savestate = game.GetStorySession.saveState;
                savestate.SetKarmaToken(Math.Max(0, savestate.GetKarmaToken() - 1));
				savestate.SessionEnded(game, false, false);
            }
        }
	}
	private static bool VoidSpecificGameOverCondition(RainWorldGame rainWorldGame)
	{
		return rainWorldGame.session is StoryGameSession session
			&& session.characterStats.name == VoidEnums.SlugcatID.Void
            && rainWorldGame.IsVoidStoryCampaign()
            && (session.saveState.deathPersistentSaveData.karma == 0 && PermaDeath
			|| session.saveState.GetKarmaToken() == 0
            || session.saveState.cycleNumber >= VoidCycleLimit.GetVoidCycleLimit(session.saveState) && session.saveState.deathPersistentSaveData.karmaCap != 10 && !session.saveState.GetVoidMarkV3() && PermaDeath)
            && !(ModManager.Expedition && rainWorldGame.rainWorld.ExpeditionMode);
	}

	private static void ExitToMenuGameOver(On.RainWorldGame.orig_ExitToMenu orig, RainWorldGame self)
	{
		orig(self);
		if (VoidSpecificGameOverCondition(self) && self.world.rainCycle.timer > 30 * TicksPerSecond) SetVoidCatDeadTrue(self);
		if (self.session is StoryGameSession session && self.world.rainCycle.timer > 30 * TicksPerSecond)
		{
            var savestate = self.world.game.GetStorySession.saveState;
            session.saveState.SetKarmaToken(Math.Max(0, savestate.GetKarmaToken() - 1));
            savestate.SessionEnded(self.world.game, false, false);
        }
	}
	private static void GenericGameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
	{
		if (self.IsVoidWorld())
		{
			if (ModManager.CoopAvailable && self.rainWorld.options.JollyPlayerCount > 1)
			{
				if (!self.JollyGameOverEvaluator(dependentOnGrasp))
				{
					return;
				}
			}
			if (!self.playedGameOverSound && dependentOnGrasp == null)
			{
				self.cameras[0].hud.PlaySound(SoundID.HUD_Game_Over_Prompt);
				self.playedGameOverSound = true;
			}
			if (VoidSpecificGameOverCondition(self) && dependentOnGrasp == null)
			{
				self.GoToRedsGameOver();
			}
		}
		orig(self, dependentOnGrasp);
	}
	#endregion
}