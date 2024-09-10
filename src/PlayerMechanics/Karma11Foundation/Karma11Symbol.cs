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
	const int karma11index = 10;
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
    #region bypassed function
#nullable enable
    static ushort? bypassPetals = null;
	private static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, RWCustom.IntVector2 k)
	{
		if (!small && k.x == -1) return "atlas-void/karma_blank";
		if(k.x == 10)
		{
			if (bypassPetals == null) 
			{ 
				logerr("lookup for karma string summonned without bypass. assuming zero karma tokens. from: " + new System.Diagnostics.StackTrace());
				bypassPetals = 0;
			}
			string res = $"atlas-void/KarmaToken{bypassPetals}" + (small ? "Small" : "Big");
			bypassPetals = null;
			return res;
        }
		return orig(small, k);
	}
#nullable disable
    #endregion


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
        var savestate = (menu as KarmaLadderScreen).saveState;
		bypassPetals = tokensToPelletsMap[(ushort)savestate.GetKarmaToken()];
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
