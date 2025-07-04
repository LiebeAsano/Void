using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class CreatureReleaseGrasp
{
    public static void Hook()
    {
        On.Creature.ReleaseGrasp += Creature_ReleaseGrasp;
    }

    private static void Creature_ReleaseGrasp(On.Creature.orig_ReleaseGrasp orig, Creature self, int grasp)
    {
        if (self is Player player && !player.AreVoidViy())
        {
            orig(self, grasp);
            return;
        }
        if (grasp >= 0 && grasp < self.grasps.Length && self.grasps[grasp] != null)
        {
            self.grasps[grasp].Release();
        }
    }
}
