using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class ViyMaul
{
    public static void Hook()
    {
        //On.Player.CanMaulCreature += Player_CanMaulCreature;
        On.Player.Grabability += Player_Grabability;
    }

    private static bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (Utils.IsViy(self.room.game.GetStorySession.saveState))
        {
            bool critStun = !crit.Stunned || crit.Stunned;
            if (crit != null && !crit.dead && ((crit is not IPlayerEdible) || crit is Centipede && !(crit as Centipede).Edible) && critStun)
            {
                crit.Stun(10);
                return true;
            }
        }
        return orig(self, crit);
    }

    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (Utils.IsViy(self.room.game.GetStorySession.saveState))
        {
            if (obj is Player
                || obj is Fly
                || obj is Hazer
                || obj is PoleMimic
                || obj is Snail
                || obj is Centipede && (obj as Centipede).Edible
                || obj is LanternMouse
                || obj is EggBug
                || obj is FlyLure
                || obj is SmallNeedleWorm
                || obj is TentaclePlant
                || obj is VultureGrub
                || obj is Spider
                || obj is GarbageWorm
                || obj is Leech
                || obj is Deer
                || obj is JellyFish
                || obj is Vulture)
            {
                return orig(self, obj);
            }
            else
            {
                if (obj is BigJellyFish or Yeek or Cicada or JetFish)
                {
                    return Player.ObjectGrabability.TwoHands;
                }
                else
                {
                    if (obj is Creature)
                    {
                        return Player.ObjectGrabability.Drag;
                    }
                    else
                    {
                        return orig(self, obj);
                    }
                }

            }
        }
        return orig(self, obj);
    }
}