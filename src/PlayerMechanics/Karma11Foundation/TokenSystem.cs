using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class TokenSystem
{
	public static void Initiate()
	{
		On.RainWorldGame.GoToDeathScreen += (orig, self) =>
		{
			orig(self);
			if (self.IsStorySession
				&& self.GetStorySession.saveState is SaveState saveState
				&& saveState.deathPersistentSaveData.karma == 10)
			{
				int karmaTokensAmount = saveState.GetKarmaToken();
				karmaTokensAmount--;
				saveState.SetKarmaToken(karmaTokensAmount);
				if (karmaTokensAmount < 0) self.GoToRedsGameOver();
			}
		};
	}
}
