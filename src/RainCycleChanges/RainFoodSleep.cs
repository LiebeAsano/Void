using System.Runtime.CompilerServices;
using HUD;
using Menu;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.RainCycleChanges.PostRainCycle;

namespace VoidTemplate.RainCycleChanges;

public static class RainFoodSleep
{
    private const int ReturnTicks = 50;

    private static readonly ConditionalWeakTable<SlugcatStats, SleepData> pending = new();
    private static readonly ConditionalWeakTable<RainWorldGame, SleepData> payments = new();
    private static readonly ConditionalWeakTable<SleepAndDeathScreen, SleepData> screens = new();
    private static readonly ConditionalWeakTable<FoodMeter, MeterState> meters = new();

    public sealed class SleepData
    {
        internal SaveState save;
        internal SlugcatStats stats;
        internal int completedCycle;
        internal int oldMax;
        internal int oldNormal;
        internal int oldGold;
        internal int oldExtra;
        internal int oldGrowth;
        internal int payment;
        internal int nextMax;
        internal int nextNormal;
        internal int nextGold;
        internal float marker;
        internal bool karma11;
        public bool CompleteFullFoodPlan { get; internal set; }
    }

    private enum Stage
    {
        Countdown,
        WaitForPips,
        Returning,
        Done
    }

    private sealed class MeterState
    {
        public SleepData data;
        public Stage stage;
        public float shown;
        public float lastShown;
        public float from;
        public int counter;
        public bool initialized;
    }

    public static bool TryBegin(SaveState save, RainWorldGame game, bool survived, bool newMalnourished, out SleepData data)
    {
        data = null;

        if (!survived || newMalnourished || save == null
            || save.sessionEndingFromSpinningTopEncounter
            || !RainFoodRequirement.TryGetRemaining(game, out int remaining))
            return false;

        StoryGameSession session = game.GetStorySession;
        RainCycleExt cycle = game.world.rainCycle.GetRainCycleExt();
        Player player = (game.FirstAlivePlayer ?? game.FirstAnyPlayer)?.realizedCreature as Player;
        int available = player == null ? 0 : player.FoodInStomach;

        if (player?.room != null)
            available = Mathf.Max(available, player.FoodInRoom(player.room, false));

        data = new SleepData
        {
            save = save,
            stats = session.characterStats,
            oldMax = session.characterStats.maxFood,
            oldNormal = RainFoodRequirement.GetNormalFoodToHibernate(session.characterStats),
            oldGold = 2 * save.GetVoidFoodToHibernate(),
            oldExtra = save.GetVoidExtraFood(),
            oldGrowth = save.GetVoidFoodToHibernate(),
            payment = remaining,
            marker = remaining,
            karma11 = save.saveStateNumber == VoidEnums.SlugcatID.Void
                && save.deathPersistentSaveData.karmaCap == 10,
            CompleteFullFoodPlan = cycle.fullFoodChangeCycle
                && available >= remaining && player != null && !player.Malnourished
        };

        if (game.cameras != null)
        {
            for (int i = 0; i < game.cameras.Length; i++)
            {
                if (game.cameras[i].hud?.foodMeter is not FoodMeter meter
                    || meter.IsPupFoodMeter || meter.hud.owner is not Player)
                    continue;

                data.marker = Mathf.Clamp(meter.ShowSurvivalLimit, 0f, data.oldMax);
                break;
            }
        }

        payments.Remove(game);
        payments.Add(game, data);
        return true;
    }

    public static void PrepareProgression(SleepData data, bool fullStomach)
    {
        if (data == null) return;

        bool grew = data.save.GetVoidExtraFood() > data.oldExtra
            || data.save.GetVoidFoodToHibernate() > data.oldGrowth;

        if (fullStomach && grew)
            data.payment = data.oldMax;
    }

    public static bool TryGetPayment(RainWorldGame game, out int payment)
    {
        payment = 0;

        if (game == null || !payments.TryGetValue(game, out SleepData data))
            return false;

        payment = data.payment;
        return true;
    }

    public static void End(RainWorldGame game, SleepData data, bool succeeded)
    {
        if (data == null)
            return;

        if (game != null && payments.TryGetValue(game, out SleepData active) && active == data)
            payments.Remove(game);

        if (!succeeded || data.save.malnourished)
            return;

        data.completedCycle = data.save.cycleNumber;
        data.nextMax = data.oldMax;
        data.nextNormal = data.oldNormal;
        data.nextGold = data.oldGold;

        if (data.karma11)
        {
            data.nextMax = 9 + data.save.GetVoidExtraFood();
            data.nextNormal = 6 + (data.save.GetVoidExtraFood() == 3
                ? data.save.GetVoidFoodToHibernate() : 0);
            data.nextGold = 2 * data.save.GetVoidFoodToHibernate();
        }
        else if (data.save.lastMalnourished && !data.save.malnourished)
        {
            if (data.save.saveStateNumber == VoidEnums.SlugcatID.Void && data.save.GetVoidMarkV3())
                data.nextNormal = 6 + (data.save.GetVoidExtraFood() == 3
                    ? data.save.GetVoidFoodToHibernate() : 0);
            else
                data.nextNormal = SlugcatStats.SlugcatFoodMeter(data.stats.name).y;
        }

        data.nextNormal = Mathf.Clamp(data.nextNormal, 0, data.nextMax);
        pending.Remove(data.stats);
        pending.Add(data.stats, data);
    }

    public static void Bind(SleepAndDeathScreen screen, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        if (!screen.IsSleepScreen || package?.characterStats == null
            || !pending.TryGetValue(package.characterStats, out SleepData data)
            || package.saveState != data.save || data.save.cycleNumber != data.completedCycle)
            return;

        screens.Remove(screen);
        screens.Add(screen, data);
        pending.Remove(package.characterStats);
    }

    public static bool HasData(SleepAndDeathScreen screen)
    {
        return screens.TryGetValue(screen, out _);
    }

    public static bool PrepareMeter(FoodMeter meter, HUD.HUD hud, Player associatedPup, ref int maxFood, ref int survivalLimit)
    {
        if (associatedPup != null || hud.owner is not SleepAndDeathScreen screen
            || !screens.TryGetValue(screen, out SleepData data))
            return false;

        maxFood = data.oldMax;
        survivalLimit = data.payment;

        meters.Remove(meter);
        meters.Add(meter, new MeterState { data = data });
        return true;
    }

    public static void FinishMeter(FoodMeter meter)
    {
        if (!meters.TryGetValue(meter, out MeterState state))
            return;

        state.shown = state.data.marker;
        state.lastShown = state.shown;
        state.from = state.shown;
        state.initialized = true;
        meter.eatCircles = state.data.payment;
        meter.MoveSurvivalLimit(state.shown, false);
        meter.GetMeterExt().showNumFoodTohibernate = state.data.oldGold;
    }

    public static bool TryGetGoldStart(FoodMeter meter, out int start)
    {
        start = 0;

        if (!meters.TryGetValue(meter, out MeterState state) || !state.data.karma11)
            return false;

        bool nextCycle = state.stage == Stage.Returning || state.stage == Stage.Done;
        start = nextCycle
            ? state.data.nextNormal - state.data.nextGold
            : state.data.oldNormal - state.data.oldGold;
        return true;
    }

    public static bool Update(On.HUD.FoodMeter.orig_SleepUpdate orig, FoodMeter meter)
    {
        if (!meters.TryGetValue(meter, out MeterState state) || !state.initialized)
            return false;

        state.lastShown = state.shown;

        if (state.stage == Stage.Countdown && meter.sleepScreenPhase == 0 && meter.eatCircles == 0)
            WaitForPips(meter, state);

        orig(meter);

        if (state.stage == Stage.Countdown)
        {
            if (meter.sleepScreenPhase == 0 && meter.eatCircles == 0)
                WaitForPips(meter, state);

            meter.MoveSurvivalLimit(state.shown, false);
            return true;
        }

        if (state.stage == Stage.WaitForPips)
        {
            if (meter.eatCircleDelay > 0 || HasEatingPip(meter))
            {
                meter.MoveSurvivalLimit(state.shown, false);
                return true;
            }

            state.stage = Stage.Returning;
            state.from = state.shown;
            state.counter = 0;
            meter.maxFood = state.data.nextMax;
            meter.survivalLimit = state.data.nextNormal;
            meter.GetMeterExt().showNumFoodTohibernate = state.data.nextGold;

            while (meter.circles.Count < meter.maxFood)
            {
                FoodMeter.MeterCircle circle = new(meter, meter.circles.Count);
                meter.circles.Add(circle);
                circle.AddGradient();
                circle.AddCircles();
            }
        }

        if (state.stage == Stage.Returning)
        {
            meter.sleepScreenPhase = 4;
            meter.eatCircleDelay = 2;
            state.counter++;
            float t = Mathf.Clamp01((float)state.counter / ReturnTicks);
            state.shown = Mathf.Lerp(state.from, state.data.nextNormal, t);
            meter.MoveSurvivalLimit(state.shown, false);

            if (state.counter >= ReturnTicks)
            {
                state.stage = Stage.Done;
                meter.eatCircleDelay = 40;
                meter.sleepScreenPhase = 3;
            }
        }

        return true;
    }

    private static void WaitForPips(FoodMeter meter, MeterState state)
    {
        state.stage = Stage.WaitForPips;
        meter.sleepScreenPhase = 4;
        meter.eatCircleDelay = ReturnTicks;
    }

    private static bool HasEatingPip(FoodMeter meter)
    {
        for (int i = 0; i < meter.circles.Count; i++)
        {
            if (meter.circles[i].eaten && meter.circles[i].eatCounter > 0)
                return true;
        }

        return false;
    }

    public static void Draw(FoodMeter meter, float timeStacker)
    {
        if (!meters.TryGetValue(meter, out MeterState state) || !state.initialized)
            return;

        meter.lineSprite.x += meter.CircleDistance(timeStacker)
            * (Mathf.Lerp(state.lastShown, state.shown, timeStacker) - state.shown);
    }
}
