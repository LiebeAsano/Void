using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.CreatureInteractions;

public static class AntiSpiderStun
{
    public static void Hook()
    {
        IL.DartMaggot.Update += DartMaggot_Update;
    }

    private static void DartMaggot_Update(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new(il);
        if(c.TryGotoNext(MoveType.After, 
            x => x.MatchMul(), 
            x => x.MatchAdd(),
            x => x.MatchMul()))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, DartMaggot, int>>((int orig, DartMaggot maggot) =>
            {
                if(maggot.stuckInChunk.owner is Player p && p.IsVoid())
                {
                    var karma = p.Karma;
                    return (int)((float)orig * (1f - 0.09f * karma));
                }
                return orig;
            });
        }    
    }
}
