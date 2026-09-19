using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class PostCycleBlizzard
{
    public static void Hook()
    {
        On.MoreSlugcats.BlizzardGraphics.CycleUpdate += BlizzardGraphics_CycleUpdate;
        IL.Creature.HypothermiaUpdate += Creature_HypothermiaUpdate;
    }

    public static bool TryGetStorm(World world, out float storm)
    {
        storm = 1f;

        if (world?.game == null || !PostRainCycle.HasRainCycle(world.game)
            || !world.rainCycle.GetRainCycleExt().PostCycleStarted)
            return false;

        storm = world.game.globalRain.Intensity;
        return true;
    }

    public static int ColdTimer(RainCycle rainCycle) =>
        TryGetStorm(rainCycle.world, out _) ? Mathf.Min(rainCycle.timer, rainCycle.cycleLength) : rainCycle.timer;

    public static float ColdGain(float gain, World world) =>
        TryGetStorm(world, out float storm) ? gain * storm : gain;

    private static void BlizzardGraphics_CycleUpdate(On.MoreSlugcats.BlizzardGraphics.orig_CycleUpdate orig, BlizzardGraphics self)
    {
        orig(self);

        if (!TryGetStorm(self.room.world, out float storm))
            return;

        self.windStrength *= storm;
        self.blizzardIntensity *= storm;
        self.snowfallIntensity *= storm;
        self.whiteOut *= storm;
    }

    private static void Creature_HypothermiaUpdate(ILContext il)
    {
        ILCursor c = new(il);
        int timers = 0;

        while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<RainCycle>(nameof(RainCycle.timer))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((int timer, Creature self) => ColdTimer(self.room.world.rainCycle));
            timers++;
        }

        if (timers == 0)
            Utils.LogExErr("PostCycleBlizzard: cycle timer not found in Creature.HypothermiaUpdate");

        c.Index = 0;

        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdcR4(0.0055f),
            x => x.MatchCall<Mathf>(nameof(Mathf.Clamp))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((float gain, Creature self) => ColdGain(gain, self.room.world));
        }
        else Utils.LogExErr("PostCycleBlizzard: hypothermia gain clamp not found in Creature.HypothermiaUpdate");
    }
}
