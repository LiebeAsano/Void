using Menu;
using System;
using System.Runtime.CompilerServices;
using static VoidTemplate.Useful.Utils;
using MoreSlugcats;
using Expedition;


namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

public static class KarmaLadderTokenDecrease
{
	const float secondsToFadeOut = 3f;
	const float ticksToFadeOut = secondsToFadeOut * TicksPerSecond;
	const float progressPerTick = 1f/ ticksToFadeOut;
	static MenuScene.SceneID sceneToUseWhenTokensDecrease => VoidEnums.SceneID.DeathScene11;
	public static void Initiate()
	{
		//change process ID to be token decrease
		On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
		//making process manager understand how to recognize new ID
		On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
        //making sleepanddeathscreen understand new ID
        On.Menu.KarmaLadder.ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool += KarmaLadder_ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool;
		//all the logic with swapping sprites
		//On.Menu.KarmaLadder.KarmaSymbol.GrafUpdate += KarmaSymbol_GrafUpdate;
        //adding background illustration to new process ID
        On.Menu.SleepAndDeathScreen.AddBkgIllustration += SleepAndDeathScreen_AddBkgIllustration;
		
	}

    private static void SleepAndDeathScreen_AddBkgIllustration(On.Menu.SleepAndDeathScreen.orig_AddBkgIllustration orig, SleepAndDeathScreen self)
    {
		orig(self);
		if(self.ID == VoidEnums.ProcessID.TokenDecrease)
		{
			self.scene = new InteractiveMenuScene(self, self.pages[0], sceneToUseWhenTokensDecrease);
			self.pages[0].subObjects.Add(self.scene);
		}
    }

    private static void KarmaLadder_ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool(On.Menu.KarmaLadder.orig_ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool orig, KarmaLadder self, Menu.Menu menu, MenuObject owner, UnityEngine.Vector2 pos, HUD.HUD hud, RWCustom.IntVector2 displayKarma, bool reinforced)
    {
        orig(self, menu, owner, pos, hud, displayKarma, reinforced);
		if(menu is KarmaLadderScreen kscreen
			&& kscreen.ID == VoidEnums.ProcessID.TokenDecrease)
		{
			changingSymbol = null;
			processDone = null;
		}
    }

    static WeakReference<KarmaLadder.KarmaSymbol> changingSymbol;
	static WeakReference<KarmaLadder.KarmaSymbol> processDone;
	private static void KarmaSymbol_GrafUpdate(On.Menu.KarmaLadder.KarmaSymbol.orig_GrafUpdate orig, KarmaLadder.KarmaSymbol self, float timeStacker)
	{
		orig(self,timeStacker);
		if(self.displayKarma.x == 10)
		{
			_ = 10;
		}
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
					$"{Karma11Symbol.currentKarmaTokens}Big")
				{ x = initialSprite.x,
				 y = initialSprite.y};
				//we are expanding array of sprites and setting last sprite to be the old sprite
				self.sprites[0].alpha = 0f;
				self.sprites[lastIndexOfSprites].alpha = 1f;
				ladder.containers[ladder.MainContainer].AddChild(self.sprites[lastIndexOfSprites]);
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

	private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
	{
		if (ID == VoidEnums.ProcessID.TokenDecrease)
		{
			self.currentMainLoop = new SleepAndDeathScreen(self, ID);
		}
		orig(self, ID);
	}

	private static void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
	{
		bool customLogic = false;
		if (self.IsVoidStoryCampaign()
            && self.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10
			&& self.GetStorySession.saveState.GetKarmaToken() >= 0)
		{
			customLogic = true;
			self.manager.RequestMainProcessSwitch(VoidEnums.ProcessID.TokenDecrease);
		}
		//if upcoming process is null, GoToDeathScreen returns before saving data to disk
		//guess we are working around it
		orig(self);
		if(customLogic)
		{
			self.GetStorySession.saveState.SessionEnded(self, false, false);

		}
	}
}
