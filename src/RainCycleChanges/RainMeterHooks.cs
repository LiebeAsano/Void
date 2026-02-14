using HUD;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace VoidTemplate.RainCycleChanges
{
    public static class RainMeterHooks
    {
        private static ConditionalWeakTable<RainMeter, StrongBox<bool>> afterCycleMode = new();

        public static StrongBox<bool> GetAfterCycleMode(this RainMeter rainMeter) => afterCycleMode.GetOrCreateValue(rainMeter);

        public static void Hook()
        {
            On.HUD.RainMeter.ctor += RainMeter_ctor;
            On.HUD.RainMeter.Update += RainMeter_Update;
        }

        private static void RainMeter_Update(On.HUD.RainMeter.orig_Update orig, RainMeter self)
        {
            if (self.GetAfterCycleMode().Value)
            {
                World world = (self.hud.owner as Player).abstractCreature.world;
                bool disableRain = world.game.setupValues.disableRain;
                if (ModManager.MSC && world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && !Region.IsRubiconRegion(self.hud.map.RegionName))
                {
                    self.halfTimeShown = true;
                }
                if (ModManager.MSC && (self.hud.owner as Player).inVoidSea)
                {
                    self.halfTimeShown = true;
                }
                self.lastPos = self.pos;
                self.pos = self.hud.karmaMeter.pos;
                if (!self.halfTimeShown && !disableRain && world.rainCycle.GetRainCycleExt().AmountLeft < 0.5f && (self.hud.owner as Player).room != null && (self.hud.owner as Player).room.roomSettings.DangerType != RoomRain.DangerType.None && (!ModManager.MMF || !world.rainCycle.RegionHidesTimer))
                {
                    self.halfTimeBlink = 220;
                    self.halfTimeShown = true;
                }
                self.lastFade = self.fade;
                if (self.remainVisibleCounter > 0)
                {
                    self.remainVisibleCounter--;
                }
                if (self.halfTimeBlink > 0)
                {
                    self.halfTimeBlink--;
                    self.hud.karmaMeter.forceVisibleCounter = Math.Max(self.hud.karmaMeter.forceVisibleCounter, 10);
                }
                if (ModManager.MMF && MMF.cfgTickTock.Value)
                {
                    self.tickPulse = Mathf.Lerp(self.tickPulse, 0f, 0.1f);
                }
                else
                {
                    self.tickPulse = 0f;
                }
                if ((self.hud.karmaMeter.fade > 0f && self.Show) || self.remainVisibleCounter > 0)
                {
                    self.fade = Mathf.Min(1f, self.fade + 0.033333335f);
                    if (ModManager.MMF && MMF.cfgTickTock.Value && (self.hud.owner as Player).room != null && self.hud.owner.RevealMap && world.rainCycle.AmountLeft > 0f && !world.rainCycle.RegionHidesTimer)
                    {
                        self.tickCounter++;
                        if (self.tickCounter % 240 == 0)
                        {
                            (self.hud.owner as Player).room.PlaySound(MMFEnums.MMFSoundID.Tick, 0f, 0.85f, 1f);
                            self.tickPulse = 1f;
                        }
                        if (self.tickCounter % 240 == 120)
                        {
                            (self.hud.owner as Player).room.PlaySound(MMFEnums.MMFSoundID.Tock, 0f, 0.85f, 1f);
                            self.tickPulse = 1f;
                        }
                    }
                }
                else
                {
                    self.fade = Mathf.Max(0f, self.fade - 0.1f);
                }
                if (self.hud.HideGeneralHud)
                {
                    self.fade = 0f;
                }
                if (self.fade >= 0.7f)
                {
                    self.plop = Mathf.Min(1f, self.plop + 0.05f);
                }
                else
                {
                    self.plop = 0f;
                }
                if (disableRain)
                {
                    self.fRain = 1f;
                }
                else
                {
                    self.fRain = world.rainCycle.GetRainCycleExt().AmountLeft;
                }
                bool flag = ModManager.MMF && MMF.cfgHideRainMeterNoThreat.Value && world.rainCycle.RegionHidesTimer && ((self.hud.owner as Player).room == null || (self.hud.owner as Player).room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.DayNight) == 0f);
                for (int i = 0; i < self.circles.Length; i++)
                {
                    self.circles[i].Update();
                    if (self.fade > 0f || self.lastFade > 0f)
                    {
                        float num = (float)i / (float)(self.circles.Length - 1);
                        float num2 = Mathf.InverseLerp((float)i / (float)self.circles.Length, (float)(i + 1) / (float)self.circles.Length, self.fRain);
                        float num3 = Mathf.InverseLerp(0.5f, 0.475f, Mathf.Abs(0.5f - Mathf.InverseLerp(0.033333335f, 1f, num2)));
                        if (flag)
                        {
                            self.circles[i].rad = (3f * Mathf.Pow(self.fade, 2f) + Mathf.InverseLerp(0.075f, 0f, Mathf.Abs(1f - num - Mathf.Lerp((1f - self.fRain) * self.fade - 0.075f, 1.075f, Mathf.Pow(self.plop, 0.85f)))) * 2f * self.fade) * Mathf.InverseLerp(0f, 0.033333335f, 1f);
                            self.circles[i].thickness = 1f;
                            self.circles[i].snapGraphic = HUDCircle.SnapToGraphic.smallEmptyCircle;
                            self.circles[i].snapRad = 3f;
                            self.circles[i].snapThickness = 1f;
                        }
                        else
                        {
                            if (self.halfTimeBlink > 0)
                            {
                                num3 = Mathf.Max(num3, (self.halfTimeBlink % 15 < 7) ? 0f : 1f);
                            }
                            self.circles[i].rad = ((2f + num3) * Mathf.Pow(self.fade, 2f) + Mathf.InverseLerp(0.075f, 0f, Mathf.Abs(1f - num - Mathf.Lerp((1f - self.fRain) * self.fade - 0.075f, 1.075f, Mathf.Pow(self.plop, 0.85f)))) * 2f * self.fade) * Mathf.InverseLerp(0f, 0.033333335f, num2);
                            if (num3 == 0f)
                            {
                                self.circles[i].thickness = -1f;
                                self.circles[i].snapGraphic = HUDCircle.SnapToGraphic.Circle4;
                                self.circles[i].snapRad = 2f;
                                self.circles[i].snapThickness = -1f;
                            }
                            else
                            {
                                self.circles[i].thickness = Mathf.Lerp(3.5f, 1f, num3);
                                self.circles[i].snapGraphic = HUDCircle.SnapToGraphic.smallEmptyCircle;
                                self.circles[i].snapRad = 3f;
                                self.circles[i].snapThickness = 1f;
                            }
                        }
                        self.circles[i].pos = self.pos + Custom.DegToVec((1f - (float)i / (float)self.circles.Length) * 360f * Custom.SCurve(Mathf.Pow(self.fade, 1.5f - num), 0.6f)) * (self.hud.karmaMeter.Radius + 8.5f + num3 + 4f * self.tickPulse);
                    }
                    else
                    {
                        self.circles[i].rad = 0f;
                    }
                }
                return;
            }
            orig(self);
        }

        private static void RainMeter_ctor(On.HUD.RainMeter.orig_ctor orig, RainMeter self, HUD.HUD hud, FContainer fContainer)
        {
            orig(self, hud, fContainer);
            if (hud.owner is Player { abstractCreature.world: var world } && world.rainCycle.GetRainCycleExt().PostCycleStarted)
            {
                self.GetAfterCycleMode().Value = true;
                self.lastPos = self.pos;
                self.timePerCircle = 2400;
                int num = world.rainCycle.GetRainCycleExt().postCycleLength / self.timePerCircle;
                self.circles = new HUDCircle[num];
                for (int i = 0; i < self.circles.Length; i++)
                {
                    self.circles[i] = new HUDCircle(hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, 1);
                }
                if (ModManager.MSC && world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && !Region.IsRubiconRegion(hud.map.RegionName))
                {
                    self.halfTimeShown = true;
                }
            }
        }
    }
}
