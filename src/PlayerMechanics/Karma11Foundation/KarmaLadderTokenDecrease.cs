using Menu;
using System;
using System.Runtime.CompilerServices;
using static VoidTemplate.Useful.Utils;


namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class KarmaLadderTokenDecrease
{
	const float secondsToFadeOut = 2f;
	const float ticksToFadeOut = secondsToFadeOut * TicksPerSecond;
	const float progressPerTick = 1f/ ticksToFadeOut;
	public static void Initiate()
	{
		//change process ID to be token decrease
		On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
		//making process manager understand how to recognize new ID
		On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
		//making sleepanddeathscreen understand new ID
		On.Menu.SleepAndDeathScreen.FoodCountDownDone += SleepAndDeathScreen_FoodCountDownDone;

        On.Menu.KarmaLadder.KarmaSymbol.GrafUpdate += KarmaSymbol_GrafUpdate;
		//TODO: add new sprite, do fadeout
	}


	static WeakReference<KarmaLadder.KarmaSymbol> changingSymbol;
	static WeakReference<KarmaLadder.KarmaSymbol> processDone;
    private static void KarmaSymbol_GrafUpdate(On.Menu.KarmaLadder.KarmaSymbol.orig_GrafUpdate orig, KarmaLadder.KarmaSymbol self, float timeStacker)
    {
		orig(self,timeStacker);
		if (self.owner is KarmaLadder ladder
			&& self.displayKarma.x == 10
			&& tokenFadeoutProcess.TryGetValue(ladder, out var process))
		{
			if(changingSymbol == null || !changingSymbol.TryGetTarget(out var target))
			{
                #region init sprite
                changingSymbol = new(self);
				Array.Resize(ref self.sprites, self.sprites.Length + 1);
				int lastIndexOfSprites = self.sprites.Length-1;
				var initialSprite = self.sprites[0];
				self.sprites[lastIndexOfSprites] = new FSprite($"atlas-void/KarmaToken" +
					$"{Karma11Symbol.tokensToPelletsMap[(ushort)(Karma11Symbol.currentKarmaTokens + 1)]}Small")
				{ x = initialSprite.x,
				 y = initialSprite.y};
				//we are expanding array of sprites and setting last sprite to be the old sprite
				self.sprites[0].alpha = 0f;
				self.sprites[lastIndexOfSprites].alpha = 1f;
				#endregion
			}
			process.Value += progressPerTick;
			if (process.Value <= 1f)
			{
				self.sprites[0].alpha = process.Value;
				self.sprites[self.sprites.Length - 1].alpha = 1 - process.Value;
			}
			else if (processDone == null || !processDone.TryGetTarget(out var target2))
			{
				Array.Resize(ref self.sprites, self.sprites.Length - 1);
				processDone = new(self);
			}
        }
    }


    static ConditionalWeakTable<KarmaLadder, StrongBox<float>> tokenFadeoutProcess = new();

	private static void InitTokenDecrease(this KarmaLadder karmaLadder)
	{
		tokenFadeoutProcess.Add(karmaLadder, new(0f));
	}

	private static void SleepAndDeathScreen_FoodCountDownDone(On.Menu.SleepAndDeathScreen.orig_FoodCountDownDone orig, SleepAndDeathScreen self)
	{
		if(self.ID == VoidEnums.ProcessID.TokenDecrease)
		{
			self.karmaLadder.InitTokenDecrease();
		}
		else orig(self);
	}

	private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
	{
		orig(self, ID);
		if(ID == VoidEnums.ProcessID.TokenDecrease)
		{
			self.currentMainLoop = new SleepAndDeathScreen(self, ID);
		}
	}

	private static void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
	{
        orig(self);
        if (self.IsVoidStoryCampaign()
			&& self.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10
			&& self.GetStorySession.saveState.GetKarmaToken() > 0)
		{
			self.manager.RequestMainProcessSwitch(VoidEnums.ProcessID.TokenDecrease);
			loginf("requested process switch to token decrease.");
		}
		loginf("post orig. the process game wants to switch to is " + self.manager.upcomingProcess.value);
	}
}
