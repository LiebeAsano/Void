using UnityEngine;
using VoidTemplate.OptionInterface;

namespace VoidTemplate.PlayerMechanics.HunterMechanics;

public static class HunterCanIPickThisUp
{
    public static void Hook()
    {
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
    }

    private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (OptionAccessors.BuffHunter && self.slugcatStats.name == SlugcatStats.Name.Red && obj is Spear spear)
        {
            if (spear.mode == Weapon.Mode.StuckInWall && (!ModManager.MSC || !spear.abstractSpear.electric))
            {
                foreach (var grasp in self.grasps)
                {
                    if (grasp?.grabbed != null)
                    {
                        if (!self.CanPutSpearToBack && self.Grabability(grasp.grabbed) >= Player.ObjectGrabability.BigOneHand)
                            return orig(self, obj);
                        if (self.CanPutSpearToBack && self.input[0].pckp && !self.input[1].pckp)
                        {
                            if (spear.hasHorizontalBeamState)
                            {
                                spear.resetHorizontalBeamState();
                                spear.stuckInWall = new Vector2?(default);
                                spear.vibrate = 20;
                                spear.firstChunk.collideWithTerrain = true;
                                spear.abstractSpear.stuckInWallCycles = 0;
                            }
                        }
                    }
                }
                return true;
            }
        }
        return orig(self, obj);
    }
}
