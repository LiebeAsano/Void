using VoidTemplate.PlayerMechanics.Karma11Features;

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
			switch (currentRegion)
			{
				case "LF":
					{
						int random = UnityEngine.Random.Range(0, 3);
						if (random == 0)
							saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Farm);
						break;
					}
				case "SI":
					{
                        int random = UnityEngine.Random.Range(0, 3);
                        if (random == 0)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sky);
						break;
					}
				case "SB":
					{
                        int random = UnityEngine.Random.Range(0, 3);
                        if (random == 0)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sub);
						break;
					}
			}
            switch (saveState.cycleNumber)
			{
				case >= 24:
					{
						saveState.EnlistDreamIfNotSeen(SaveManager.Dream.HunterRot);
						break;
					}
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
						if (Karma11Update.VoidKarma11)
							saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidHeart);
						break;
					}
			}
		}
		orig(self, saveState, currentRegion, denPosition);
	}
}
