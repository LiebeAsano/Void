using System;
using System.Linq;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class CanIPickThisUp
{
	public static void Hook()
	{
		On.Player.CanIPickThisUp += Player_CanIPickThisUp;
		On.Player.CanIPickThisUp += Player_CanIPickThisSpear;
		On.Player.SlugOnBack.SlugToBack += Player_SlugToBack;
	}

    private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
	{
        var result = orig(self, obj);
        if (self.slugcatStats.name != VoidEnums.SlugcatID.Void) return result;
        int heavyObjectsCount = 0;
        foreach (var grasp in self.grasps) if (grasp?.grabbed != null && self.Grabability(grasp.grabbed) == Player.ObjectGrabability.Drag) heavyObjectsCount++;
        if (heavyObjectsCount == 1 && obj is Spear) return true;
        int amountOfSpearsInHands = self.grasps.Aggregate(func: (int acc, Creature.Grasp grasp) => acc + ((grasp?.grabbed is Spear) ? 1 : 0), seed: 0);
		if (amountOfSpearsInHands == 1 && self.Grabability(obj) == Player.ObjectGrabability.Drag) return true;
        return result;
	}

	public static bool Player_CanIPickThisSpear(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
	{
		if (self.IsVoid())
		{
			bool canPick = true;
			foreach (var grasp in self.grasps)
			{
				if (grasp != null && self.Grabability(grasp.grabbed) >= Player.ObjectGrabability.BigOneHand)
				{
					canPick = false;
					break;
				}
			}
			if (obj is Spear spear && spear.mode == Weapon.Mode.StuckInWall &&
				(!ModManager.MSC || !spear.abstractSpear.electric) && canPick)
				return true;
		}
		return orig(self, obj);
	}
    private static void Player_SlugToBack(On.Player.SlugOnBack.orig_SlugToBack orig, Player.SlugOnBack self, Player player)
    {
        if (self.slugcat != null)
        {
			return;
        }

        if (self.owner.slugcatStats.name == VoidEnums.SlugcatID.Void)
		{
            return;
        }

        if (player.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            return;
        }

        orig(self, player);
    }
}
