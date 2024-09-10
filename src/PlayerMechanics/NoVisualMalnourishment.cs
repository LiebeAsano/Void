using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

internal static class NoVisualMalnourishment
{
    public static void Startup()
    {
        On.PlayerGraphics.ctor += static (orig, self, arg) =>
        {
            orig(self, arg);
            self.malnourished = 0f;
        };
    }
}
