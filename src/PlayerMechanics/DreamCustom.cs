namespace VoidTemplate.PlayerMechanics;

public static class DreamCustom
{
	public static void Hook()
	{
		On.DreamsState.EndOfCycleProgress += CustomDream;
	}

	private static void CustomDream(On.DreamsState.orig_EndOfCycleProgress orig, DreamsState self, SaveState saveState, string currentRegion, string denPosition)
	{
		if (saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
		{
			var miscData = saveState.miscWorldSaveData;

			switch (currentRegion)
			{
				case "LF":
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Farm);
						break;
					}
				case "SI":
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sky);
						break;
					}
				case "VS":
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sub);
						break;
					}
			}
            switch (saveState.cycleNumber)
            {
                case >= 18:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidSea);
                        break;
                    }
                case >= 12:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidBody);
                        break;
                    }
                case >= 6:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.NSH);
                        break;
                    }
            }
            switch (saveState.deathPersistentSaveData.karmaCap)
			{
				case 10:
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidHeart);
						break;
					}
				case >= 8:
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidSea);
						break;
					}
				case >= 6:
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidBody);
						break;
					}
				case >= 4:
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.NSH);
						break;
					}
				default:
					{
						if (currentRegion == "SB")
							saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidNSH);
						break;
					}
			}
		}
		orig(self, saveState, currentRegion, denPosition);
	}
}
