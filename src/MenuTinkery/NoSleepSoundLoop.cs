namespace VoidTemplate.MenuTinkery;
using static VoidEnums.SlugcatID;

internal static class NoSleepSoundLoop
{
	public static void Hook()
	{
		On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
	}

	private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
	{
		orig(self, package);
		var slugcatID = package.characterStats.name;
		if(slugcatID == Void || slugcatID == Viy)
		{
			if(self.soundLoop != null)
			{
				self.soundLoop.Destroy();
			}
			//you can assign your own self.mySoundLoopID here
			self.mySoundLoopID = SoundID.None;
		}
	}
}
