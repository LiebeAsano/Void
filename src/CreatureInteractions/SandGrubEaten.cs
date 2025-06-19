using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions
{
    public static class SandGrubEaten
    {
        public static void Hook()
        {
            On.Watcher.SandGrub.Bury += SandGrub_Bury;
        }

        private static void SandGrub_Bury(On.Watcher.SandGrub.orig_Bury orig, Watcher.SandGrub self, BodyChunk chunk, bool eu)
        {
            orig(self, chunk, eu);
            if (self.buryCounter > 40 && chunk.owner.grabbedBy.Count == 1)
            {
                Vector2 vector = self.burrow.pos - self.burrow.dir * (chunk.rad * 2f + 10f);
                if (self.buryCounter > 80 && Custom.DistLess(chunk.pos, vector, 5f))
                {
                    if (chunk.owner is Player player && player.IsVoid())
                    {
                        self.Die();
                    }
                }
            }
        }
    }
}
