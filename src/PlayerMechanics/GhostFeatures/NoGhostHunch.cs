using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

internal static class NoGhostHunch
{
    public static void Hook()
    {
        On.Room.Loaded += Room_Loaded;
    }
#nullable enable
    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        orig(self);
        GhostHunch? hunch = null;
        if (self.updateList.Exists(x =>
        {
            if (x is GhostHunch h)
            {
                hunch = h;
                return true;
            }
            return false;
        }))
        {
            self.updateList.Remove(hunch);
        }
    }
}
