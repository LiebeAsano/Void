using Menu;
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

        On.Menu.KarmaLadder.Update += KarmaLadder_Update;
		//TODO: add new sprite, do fadeout
	}
	static ConditionalWeakTable<KarmaLadder, PositionedMenuObject> newKarmaToken = new();

    private static void KarmaLadder_Update(On.Menu.KarmaLadder.orig_Update orig, KarmaLadder self)
    {
		orig(self);
		if(tokenFadeoutProcess.TryGetValue(self, out var process))
		{
			if(!newKarmaToken.TryGetValue(self, out var newKarma)) newKarmaToken.Add(self, )
			process.Value += progressPerTick;
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
		if (self.IsVoidStoryCampaign()
			&& self.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10
			&& self.GetStorySession.saveState.GetKarmaToken() > 0)
		{
			self.manager.RequestMainProcessSwitch(VoidEnums.ProcessID.TokenDecrease);
		}
		orig(self);
	}
}
