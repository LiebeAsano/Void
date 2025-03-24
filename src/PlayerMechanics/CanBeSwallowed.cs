using IL;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class CanBeSwallowed
{
	public static void Hook()
	{
		On.Player.CanBeSwallowed += Player_CanBeSwallowed;
	}

	private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
	{
		if (self.IsVoid())
		{
			return testObj is not Creature && testObj is not Spear && testObj is not VultureMask || testObj is NeedleEgg || orig(self, testObj);
		}
		return orig(self, testObj);
	}
}
