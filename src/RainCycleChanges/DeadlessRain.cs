using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;

namespace VoidTemplate.RainCycleChanges;

using System;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using UnityEngine;

public static class DeadlessRain
{
    public sealed class RainState
    {
        public float ox;
        public float oy;
        public float smoothI;
        public float t;
    }

    public static readonly ConditionalWeakTable<GlobalRain, RainState> _states = new();

    private static RainState GetState(GlobalRain rain) => _states.GetValue(rain, _ =>
        new RainState
        {
            ox = UnityEngine.Random.value * 1000f,
            oy = UnityEngine.Random.value * 1000f,
            smoothI = 1f,
            t = 0f
        });

    public static bool TryGetState(this GlobalRain rain, out RainState state) => _states.TryGetValue(rain, out state);

    public static void Hook()
    {
        new Hook(typeof(GlobalRain).GetProperty("InsidePushAround").GetMethod, GlobalRain_InsidePushAround);
        On.RoomSettings.Load_Timeline += RoomSettings_Load_Timeline;
        On.GlobalRain.Update += GlobalRain_Update;
    }

    private static void GlobalRain_Update(On.GlobalRain.orig_Update orig, GlobalRain self)
    {
        orig(self);

        if (self.game.StoryCharacter == VoidEnums.SlugcatID.Void &&
            self.deathRain != null &&
            self.deathRain.deathRainMode == GlobalRain.DeathRain.DeathRainMode.Mayhem)
        {
            var st = GetState(self);

            ref float t = ref st.t;
            t += 0.005f;

            float fbm = 0f;
            float amp = 1f;
            float freq = 0.06f;
            float norm = 0f;

            for (int i = 0; i < 4; i++)
            {
                fbm += amp * Mathf.PerlinNoise(st.ox + t * freq, st.oy + t * freq * 1.17f);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            fbm /= norm;

            float gust = Mathf.PerlinNoise(st.ox + 777.7f, st.oy + t * 0.015f);
            float gustMul = Mathf.Lerp(0.55f, 1.35f, Mathf.SmoothStep(0f, 1f, gust));

            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 0.9f + fbm * 6.28318f);

            float target = fbm * 0.75f + pulse * 0.25f;
            target = Mathf.SmoothStep(0f, 1f, target);
            target = Mathf.Pow(target, 0.85f) * gustMul;
            target = Mathf.Clamp01(target);

            st.smoothI = Mathf.Lerp(st.smoothI, target, 0.08f);
            self.Intensity = st.smoothI;
        }
    }

    private static float GlobalRain_InsidePushAround(Func<GlobalRain, float> orig, GlobalRain self)
    {
        if (self.game.StoryCharacter == VoidEnums.SlugcatID.Void)
            return 0f;

        return orig(self);
    }

    private static bool RoomSettings_Load_Timeline(On.RoomSettings.orig_Load_Timeline orig, RoomSettings self, SlugcatStats.Timeline timelinePoint)
    {
        bool loaded = orig(self, timelinePoint);

        if (loaded && !self.isTemplate &&
            timelinePoint == VoidEnums.SlugcatTimeline.VoidTimeline &&
            self.DangerType != DLCSharedEnums.RoomRainDangerType.Blizzard &&
            self.DangerType != WatcherEnums.WatcherDangerType.Sandstorm)
        {
            float intensity;
            float rumble;

            if (self.DangerType == RoomRain.DangerType.Flood || self.DangerType == RoomRain.DangerType.None)
            {
                intensity = 0f;
                rumble = 0.3f;
            }
            else
            {
                intensity = 0.6f;
                rumble = 0.06f;
            }

            self.RainIntensity = intensity;
            self.RumbleIntensity = rumble;
        }

        return loaded;
    }
}
