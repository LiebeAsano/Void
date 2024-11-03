using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.VoidEnums;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class Karma11Symbol
{
	public static void Startup()
	{
		On.Menu.KarmaLadder.KarmaSymbol.ctor += KarmaSymbol_ctor;
		On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;
		On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState;
	}

	const int karma11index = 10;
	public static Dictionary<ushort, ushort> tokensToPelletsMap = new()
	{
		{0, 0},
		{1, 1},
		{2, 1},
		{3, 2},
		{4, 2},
		{5, 3},
		{6, 3},
		{7, 4},
		{8, 4},
		{9, 5},
		{10, 5},
	};
	public static ushort currentKarmaTokens = 0;

	private static SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
	{
		SaveState result = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
		if (saveStateNumber == SlugcatID.Void)
		{
			currentKarmaTokens = (ushort)result.GetKarmaToken();
        }
		return result;
	}

	private static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, RWCustom.IntVector2 k)
	{
		if (k.x == 10)
		{
			string res = $"atlas-void/KarmaToken{tokensToPelletsMap[currentKarmaTokens]}" + (small ? "Small" : "Big");
			return res;
		}
		return orig(small, k);
	}


	/// <summary>
	/// this introduces spawning logic for when required karma is (10,10) and makes all other invisible if it's the current one
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	/// <param name="menu"></param>
	/// <param name="owner"></param>
	/// <param name="pos"></param>
	/// <param name="container"></param>
	/// <param name="foregroundContainer"></param>
	/// <param name="displayKarma"></param>
	private static void KarmaSymbol_ctor(On.Menu.KarmaLadder.KarmaSymbol.orig_ctor orig, KarmaLadder.KarmaSymbol self, Menu.Menu menu, MenuObject owner, Vector2 pos, FContainer container, FContainer foregroundContainer, IntVector2 displayKarma)
	{
		orig(self, menu, owner, pos, container, foregroundContainer, displayKarma);

		if (displayKarma.x == karma11index)
		{
			self.sprites[self.RingSprite].alpha = 0f;
		}
		if (self.ladder.displayKarma.x == karma11index && displayKarma.x != karma11index)
		{
			Array.ForEach(self.sprites, sprite => sprite.alpha = 0f);
		}
	}
}
