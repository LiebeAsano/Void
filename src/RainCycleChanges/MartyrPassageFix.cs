using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class MartyrPassageFix
{

    public static void Hook()
    {
        IL.WinState.CycleCompleted += WinState_CycleCompleted;
        On.SaveState.SessionEnded += SaveState_SessionEnded;
    }

    private static void WinState_CycleCompleted(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            x => x.MatchLdcR4(0.27f)))
        {
            Utils.LogExErr("MartyrPassageFix: Martyr +0.27 value not found.");
            return;
        }

        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate(MartyrGain);
    }

    private static float MartyrGain(float original, RainWorldGame game)
    {
        if (game?.session is not StoryGameSession session
            || game.StoryCharacter != VoidEnums.SlugcatID.Void)
            return original;

        SaveState save = session.saveState;

        if (save.malnourished && save.lastMalnourished)
            return 0f;

        return original;
    }

    private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        if (survived
            && newMalnourished
            && self.malnourished
            && game != null
            && game.IsVoidStoryCampaign()
            && RecoveredFromStarvation(game))
        {
            newMalnourished = false;
        }

        orig(self, game, survived, newMalnourished);
    }

    private static bool RecoveredFromStarvation(RainWorldGame game)
    {
        AbstractCreature absPlayer = game.FirstAlivePlayer ?? game.FirstAnyPlayer;

        if (absPlayer?.realizedCreature is not Player player
            || !player.playerState.alive
            || player.Malnourished)
            return false;

        int food = player.room != null
            ? player.FoodInRoom(player.room, false)
            : player.FoodInStomach;

        return food >= player.MaxFoodInStomach;
    }
}