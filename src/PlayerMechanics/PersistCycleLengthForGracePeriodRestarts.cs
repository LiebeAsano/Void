using SlugBase.SaveData;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics
{
	public static class PersistCycleLengthForGracePeriodRestarts
	{
		public const string PERSISTED_CYCLE_LENGTH_SLUGBASE_KEY = "theVoidpersistedCycleLength";

		public static void Hook()
		{
			On.RainWorldGame.ExitGame += RainWorldGame_ExitGame;
			On.RainCycle.ctor += RainCycle_ctor;
		}

		private static void RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
		{
			orig(self, world, minutes);

			if (world.game.StoryCharacter == VoidEnums.SlugcatID.Void || world.game.StoryCharacter == VoidEnums.SlugcatID.Viy)
			{
				SlugBaseSaveData slugBaseSaveData = world.game.rainWorld.progression?.currentSaveState?.deathPersistentSaveData?.GetSlugBaseData();
				if (slugBaseSaveData != null)
				{
					if (slugBaseSaveData.TryGet(PERSISTED_CYCLE_LENGTH_SLUGBASE_KEY, out int cycleLength))
					{
						loginf($"Grace period restart cycle length of {cycleLength} restored");

						slugBaseSaveData.Remove(PERSISTED_CYCLE_LENGTH_SLUGBASE_KEY);
						self.cycleLength = cycleLength;
					}
				}
			}

		}

		private static void RainWorldGame_ExitGame(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
		{
			loginf($"Called Exit Game with asDeath: {asDeath}, asQuit: {asQuit}, clock: {self.clock}");

			if (self.clock <= 1200 && (self.StoryCharacter == VoidEnums.SlugcatID.Void || self.StoryCharacter == VoidEnums.SlugcatID.Viy))
			{
				SlugBaseSaveData slugBaseSaveData = self.manager.rainWorld.progression?.currentSaveState?.deathPersistentSaveData?.GetSlugBaseData();
				if (slugBaseSaveData != null)
				{
					loginf($"Grace period restart cycle length of {self.world.rainCycle.cycleLength} saved");

					slugBaseSaveData.Set(PERSISTED_CYCLE_LENGTH_SLUGBASE_KEY, self.world.rainCycle.cycleLength);
				}
			}

			orig(self, asDeath, asQuit);
		}

	}
}
