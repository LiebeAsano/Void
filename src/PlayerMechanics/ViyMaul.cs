using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

internal static class ViyMaul
{
    public static void Hook()
    {
        On.Player.CanMaulCreature += Player_CanMaulCreature;
    }

    private static bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        bool critStun = !crit.Stunned || crit.Stunned;
        if (crit != null && !crit.dead && (crit is not IPlayerEdible || crit is Centipede) && critStun)
        {
            crit.Stun(20);
            return true;
        }
        return orig(self, crit);
    }
}