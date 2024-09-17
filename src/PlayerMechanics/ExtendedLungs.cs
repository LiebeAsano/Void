using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class ExtendedLungs
{
    public static void Hook()
    {
        On.Player.Update += Player_Update;
    }
    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.IsVoid())
        {
            if (self.KarmaCap != 10)
            {
                int karma = self.Karma;

                float baseLungAirConsumption = 1.0f;
                float reducePerKarma = 0.07f;
                float newLungCapacity = baseLungAirConsumption - (reducePerKarma * (karma + 1));

                self.slugcatStats.lungsFac = newLungCapacity;

            }
            else
                self.slugcatStats.lungsFac = 0.2f;
        }
    }
}
