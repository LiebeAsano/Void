using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class PebblesCycleRollback
{
    public static void Hook()
    {
        IL.SSOracleBehavior.SSOracleMeetWhite.ctor += SkipRollback;
        IL.SSOracleBehavior.SSOracleMeetYellow.ctor += SkipRollback;
        IL.SSOracleBehavior.SSOracleMeetGourmand.ctor += SkipRollback;
    }

    private static void SkipRollback(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<MiscWorldSaveData>(nameof(MiscWorldSaveData.memoryArraysFrolicked))))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate((bool frolicked, SSOracleBehavior owner) => frolicked && !PostRainCycle.HasRainCycle(owner.oracle.room.game));
        }
        else Utils.LogExErr($"PebblesCycleRollback: rollback not found in {il.Method.DeclaringType.Name}");
    }
}
