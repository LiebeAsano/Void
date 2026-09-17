using System;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.RainCycleChanges.PostRainCycle;

namespace VoidTemplate.RainCycleChanges;

public static class RainFoodRequirement
{
    private static readonly ConditionalWeakTable<SlugcatStats, RequirementState> states = new();

    private sealed class RequirementState
    {
        public int normal;
        public int applied;
        public bool active;
    }

    public static void Hook()
    {
        On.Player.Update += Player_Update;
        On.ShelterDoor.Close += ShelterDoor_Close;
        On.GlobalRain.ResetRain += GlobalRain_ResetRain;
        On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
        IL.ShelterDoor.Update += ShelterDoor_Update;
        IL.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
        IL.SaveState.SessionEnded += SaveState_SessionEnded;
    }

    public static int GetNormalFoodToHibernate(SlugcatStats stats)
    {
        if (stats == null)
            return 0;

        if (stats.malnourished || stats.malnourishedByCreature)
            return stats.maxFood;

        int required = stats.foodToHibernate;

        if (states.TryGetValue(stats, out RequirementState state)
            && state.active
            && required == state.applied)
            required = state.normal;

        return Mathf.Clamp(required, 0, stats.maxFood);
    }

    private static SlugcatStats GetCurrentStats(RainWorldGame game, StoryGameSession session)
    {
        AbstractCreature absPlayer = game.FirstAlivePlayer ?? game.FirstAnyPlayer;

        if (absPlayer?.realizedCreature is Player player)
            return player.slugcatStats;

        if (absPlayer?.state is PlayerState playerState
            && session.characterStatsJollyplayer != null
            && playerState.playerNumber >= 0
            && playerState.playerNumber < session.characterStatsJollyplayer.Length
            && session.characterStatsJollyplayer[playerState.playerNumber] != null)
            return session.characterStatsJollyplayer[playerState.playerNumber];

        return session.characterStats;
    }

    public static bool TryGetRemaining(RainWorldGame game, out int required)
    {
        required = 0;

        if (game?.session is not StoryGameSession session
            || session.characterStats == null
            || game.world?.rainCycle == null
            || (!game.IsVoidWorld() && !game.IsViyWorld()))
            return false;

        RainCycleExt cycle = game.world.rainCycle.GetRainCycleExt();

        if (!cycle.PostCycleStarted
            || !cycle.foodPlanInitialized
            || cycle.postCycleFailed)
            return false;

        SlugcatStats stats = GetCurrentStats(game, session);
        int maxFood = stats?.maxFood ?? session.characterStats.maxFood;

        required = cycle.starvationRequirement
            ? maxFood
            : cycle.RemainingFoodCost;

        required = Mathf.Clamp(required, 0, maxFood);

        return true;
    }

    public static void Refresh(RainWorldGame game)
    {
        if (game?.session is not StoryGameSession session)
            return;

        if (!TryGetRemaining(game, out int required))
        {
            Restore(game);
            return;
        }

        Apply(session.characterStats, required);

        if (session.characterStatsJollyplayer != null)
        {
            for (int i = 0; i < session.characterStatsJollyplayer.Length; i++)
                Apply(session.characterStatsJollyplayer[i], required);
        }

        for (int i = 0; i < game.Players.Count; i++)
        {
            if (game.Players[i]?.realizedCreature is Player player)
                Apply(player.slugcatStats, required);
        }
    }

    public static void Restore(RainWorldGame game)
    {
        if (game?.session is not StoryGameSession session)
            return;

        Restore(session.characterStats);

        if (session.characterStatsJollyplayer != null)
        {
            for (int i = 0; i < session.characterStatsJollyplayer.Length; i++)
                Restore(session.characterStatsJollyplayer[i]);
        }

        for (int i = 0; i < game.Players.Count; i++)
        {
            if (game.Players[i]?.realizedCreature is Player player)
                Restore(player.slugcatStats);
        }
    }

    private static void Apply(SlugcatStats stats, int required)
    {
        if (stats == null) return;

        RequirementState state = states.GetValue(stats, _ => new RequirementState());

        if (!state.active || stats.foodToHibernate != state.applied)
            state.normal = stats.foodToHibernate;

        state.applied = Mathf.Clamp(required, 0, stats.maxFood);
        state.active = true;
        stats.foodToHibernate = state.applied;
    }

    private static void Restore(SlugcatStats stats)
    {
        if (stats == null
            || !states.TryGetValue(stats, out RequirementState state)
            || !state.active)
            return;

        if (stats.foodToHibernate == state.applied)
        {
            if (stats.malnourished || stats.malnourishedByCreature)
                stats.foodToHibernate = stats.maxFood;
            else
                stats.foodToHibernate = state.normal;
        }

        state.active = false;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        RainWorldGame game = self.abstractCreature?.world?.game;

        Refresh(game);
        orig(self, eu);
        Refresh(self.abstractCreature?.world?.game);
    }

    private static void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        Refresh(self.room?.game);
        orig(self);
    }

    private static void GlobalRain_ResetRain(On.GlobalRain.orig_ResetRain orig, GlobalRain self)
    {
        Restore(self.game);
        orig(self);
    }

    private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
    {
        RainWorldGame game = self.activeWorld?.game;

        Restore(game);
        orig(self, warpUsed);
        Refresh(self.activeWorld?.game);
    }

    private static int RemainingOrOriginal(int original, RainWorldGame game)
    {
        if (RainFoodSleep.TryGetPayment(game, out int payment))
            return payment;

        return TryGetRemaining(game, out int required)
            ? required
            : original;
    }

    private static int MinimumFoodForShelter(int original, ShelterDoor door)
    {
        RainWorldGame game = door.room?.game;

        if (!TryGetRemaining(game, out int required))
            return original;

        RainCycleExt cycle = game.world.rainCycle.GetRainCycleExt();

        if (cycle.starvationRequirement)
            return required;

        return Mathf.Min(original, required);
    }

    private static void ShelterDoor_Update(ILContext il)
    {
        ILCursor c = new(il);
        int patched = 0;

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<ShelterDoor>>(door => Refresh(door.room?.game));

        while (c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<SlugcatStats>(nameof(SlugcatStats.maxFood))))
        {
            Instruction comparison = c.Next;

            for (int i = 0; i < 8 && comparison != null; i++)
            {
                if (comparison.OpCode == OpCodes.Nop)
                {
                    comparison = comparison.Next;
                    continue;
                }

                if (comparison.OpCode == OpCodes.Br || comparison.OpCode == OpCodes.Br_S)
                {
                    comparison = comparison.Operand is ILLabel label
                        ? label.Target
                        : comparison.Operand as Instruction;

                    continue;
                }

                break;
            }

            if (comparison == null || !IsFoodComparison(comparison))
                continue;

            c.Goto(comparison);
            c.MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, ShelterDoor, int>>(MinimumFoodForShelter);

            patched++;
        }

        if (patched == 0)
            Utils.LogExErr("RainFoodRequirement: ShelterDoor.Update minimum-food comparison not found.");
    }

    private static bool IsFoodComparison(Instruction instruction)
    {
        return instruction.OpCode == OpCodes.Bge
            || instruction.OpCode == OpCodes.Bge_S
            || instruction.OpCode == OpCodes.Bge_Un
            || instruction.OpCode == OpCodes.Bge_Un_S
            || instruction.OpCode == OpCodes.Blt
            || instruction.OpCode == OpCodes.Blt_S
            || instruction.OpCode == OpCodes.Blt_Un
            || instruction.OpCode == OpCodes.Blt_Un_S;
    }

    private static bool ShelterMalnourished(bool original, ShelterDoor door)
    {
        RainWorldGame game = door.room?.game;

        if (!TryGetRemaining(game, out int required))
            return original;

        int food = 0;

        if (ModManager.CoopAvailable)
        {
            for (int i = 0; i < door.room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < door.room.physicalObjects[i].Count; j++)
                {
                    if (door.room.physicalObjects[i][j] is Player player)
                        food = Mathf.Max(food, player.FoodInRoom(door.room, false));
                }
            }
        }
        else
        {
            if (game.Players.Count == 0
                || game.Players[0]?.realizedCreature is not Player player)
                return original;

            food = player.FoodInRoom(door.room, false);
        }

        Refresh(game);

        return food < required;
    }

    private static void ShelterDoor_DoorClosed(ILContext il)
    {
        ILCursor c = new(il);
        int patched = 0;

        while (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdcI4(0),
            x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.Win))))
        {
            c.MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, ShelterDoor, bool>>(ShelterMalnourished);

            c.Index += 2;
            patched++;
        }

        if (patched != 2)
            Utils.LogExErr("RainFoodRequirement: expected 2 ShelterDoor.DoorClosed Win calls, found " + patched + ".");
    }

    private static void SaveState_SessionEnded(ILContext il)
    {
        ILCursor c = new(il);
        int patched = 0;

        while (c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<SlugcatStats>(nameof(SlugcatStats.foodToHibernate))))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<int, RainWorldGame, int>>(RemainingOrOriginal);

            patched++;
        }

        if (patched == 0)
            Utils.LogExErr("RainFoodRequirement: SaveState.SessionEnded food deduction not found.");
    }
}