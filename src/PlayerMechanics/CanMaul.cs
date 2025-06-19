using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

public static class СanMaul
{
    public static void Hook()
    {
        On.Player.CanMaulCreature += CanMaulCreatureHook;
        IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    private static bool CanMaulCreatureHook(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (crit is Player && !crit.dead && self != null && (self.slugcatStats.name == VoidEnums.SlugcatID.Void || self.slugcatStats.name == VoidEnums.SlugcatID.Viy))
        {
            return true;
        }
        return orig(self, crit);
    }

    private static void Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        if (c.TryGotoNext(
        MoveType.After,
        x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod("Violence"))))
        {

            c.Emit(OpCodes.Ldarg_0);

            c.EmitDelegate<Action<Creature>>(creature =>
            {
                Array.ForEach(creature.grasps, grasp =>
                {
                    if (grasp != null
                        && grasp.grabbed is Player playerInGrasp
                        && (playerInGrasp.IsVoid() || playerInGrasp.IsViy())
                        && creature is Player player 
                        && player.slugcatStats.name != VoidEnums.SlugcatID.Void && player.slugcatStats.name != VoidEnums.SlugcatID.Viy)
                    {
                        creature.Stun(TicksPerSecond * 5);
                    }
                });
            });
        }
        else
        {
            LogExErr("Failed to find the call to Player_GrabUpdate injection point not found.");
        }
    }
}