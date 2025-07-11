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

public static class CanMaul
{
    private const int StunDuration = 5;

    public static void Hook()
    {
        On.Player.CanMaulCreature += CanMaulCreatureHook;
        IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    private static bool CanMaulCreatureHook(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (self == null || crit == null || crit.dead)
            return orig(self, crit);

        return crit is Player && self.AreVoidViy() || orig(self, crit);
    }

    private static void Player_GrabUpdate(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod("Violence"))))
            {
                LogExErr("Player_GrabUpdate injection point not found.");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Creature>>(creature =>
            {
                if (creature?.grasps == null) return;

                foreach (var grasp in creature.grasps)
                {
                    if (grasp?.grabbed is not Player playerInGrasp ||
                        !playerInGrasp.AreVoidViy())
                        continue;

                    if (creature is Player attackingPlayer &&
                        !attackingPlayer.AreVoidViy())
                    {
                        creature.Stun(StunDuration * TicksPerSecond);
                        break;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            LogExErr($"Error in Player_GrabUpdate IL hook: {ex}");
        }
    }
}