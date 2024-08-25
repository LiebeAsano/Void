using MonoMod.Cil;
using static VoidTemplate.Useful.Utils;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Cecil.Cil;
using static Creature;

namespace VoidTemplate.PlayerMechanics
{
    internal static class HealthSpear
    {
        public static void Hook()
        {
            IL.Spear.HitSomething += Spear_HitSomething;
        }

        public static void Spear_HitSomething(ILContext il)
        {
            ILCursor c = new(il);;
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<global::PlayerState>(nameof(global::PlayerState.permanentDamageTracking))))
            {
                c.Emit(OpCodes.Ldloc_3);
                c.EmitDelegate((double orig, Player p) =>
                {
                    if (p.IsVoid() && p.KarmaCap == 10)
                        return 1.25;
                    else
                        return orig;
                }); 
            }
            else
                logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(HealthSpear)}.{nameof(Spear_HitSomething)}: first match failed");
        }
    }
}
