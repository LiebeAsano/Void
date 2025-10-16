namespace VoidTemplate.PlayerMechanics.Karma11Foundation;
using static VoidTemplate.Useful.Utils;

public static class TokenSystem
{
	public static void Initiate()
	{
		On.RainWorldGame.GoToDeathScreen += (orig, self) =>
		{
			if (self.IsStorySession
				&& self.GetStorySession.saveState is SaveState saveState
				&& saveState.deathPersistentSaveData.karma == 10
                && self.IsVoidStoryCampaign())
			{
				int karmaTokensAmount = saveState.GetKarmaToken();
				karmaTokensAmount -= 1;
				saveState.SetKarmaToken(karmaTokensAmount);
				if (karmaTokensAmount < 0) self.GoToRedsGameOver();
			}
			orig(self); //orig contains saving file to disk, so it must be called after changing token amount
		};
	}
}
