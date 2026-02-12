using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;

namespace VoidTemplate.RainCycleChanges
{
    public static class DeadlessRain
    {
        public static ConditionalWeakTable<GlobalRain, StrongBox<float>> PulseRain = new();

        public static StrongBox<float> GetRainPulseRef(this GlobalRain rain) => PulseRain.GetOrCreateValue(rain);

        public static void Hook()
        {
            new Hook(typeof(GlobalRain).GetProperty("InsidePushAround").GetMethod, GlobalRain_InsidePushAround);
            On.RoomSettings.Load_Timeline += RoomSettings_Load_Timeline;
            On.GlobalRain.Update += GlobalRain_Update;
        }

        private static void GlobalRain_Update(On.GlobalRain.orig_Update orig, GlobalRain self)
        {
            orig(self);
            if (self.game.StoryCharacter == VoidEnums.SlugcatID.Void && self.deathRain != null && self.deathRain.deathRainMode == GlobalRain.DeathRain.DeathRainMode.Mayhem)
            {
                self.GetRainPulseRef().Value += 0.025f;
                self.Intensity = Mathf.Abs(Mathf.Cos(self.GetRainPulseRef().Value));
            }
        }

        private static float GlobalRain_InsidePushAround(Func<GlobalRain, float> orig, GlobalRain self)
        {
            if (self.game.StoryCharacter == VoidEnums.SlugcatID.Void)
            {
                return 0;
            }
            return orig(self);
        }

        private static bool RoomSettings_Load_Timeline(On.RoomSettings.orig_Load_Timeline orig, RoomSettings self, SlugcatStats.Timeline timelinePoint)
        {
            bool loaded = orig(self, timelinePoint);

            if (loaded && !self.isTemplate && timelinePoint == VoidEnums.SlugcatTimeline.VoidTimeline && self.DangerType != DLCSharedEnums.RoomRainDangerType.Blizzard && self.DangerType != WatcherEnums.WatcherDangerType.Sandstorm)
            {
                float intensity;
                float rumble;
                if (self.DangerType == RoomRain.DangerType.Flood || self.DangerType == RoomRain.DangerType.None)
                {
                    intensity = 0;
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
}
