using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class PlayerGrabbed
{
    public static void Hook()
    {
        //On.Player.Grabbed += Player_Grabbed;
    }

    private static void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
    {
        orig(self, grasp);
        if (grasp.grabbed is Player player && player.AreVoidViy())
        {
            self.dangerGraspTime = 0;
            self.dangerGrasp = grasp;
        }
    }
}
