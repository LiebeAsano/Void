using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class Sandstorm
{
    public static void Hook()
    {
        On.Watcher.Sandstorm.AffectObjects += Sandstorm_AffectObjects;
    }

    private static void Sandstorm_AffectObjects(On.Watcher.Sandstorm.orig_AffectObjects orig, Watcher.Sandstorm self, float amount)
    {
        List<PhysicalObject>[] physicalObjects = self.room.physicalObjects;
        for (int i = 0; i < physicalObjects.Length; i++)
        {
            foreach (PhysicalObject physicalObject in physicalObjects[i])
            {
                if (physicalObject != null && physicalObject is Player player && player.AreVoidViy())
                    return;
            }
        }
        orig(self, amount);
    }
}
