using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

internal static class СanMaul
{
    public static void Hook()
    {
        On.Player.CanMaulCreature += CanMaulCreatureHook;
    }

    private static bool CanMaulCreatureHook(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (crit is Player && !crit.dead && self != null && self.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            return true; 
        }
        return orig(self,crit);
    }
}
