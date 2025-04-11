using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

internal static class RottenDangleResist
{
    public static void Hook()
    {
        On.Player.ObjectEaten += Player_ObjectEaten;
    }

    private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as PlayerGraphics).LookAtNothing();
            }
            if (edible is DangleFruit && (edible as DangleFruit).AbstrConsumable.rotted)
            {
                self.AddQuarterFood();
                self.AddQuarterFood();
                return;
            }
        }
        orig(self, edible);
    }
}
