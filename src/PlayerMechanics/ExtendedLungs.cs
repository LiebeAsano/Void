using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class ExtendedLungs
{
	public static void Hook()
	{
		On.StoryGameSession.ctor += StoryGameSession_ctor;
	}

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        if (saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            if (self.saveState.deathPersistentSaveData.karma != 10)
            {
                int karma = self.saveState.deathPersistentSaveData.karma;

                float baseLungAirConsumption = 1.0f;
                float reducePerKarma = 0.07f;
                float newLungCapacity = baseLungAirConsumption - (reducePerKarma * (karma + 1));

                self.characterStats.lungsFac = newLungCapacity;

            }
            else
                self.characterStats.lungsFac = 0.2f;
        }
    }
}
