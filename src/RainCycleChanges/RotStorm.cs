using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class RotStorm
{
    public static void Hook()
    {
        IL.RainCycle.Update += RainCycle_Update;
    }

    private static void RainCycle_Update(ILContext il)
    {
        ILCursor c = new(il);
        bool found = false;

        while (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("RM"), x => x.MatchCall<string>("op_Equality")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool rot, RainCycle self) => rot && !PostRainCycle.HasRainCycle(self.world.game));
            found = true;
        }

        if (!found) Utils.LogExErr("RotStorm: RM rain exception not found");
    }
}
