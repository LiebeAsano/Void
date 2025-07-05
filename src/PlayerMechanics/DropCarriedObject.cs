using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class DropCarriedObject
{
    public static void Hook()
    {
        On.AbstractCreature.DropCarriedObject += AbstractCreature_DropCarriedObject;
    }

    private static void AbstractCreature_DropCarriedObject(On.AbstractCreature.orig_DropCarriedObject orig, AbstractCreature self, int graspIndex)
    {
        if (self.realizedCreature is Player player && !player.AreVoidViy())
        {
            orig(self, graspIndex);
            return;
        }

        foreach (var objectStick in self.stuckObjects.ToArray())
        {
            if (objectStick is AbstractPhysicalObject.CreatureGripStick &&
                objectStick.A == self &&
                (objectStick as AbstractPhysicalObject.CreatureGripStick).grasp == graspIndex)
            {
                objectStick.Deactivate();
            }
        }

        if (self.realizedCreature != null &&
            self.realizedCreature.grasps != null &&
            graspIndex >= 0 &&
            graspIndex < self.realizedCreature.grasps.Length &&
            self.realizedCreature.grasps[graspIndex] != null)
        {
            self.realizedCreature.ReleaseGrasp(graspIndex);
        }
    }
}
