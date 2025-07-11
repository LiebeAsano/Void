using IL;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class CanBeSwallowed
{
    public static void Hook() => On.Player.CanBeSwallowed += Player_CanBeSwallowed;

    private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        return self.IsVoid() 
            ? !(testObj is Creature or Spear or VultureMask) || orig(self, testObj)
            : orig(self, testObj);
    }
}
