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
#warning WIP
		IL.Menu.KarmaLadder.NewPhase += KarmaLadder_NewPhase;
		//for debug purposes. press H to go to game over screen
		//On.RainWorldGame.Update += RainWorldGame_Update;
	}

	private static void KarmaLadder_NewPhase(MonoMod.Cil.ILContext il)
	{
		ILCursor c = new(il);
		ILLabel skipSoundPlay = c.DefineLabel();
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.MENU_Karma_Ladder_Reselect)))))
		{
			//moving past soundID play call
			c.Index++;
			c.MarkLabel(skipSoundPlay);
		}
		else LogExErr("failed to find reselection. skipping defining label");
		if (c.TryGotoPrev(MoveType.Before,
			x => x.MatchLdarg(0)))
		{
			c.Emit(OpCodes.Ldarg, 0);
			c.EmitDelegate<Predicate<KarmaLadder>>((KarmaLadder self) => self.karmaSymbols[0].sprites[self.karmaSymbols[0].KarmaSprite].element.name.Contains("blank"));
			c.Emit(OpCodes.Brtrue, skipSoundPlay);
		}
		else LogExErr("it shouldn't be possible to miss ldarg0 but somehoww you did it.");
	}

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
		if (self.karmaSymbols[0].sprites[self.karmaSymbols[0].KarmaSprite].element.name.Contains("blank"))
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

			//removing the ring surrounding empty karma
			zeroKarma.sprites[zeroKarma.RingSprite].RemoveFromContainer();

			self.karmaSymbols.Insert(0, zeroKarma);
			self.subObjects.Add(self.karmaSymbols[0]);
			self.karmaSymbols[0].sprites[self.karmaSymbols[0].KarmaSprite].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);
			self.karmaSymbols[0].sprites[self.karmaSymbols[0].RingSprite].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);
			self.karmaSymbols[0].sprites[self.karmaSymbols[0].LineSprite].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);

			self.karmaSymbols[0].sprites[self.karmaSymbols[0].GlowSprite(0)].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].GlowSprite(0)]);
			self.karmaSymbols[0].sprites[self.karmaSymbols[0].GlowSprite(1)].MoveBehindOtherNode(
				self.karmaSymbols[1].sprites[self.karmaSymbols[1].GlowSprite(0)]);
			foreach (var symbol in self.karmaSymbols)
				symbol.displayKarma.x++;
			self.displayKarma.x++;
			self.scroll = self.displayKarma.x;
			self.lastScroll = self.displayKarma.x;
		}
	}
}
