using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class ExtendedLungs
{
    public static void Hook()
    {
        On.StoryGameSession.AddPlayer += StoryGameSession_AddPlayer;
    }
    private static void StoryGameSession_AddPlayer(On.StoryGameSession.orig_AddPlayer orig, StoryGameSession self, AbstractCreature abstractCreature)
    {
        orig(self, abstractCreature);
        if (abstractCreature.realizedCreature is Player player
            && player.IsVoid())
        {
            if (player.KarmaCap != 10)
            {
                int karma = player.Karma;

                float baseLungAirConsumption = 1.0f;
                float reducePerKarma = 0.06f;
                float newLungCapacity = baseLungAirConsumption - (reducePerKarma * (karma + 1));

                player.slugcatStats.lungsFac = newLungCapacity;
            }
            else
                player.slugcatStats.lungsFac = 0.15f;
        }
    }
}
