using System.Runtime.CompilerServices;
using HUD;
using RWCustom;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.RainCycleChanges.PostRainCycle;

namespace VoidTemplate.RainCycleChanges;

public class FoodMeterHooks
{
    private const int ColorFadeTicks = 50;

    private static readonly ConditionalWeakTable<FoodMeter.MeterCircle, PipState> states = new();
    private static readonly ConditionalWeakTable<FoodMeter, MeterState> meters = new();

    private sealed class PipState
    {
        public Player owner;
        public float red;
        public float lastRed;
    }

    private sealed class MeterState
    {
        public Player owner;
        public int target;
        public float shown;
        public float lastShown;
        public float from;
        public int counter;
        public int duration = ColorFadeTicks;
        public bool waiting;
        public FoodMeter.MeterCircle spending;
    }

    public static void Hook()
    {
        On.HUD.FoodMeter.Update += FoodMeter_Update;
        On.HUD.FoodMeter.Draw += FoodMeter_Draw;
        On.HUD.FoodMeter.MeterCircle.Update += MeterCircle_Update;
        On.HUD.FoodMeter.MeterCircle.EatFade += MeterCircle_EatFade;
        On.HUD.FoodMeter.MeterCircle.Draw += MeterCircle_Draw;
        On.HUD.FoodMeter.QuarterPipShower.Draw += QuarterPipShower_Draw;
    }

    private static bool TryGetCycle(FoodMeter meter, out Player player, out RainCycleExt cycle)
    {
        player = null;
        cycle = null;

        if (meter.IsPupFoodMeter || meter.hud.owner is not Player owner)
            return false;

        World world = owner.abstractCreature?.world;
        RainWorldGame game = world?.game;

        if (world?.rainCycle == null || !HasRainCycle(game))
            return false;

        player = owner;
        cycle = world.rainCycle.GetRainCycleExt();
        return true;
    }

    private static bool WaitingForEatFade(FoodMeter.MeterCircle circle, PipState state, Player player)
    {
        return state.red > 0f
            && circle.foodPlopped
            && !circle.eaten
            && circle.number >= player.FoodInStomach
            && circle.number < circle.meter.showCount;
    }

    private static bool HasPendingRedPip(FoodMeter meter, Player player)
    {
        for (int i = 0; i < meter.circles.Count; i++)
        {
            FoodMeter.MeterCircle circle = meter.circles[i];

            if (states.TryGetValue(circle, out PipState state)
                && state.owner == player
                && WaitingForEatFade(circle, state, player))
                return true;
        }

        return false;
    }

    private static void FoodMeter_Update(On.HUD.FoodMeter.orig_Update orig, FoodMeter self)
    {
        if (!TryGetCycle(self, out Player player, out _))
        {
            if (meters.TryGetValue(self, out _))
            {
                if (!self.IsPupFoodMeter && self.hud.owner is Player newOwner)
                {
                    self.survivalLimit = newOwner.slugcatStats.foodToHibernate;
                    self.MoveSurvivalLimit(self.survivalLimit, false);
                }

                meters.Remove(self);
            }

            orig(self);
            return;
        }

        bool active = RainFoodRequirement.TryGetRemaining(player.abstractCreature.world.game, out int required);

        if (!active && !meters.TryGetValue(self, out _))
        {
            orig(self);
            return;
        }

        if (!active)
            required = RainFoodRequirement.GetNormalFoodToHibernate(player.slugcatStats);

        required = Mathf.Clamp(required, 0, self.maxFood);
        MeterState state = meters.GetValue(self, _ => new MeterState());

        if (state.owner != player)
        {
            state.owner = player;
            state.target = self.survivalLimit;
            state.shown = self.ShowSurvivalLimit;
            state.lastShown = state.shown;
            state.from = state.shown;
            state.counter = state.duration;
            state.spending = null;
            state.waiting = false;
            self.visibleCounter = Mathf.Max(self.visibleCounter, 80);
        }

        state.lastShown = state.shown;

        if (state.target != required)
        {
            state.waiting = active && required < state.target && HasPendingRedPip(self, player);
            state.from = state.shown;
            state.target = required;
            state.counter = 0;
            state.duration = ColorFadeTicks;
            state.spending = null;
            self.visibleCounter = Mathf.Max(self.visibleCounter, 80);
        }

        self.survivalLimit = required;
        orig(self);
        self.survivalLimit = required;

        if (state.spending != null)
        {
            float progress = state.spending.eaten
                ? 1f - Mathf.Clamp01((float)state.spending.eatCounter / state.duration)
                : 1f;

            state.shown = Mathf.Lerp(state.from, state.target, progress);

            if (progress >= 1f)
            {
                state.spending = null;
                state.counter = state.duration;
            }
        }
        else
        {
            if (state.waiting && !HasPendingRedPip(self, player))
                state.waiting = false;

            if (!state.waiting && state.counter < state.duration)
            {
                state.counter++;
                state.shown = Mathf.Lerp(state.from, state.target, (float)state.counter / state.duration);
            }
        }

        self.MoveSurvivalLimit(state.shown, false);

        if (!active && !state.waiting && state.spending == null
            && state.shown == state.target && state.lastShown == state.shown)
            meters.Remove(self);
    }

    private static void FoodMeter_Draw(On.HUD.FoodMeter.orig_Draw orig, FoodMeter self, float timeStacker)
    {
        orig(self, timeStacker);
        RainFoodSleep.Draw(self, timeStacker);

        if (!meters.TryGetValue(self, out MeterState state)
            || self.IsPupFoodMeter || self.hud.owner != state.owner)
            return;

        self.lineSprite.x += self.CircleDistance(timeStacker)
            * (Mathf.Lerp(state.lastShown, state.shown, timeStacker) - state.shown);
    }

    private static void MeterCircle_Update(On.HUD.FoodMeter.MeterCircle.orig_Update orig, FoodMeter.MeterCircle self)
    {
        if (!TryGetCycle(self.meter, out Player player, out RainCycleExt cycle))
        {
            if (states.TryGetValue(self, out _))
            {
                self.circles[1].color = 0;
                states.Remove(self);
            }

            orig(self);
            return;
        }

        PipState state = states.GetValue(self, _ => new PipState());

        if (state.owner != player)
        {
            state.owner = player;
            state.red = 0f;
            state.lastRed = 0f;
        }

        state.lastRed = state.red;

        self.circles[1].color = 0;
        orig(self);

        bool highlighted = cycle.ShouldHighlightFoodPip(self.number, player.FoodInStomach);
        bool waitingForAnimation = WaitingForEatFade(self, state, player);
        float target = highlighted || waitingForAnimation ? 1f : 0f;

        state.red = Mathf.MoveTowards(state.red, target, 1f / ColorFadeTicks);

        if (state.red > 0f)
        {
            self.circles[0].color = 1;

            if (!self.ReqFoodPip())
                self.circles[1].color = 1;
        }
    }

    private static void MeterCircle_EatFade(On.HUD.FoodMeter.MeterCircle.orig_EatFade orig, FoodMeter.MeterCircle self)
    {
        orig(self);

        if (!self.eaten
            || !TryGetCycle(self.meter, out Player player, out RainCycleExt cycle)
            || !states.TryGetValue(self, out PipState state)
            || state.owner != player
            || state.red <= 0f)
            return;

        if (cycle.PostCycleStarted
            && meters.TryGetValue(self.meter, out MeterState meter)
            && meter.owner == player && meter.target < meter.shown)
        {
            meter.from = meter.shown;
            meter.spending = self;
            meter.duration = Mathf.Max(1, self.eatCounter);
            meter.counter = 0;
            meter.waiting = false;
        }
    }

    private static void MeterCircle_Draw(On.HUD.FoodMeter.MeterCircle.orig_Draw orig, FoodMeter.MeterCircle self, float timeStacker)
    {
        orig(self, timeStacker);

        if ((self.ReqFoodPip() && self.foodPlopped)
            || !TryGetFadeColor(self, timeStacker, out Color color))
            return;

        for (int i = 0; i < self.circles.Length; i++)
        {
            HUDCircle circle = self.circles[i];

            if (circle.snapGraphic == HUDCircle.SnapToGraphic.None || circle.snapRad <= 0f)
                continue;

            circle.sprite.element = Futile.atlasManager.GetElementWithName(circle.snapGraphic.ToString());
            circle.sprite.shader = circle.basicShader;
            circle.sprite.scale = Mathf.Max(0f, Mathf.Lerp(circle.lastRad, circle.rad, timeStacker)) / circle.snapRad;
            circle.sprite.alpha = Mathf.Clamp01(Mathf.Lerp(circle.lastFade, circle.fade, timeStacker));
            circle.sprite.color = color;
        }
    }

    private static void QuarterPipShower_Draw(On.HUD.FoodMeter.QuarterPipShower.orig_Draw orig, FoodMeter.QuarterPipShower self, float timeStacker)
    {
        orig(self, timeStacker);

        int index = self.owner.showCount;

        if (index >= 0 && index < self.owner.circles.Count
            && !self.owner.circles[index].ReqFoodPip()
            && TryGetFadeColor(self.owner.circles[index], timeStacker, out Color color))
            self.quarterPips.color = color;
    }

    public static bool TryGetRainColor(FoodMeter.MeterCircle self, float timeStacker, Color normalColor, out Color color)
    {
        color = normalColor;

        if (!states.TryGetValue(self, out PipState state)
            || self.meter.IsPupFoodMeter || self.meter.hud.owner != state.owner
            || (state.red <= 0f && state.lastRed <= 0f))
            return false;

        float red = Mathf.Lerp(state.lastRed, state.red, timeStacker);
        color = Color.Lerp(normalColor, Color.red, red);
        return true;
    }

    private static bool TryGetFadeColor(FoodMeter.MeterCircle self, float timeStacker, out Color color)
    {
        color = default;

        if (!states.TryGetValue(self, out PipState state)
            || self.meter.IsPupFoodMeter || self.meter.hud.owner != state.owner)
            return false;

        bool transitioning = state.red != state.lastRed
            || (state.red > 0f && state.red < 1f);

        if (!transitioning)
            return false;

        float red = Mathf.Lerp(state.lastRed, state.red, timeStacker);
        color = Color.Lerp(Custom.FadableVectorCircleColors[0], Custom.FadableVectorCircleColors[1], red);
        return true;
    }
}