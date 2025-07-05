using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class CreatureGrab
{
    public static void Hook()
    {
        //On.Creature.Grab += Creature_Grab;
    }

    private static bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (self is Player player && !player.AreVoidViy())
        {
            return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
        }
        if (self.grasps == null || graspUsed < 0 || graspUsed > self.grasps.Length)
        {
            return false;
        }
        if (obj.abstractPhysicalObject.rippleLayer != self.abstractCreature.rippleLayer && !obj.abstractPhysicalObject.rippleBothSides && !self.abstractCreature.rippleBothSides)
        {
            return false;
        }
        if (obj.slatedForDeletetion || (obj is Creature && !(obj as Creature).CanBeGrabbed(self)))
        {
            return false;
        }
        if (self.grasps[graspUsed] != null && self.grasps[graspUsed].grabbed == obj)
        {
            self.ReleaseGrasp(graspUsed);
            self.grasps[graspUsed] = new Creature.Grasp(self, obj, graspUsed, chunkGrabbed, shareability, dominance, true);
            obj.Grabbed(self.grasps[graspUsed]);
            new AbstractPhysicalObject.CreatureGripStick(self.abstractCreature, obj.abstractPhysicalObject, graspUsed, pacifying || obj.TotalMass < self.TotalMass);
            return true;
        }
        foreach (Creature.Grasp grasp in obj.grabbedBy)
        {
            if (grasp.grabber == self || (grasp.ShareabilityConflict(shareability) && ((overrideEquallyDominant && grasp.dominance == dominance) || grasp.dominance > dominance)))
            {
                return false;
            }
        }
        for (int i = obj.grabbedBy.Count - 1; i >= 0; i--)
        {
            if (obj.grabbedBy[i].ShareabilityConflict(shareability))
            {
                obj.grabbedBy[i].Release();
            }
        }
        if (self.grasps[graspUsed] != null)
        {
            self.ReleaseGrasp(graspUsed);
        }
        self.grasps[graspUsed] = new Creature.Grasp(self, obj, graspUsed, chunkGrabbed, shareability, dominance, pacifying);
        obj.Grabbed(self.grasps[graspUsed]);
        new AbstractPhysicalObject.CreatureGripStick(self.abstractCreature, obj.abstractPhysicalObject, graspUsed, pacifying || obj.TotalMass < self.TotalMass);
        
        return true;
    }
}
