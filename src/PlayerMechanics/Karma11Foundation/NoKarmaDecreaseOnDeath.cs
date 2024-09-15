using MonoMod.Cil;
using Mono.Cecil.Cil;
using static VoidTemplate.Useful.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class NoKarmaDecreaseOnDeath
{
    public static void Initiate()
    {
        IL.DeathPersistentSaveData.SaveToString += DeathPersistentSaveData_SaveToString;
    }

    private static void DeathPersistentSaveData_SaveToString(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new(il);
        ILLabel skipSubtract = c.DefineLabel();

        //rain world saves karma-1 to disk and then loads it back, so we are intercepting it here, at last moment
        if(c.TryGotoNext(MoveType.After, x => x.MatchLdstr("REINFORCEDKARMA<dpB>0<dpA>"))
            && c.TryGotoNext(MoveType.Before, x => x.MatchLdcI4(1)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((DeathPersistentSaveData saveData) =>
            {
                return saveData.karma == 10;
            });
            c.Emit(OpCodes.Brtrue_S, skipSubtract);
            c.Index++;
            c.MarkLabel(skipSubtract);
            LogExInf("injection success");
        }
        else
        {
            LogExErr("failed to find karma decrease in saving data. The karma will go down if you die on K11");
        }
    }
}
