using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.VoidEnums;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

public static class Karma11Symbol
{
	public static void Startup()
	{
		On.Menu.KarmaLadder.KarmaSymbol.ctor += KarmaSymbol_ctor;
		On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;
		On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState;
		On.Menu.SleepAndDeathScreen.ctor += SleepAndDeathScreen_ctor;
    }

    const int karma11index = 10;
	public static ushort currentKarmaTokens = 0;

    private static SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
	{
		SaveState result = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
		if (saveStateNumber == SlugcatID.Void || saveStateNumber == SlugcatID.Viy)
		{
			currentKarmaTokens = (ushort)result.GetKarmaToken();
        }
		return result;
	}

	private static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, RWCustom.IntVector2 k)
	{
		if (k.x == 10)
		{
			string res = $"atlas-void/KarmaToken{Mathf.Clamp(currentKarmaTokens, 0, 5)}" + (small ? "Small" : "Big");
			return res;
		}
		return orig(small, k);
	}

    private static void SleepAndDeathScreen_ctor(On.Menu.SleepAndDeathScreen.orig_ctor orig, SleepAndDeathScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
    {	
		orig(self, manager, ID);

        if (self?.saveState == null) return;

        if (self.saveState.saveStateNumber == SlugcatID.Void)
        {
            SaveState saveState = manager.rainWorld.progression.GetOrInitiateSaveState(SlugcatID.Void, null, manager.rainWorld.processManager.menuSetup, false);
            int maxFood = 9 + (saveState.deathPersistentSaveData.karmaCap == 10 ? saveState.GetVoidExtraFood() : 0);
            int foodToHibernate = 6 + (saveState.GetVoidExtraFood() == 3 ? saveState.GetVoidFoodToHibernate() : 0);
            if (ID == ProcessID.TokenDecrease
                || ID == ProcessManager.ProcessID.SleepScreen
                && maxFood == saveState.food + foodToHibernate - 1
                && saveState.GetVoidFoodToHibernate() < 6
                && saveState.GetKarmaToken() > 0)
                currentKarmaTokens++;
        }
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
    private static void KarmaSymbol_ctor(On.Menu.KarmaLadder.KarmaSymbol.orig_ctor orig, KarmaLadder.KarmaSymbol self, Menu.Menu menu, MenuObject owner, Vector2 pos, FContainer container, FContainer foregroundContainer, IntVector2 displayKarma, bool ripple)
	{
		orig(self, menu, owner, pos, container, foregroundContainer, displayKarma, ripple);
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
