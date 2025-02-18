namespace VoidTemplate.PlayerMechanics.Karma11Foundation;
using static VoidTemplate.Useful.Utils;

internal static class TokenSystem
{
	public static void Initiate()
	{
		On.RainWorldGame.GoToDeathScreen += (orig, self) =>
		{
			if (self.IsStorySession
				&& self.GetStorySession.saveState is SaveState saveState
				&& saveState.deathPersistentSaveData.karma == 10)
			{
				int karmaTokensAmount = saveState.GetKarmaToken();
				karmaTokensAmount--;
				saveState.SetKarmaToken(karmaTokensAmount);
				Karma11Symbol.currentKarmaTokens = (ushort)karmaTokensAmount;
				if (karmaTokensAmount < 0 && self.IsVoidStoryCampaign()) self.GoToRedsGameOver();
			}
			orig(self); //orig contains saving file to disk, so it must be called after changing token amount
		};
	}
}
