using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class NoForceSleep
{
    public static void Hook()
    {
        IL.Player.Update += Player_Update;
    }

    private static void Player_Update(ILContext il)
    {
        ILCursor c = new(il);

        int patched = 0;

        while (c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdfld<Player>(nameof(Player.forceSleepCounter)),
            x => x.MatchLdcI4(1),
            x => x.MatchAdd()))
        {
            if (c.Next == null || !c.Next.MatchStfld<Player>(nameof(Player.forceSleepCounter)))
                continue; 

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(FilterForceSleepCounter);

            patched++;
        }

        if (patched != 2) Utils.Logerr($"{nameof(NoForceSleep)}: expected 2 forceSleepCounter increments, patched {patched}");
        else Utils.Loginf($"{nameof(NoForceSleep)}: patched both forceSleepCounter increments");
        
    }

    private static int FilterForceSleepCounter(int value, Player self)
    {
        if (!self.IsVoid())
            return value;

        if (self.abstractCreature?.world?.game?.session is not StoryGameSession story)
            return value;

        if (story.saveState.GetVoidMarkV3())
            return value;

        return 0;
    }
}