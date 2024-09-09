using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static VoidTemplate.Useful.Utils;
using RWCustom;
using System.Runtime.CompilerServices;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class Karma11Symbol
{
	const ushort maximumTokens = 10;
	static Dictionary<ushort, ushort> tokensToPelletsMap = new()
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
	

	public static void Startup()
	{
		On.Menu.KarmaLadder.KarmaSymbol.ctor += KarmaSymbol_ctor;
		On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;
	}



	private static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, RWCustom.IntVector2 k)
	{
		if (!small && k.x == -1) return "atlas-void/karma_blank";
		return orig(small, k);
	}

	private static void KarmaSymbol_ctor(On.Menu.KarmaLadder.KarmaSymbol.orig_ctor orig, KarmaLadder.KarmaSymbol self, Menu.Menu menu, MenuObject owner, Vector2 pos, FContainer container, FContainer foregroundContainer, IntVector2 displayKarma)
	{
		if(displayKarma == new IntVector2(10, 10))
		{
			self.sprites = new FSprite[6];
			var savestate = (menu as KarmaLadderScreen).saveState;

			
		}
		orig(self, menu, owner, pos, container, foregroundContainer, displayKarma);
	}
}
