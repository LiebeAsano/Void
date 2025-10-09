using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

public static class NoForceSleep
{
	public static void Hook()
	{
		On.Player.Update += NoForceSleep_Update;
	}

    private static void NoForceSleep_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (self.IsVoid() &&
            self.abstractCreature?.world?.game?.GetStorySession?.saveState?.GetVoidMarkV3() == false)
        {
            self.forceSleepCounter = 0;
        }
    }
}
