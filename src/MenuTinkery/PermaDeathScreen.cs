namespace VoidTemplate.MenuTinkery;
using Menu;
using RWCustom;
using UnityEngine;
using MoreSlugcats;
using static VoidTemplate.Useful.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

internal static class PermaDeathScreen
{
	public static void Hook()
	{
		//when needed, initiate karmatominscreen process
		On.Menu.KarmaLadder.ctor += KarmaLadder_ctor;
		//special treatment for go to karma if karma0 exists
		On.Menu.KarmaLadder.GoToKarma += KarmaLadder_GoToKarma;
		//fetch empty karma
		On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;
		//make the empty karma symbol not turn red in the end of permadeath animation
		//and also not pulsate
		On.Menu.KarmaLadder.KarmaSymbol.GrafUpdate += KarmaSymbol_GrafUpdate;
		//for debug purposes. press H to go to game over screen
		On.RainWorldGame.Update += RainWorldGame_Update;
	}

	static bool IsPlummetingScreen(this KarmaLadder karmaLadder) => karmaLadder.karmaSymbols[0].sprites[karmaLadder.karmaSymbols[0].KarmaSprite].element.name.Contains("blank");


	private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
	{
		orig(self);
		if (Input.GetKey(KeyCode.H))
		{
			self.GetStorySession.saveState.redExtraCycles = true;
			self.GoToRedsGameOver();
		}
	}

	private static void KarmaLadder_GoToKarma(On.Menu.KarmaLadder.orig_GoToKarma orig, KarmaLadder self, int newGoalKarma, bool displayMetersOnRest)
	{
		orig(self, newGoalKarma, displayMetersOnRest);
		if (self.IsPlummetingScreen())
		{
			self.movementShown = true;
			self.showEndGameMetersCounter = 85;
		}
	}

	private static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, RWCustom.IntVector2 k)
	{
		if (!small && k.x == -1) return "atlas-void/karma_blank";
		return orig(small, k);
	}

	private static void KarmaSymbol_GrafUpdate(On.Menu.KarmaLadder.KarmaSymbol.orig_GrafUpdate orig, KarmaLadder.KarmaSymbol self, float timeStacker)
	{
		orig(self, timeStacker);
		if (self.displayKarma == new IntVector2(0, 0) && self.parent.IsPlummetingScreen())
		{
			self.pulsateCounter = 0;
			var color = Color.white;
			self.sprites[self.RingSprite].color = color;
			self.sprites[self.KarmaSprite].color = color;
			self.sprites[self.LineSprite].color = color;
			self.sprites[self.GlowSprite(0)].color = color;
			self.sprites[self.GlowSprite(1)].color = color;
		}
	}

	private static void KarmaLadder_ctor(On.Menu.KarmaLadder.orig_ctor orig, KarmaLadder self, Menu menu, MenuObject owner, Vector2 pos, HUD.HUD hud, IntVector2 displayKarma, bool reinforced)
	{
		var screen = menu as KarmaLadderScreen;
		bool needInsert = false;

		if (screen.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
		{
			if ((screen.saveState.redExtraCycles) && screen.saveState.deathPersistentSaveData.karmaCap != 10)
			{
				screen.ID = MoreSlugcatsEnums.ProcessID.KarmaToMinScreen;
				needInsert = true;
			}
		}

		orig(self, menu, owner, pos, hud, displayKarma, reinforced);
		if (needInsert)
		{
			var zeroKarma = new KarmaLadder.KarmaSymbol(menu, self,
				new Vector2(0f, 0f), self.containers[self.MainContainer],
				self.containers[self.FadeCircleContainer], new IntVector2(-1, 0));

			self.karmaSymbols.Insert(0, zeroKarma);
			self.subObjects.Add(zeroKarma);
			zeroKarma.sprites[zeroKarma.KarmaSprite].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);
			zeroKarma.sprites[zeroKarma.RingSprite].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);
			zeroKarma.sprites[zeroKarma.LineSprite].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);

			zeroKarma.sprites[zeroKarma.GlowSprite(0)].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].GlowSprite(0)]);
			zeroKarma.sprites[zeroKarma.GlowSprite(1)].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].GlowSprite(0)]);
			foreach (var symbol in self.karmaSymbols)
				symbol.displayKarma.x++;
			self.displayKarma.x++;
			self.scroll = self.displayKarma.x;
			self.lastScroll = self.displayKarma.x;
		}
	}
}
