using HUD;
using Menu;
using VoidTemplate.PlayerMechanics.Karma11Features;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

public static class KarmaLadderTweaks
{
	const int karma11index = 10;
	public static void Hook()
	{
		On.Menu.SleepAndDeathScreen.FoodCountDownDone += SleepAndDeathScreen_FoodCountDownDone;
	}

	private static void SleepAndDeathScreen_FoodCountDownDone(On.Menu.SleepAndDeathScreen.orig_FoodCountDownDone orig, Menu.SleepAndDeathScreen self)
	{
		orig(self);
		if (self.karma.x == 10)
		{
			self.karmaLadder.GoToKarma(10, true);
			if (self.IsSleepScreen)
			{
				if (self.saveState.VoidFullAnd11Karma(self.food, self.hud.foodMeter.survivalLimit, self.hud.foodMeter.maxFood))
				{
					if (self.saveState.CanAddFoodToHibernate(self.hud.foodMeter.survivalLimit))
					{
                        if (Karma11Symbol.currentKarmaTokens > 0)
                            self.karmaLadder.NewPhase(KarmaLadder.Phase.Bump);
						Karma11Symbol.currentKarmaTokens -= 1;
						self.karmaLadder.karmaSymbols[10].UpdateDisplayKarma(self.karmaLadder.displayKarma);
					}
					else if (self.saveState.GetVoidFoodToHibernate() > 0 && !self.saveState.CanAddFoodToHibernate(self.hud.foodMeter.survivalLimit))
					{
						if (Karma11Symbol.currentKarmaTokens > 0)
							self.karmaLadder.NewPhase(KarmaLadder.Phase.Bump);
						Karma11Symbol.currentKarmaTokens = 0;
						self.karmaLadder.karmaSymbols[10].UpdateDisplayKarma(self.karmaLadder.displayKarma);
					}
				}
			}
			else if (self.ID == VoidEnums.ProcessID.TokenDecrease)
			{
				self.karmaLadder.NewPhase(KarmaLadder.Phase.Settling);
				self.PlaySound(SoundID.MENU_Karma_Ladder_Reinforce_Save_Pull);
                self.hud.fadeCircles.Add(new FadeCircle(self.hud, self.karmaLadder.circleRad, 26f, 0.88f, 60f, 6f, self.karmaLadder.DrawPos(1f), self.karmaLadder.containers[self.karmaLadder.FadeCircleContainer]));
                Karma11Symbol.currentKarmaTokens -= 1;
                self.karmaLadder.karmaSymbols[10].UpdateDisplayKarma(self.karmaLadder.displayKarma);
            }
		}
	}
}
