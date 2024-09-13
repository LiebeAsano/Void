using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class NoVisualMalnourishment
{
    public static void Hook()
    {
        On.PlayerGraphics.ctor += PlayerGraphics_ctor; 
    }

    private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);
        if (self.player.IsVoid())
            self.malnourished = 0f;
    }
}
