using Menu;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.OptionInterface;

namespace VoidTemplate.MenuTinkery;

public static class StopContinueButtonWhenAboutToDie
{
	private const float secondsToWatchForFlickering = 2;
	private const float ticksToWatchForFlickering = secondsToWatchForFlickering * 40;
	public static void Hook()
	{
		//forcing watching animation when going to karma 1 as void
		On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
		//preventing button being clicked until button has flickered for a bit
		new Hook(typeof(Menu.SleepAndDeathScreen).GetProperty(nameof(SleepAndDeathScreen.ButtonsGreyedOut)).GetGetMethod(), ButtonsGreyedOut);
	}

	private static bool ButtonsGreyedOut(Func<SleepAndDeathScreen, bool> orig, SleepAndDeathScreen self)
	{
		if(self.myGamePackage.characterStats.name == VoidEnums.SlugcatID.Void
			&& self.karma.x == 1
			&& self.IsDeathScreen
			&& OptionAccessors.PermaDeath)
			return orig(self) && self.karmaLadder.karmaSymbols[0].flickerCounter < ticksToWatchForFlickering;
		return orig(self);
	}
	private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
	{
		orig(self, package);
		if(package.characterStats.name == VoidEnums.SlugcatID.Void
			&& self.karma.x == 1
			&& self.IsDeathScreen
            && OptionAccessors.PermaDeath)
		{
			self.forceWatchAnimation = true;
		}
	}
}
