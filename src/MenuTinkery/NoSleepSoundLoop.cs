namespace VoidTemplate.MenuTinkery;
using static VoidEnums.SlugcatID;

internal static class NoSleepSoundLoop
{
	public static void Hook()
	{
		On.Menu.DreamScreen.GetDataFromGame += DreamScreen_GetDataFromGame;
	}

	private static void DreamScreen_GetDataFromGame(On.Menu.DreamScreen.orig_GetDataFromGame orig, Menu.DreamScreen self, DreamsState.DreamID dreamID, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
	{
		orig(self, dreamID, package);
		var slugcatID = package.characterStats.name;
		if (slugcatID == Void || slugcatID == Viy)
		{
			if (self.soundLoop != null)
			{
				self.soundLoop.Destroy();
			}
			//you can assign your own self.mySoundLoopID here
			self.mySoundLoopID = SoundID.None;
		}
	}

	/*private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
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
	}*/
}
