using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Objects.NoodleEgg;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery
{
    public static class FoodMeterPipsChange
    {
        public static bool ReqFoodPip(this FoodMeter.MeterCircle pip)
        {
            return pip.meter.hud.owner is Player player && player.IsVoid() && !pip.meter.IsPupFoodMeter && pip.number >= pip.meter.survivalLimit;
        }
        

        public static void Hook()
        {
            On.HUD.FoodMeter.MeterCircle.Draw += MeterCircle_Draw;
            On.HUD.FoodMeter.MeterCircle.AddCircles += MeterCircle_AddCircles;
            IL.HUD.FoodMeter.MeterCircle.Update += MeterCircle_Update;
            On.HUD.FoodMeter.MeterCircle.FoodPlop += MeterCircle_FoodPlop;
            On.HUD.FoodMeter.QuarterPipShower.Draw += QuarterPipShower_Draw;
            On.HUD.FoodMeter.MeterCircle.Update += On_MeterCircle_Update;
            On.HUD.FoodMeter.MeterCircle.QuarterCirclePlop += MeterCircle_QuarterCirclePlop;
        }

        private static void MeterCircle_QuarterCirclePlop(On.HUD.FoodMeter.MeterCircle.orig_QuarterCirclePlop orig, FoodMeter.MeterCircle self)
        {
            if (self.ReqFoodPip())
            {
                self.rads[0, 0] += 1.5f;
                self.meter.hud.PlaySound(SoundID.Snail_Pop);
                FadeCircle fadeCircle = new(self.meter.hud, 10f, 4f, 0.82f, 14f, 4f, self.DrawPos(1f), self.meter.fContainer);
                fadeCircle.circle.circleShader = fadeCircle.hud.rainWorld.Shaders["VectorCircle"];
                fadeCircle.circle.forceColor = new(1f, 0.86f, 0f, 0.5f);
                self.meter.hud.fadeCircles.Add(fadeCircle);
                return;
            }
            orig(self);
        }

        private static void On_MeterCircle_Update(On.HUD.FoodMeter.MeterCircle.orig_Update orig, FoodMeter.MeterCircle self)
        {
            orig(self);
            if (self.ReqFoodPip())
            {
                self.circles[1].rad = self.circles[0].rad - 1;
            }
        }

        private static void QuarterPipShower_Draw(On.HUD.FoodMeter.QuarterPipShower.orig_Draw orig, FoodMeter.QuarterPipShower self, float timeStacker)
        {
            orig(self, timeStacker);
            if (self.owner.hud.owner is Player player && player.IsVoid() && !self.owner.IsPupFoodMeter && self.owner.showCount >= self.owner.survivalLimit && self.owner.showCount < self.owner.circles.Count)
            {
                self.quarterPips.color = new(0, 0, 0.005f);
                self.quarterPips.scale = (self.owner.circles[self.owner.showCount].circles[0].rad + 3) / 8f;
            }
        }

        private static void MeterCircle_FoodPlop(On.HUD.FoodMeter.MeterCircle.orig_FoodPlop orig, FoodMeter.MeterCircle self)
        {
            if (self.ReqFoodPip())
            {
                self.foodPlopped = true;
                self.rads[1, 1] += 2f;
                self.foodPlopDelay = 16;
                self.meter.hud.PlaySound(SoundID.Lizard_Jaws_Grab_NPC);
                return;
            }
            orig(self);
        }

        private static void MeterCircle_Update(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchNewobj<FadeCircle>()))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((FadeCircle circle, FoodMeter.MeterCircle self) =>
                {
                    if (self.ReqFoodPip())
                    {
                        circle.circle.circleShader = circle.hud.rainWorld.Shaders["VectorCircle"];
                        circle.circle.forceColor = new(1f, 0.86f, 0f, 0.5f);
                    }
                    return circle;
                });
            }
            else logerr($"{nameof(MenuTinkery)}.{nameof(FoodMeterPipsChange)}.{nameof(MeterCircle_Update)}: match failed");
        }

        private static void MeterCircle_AddCircles(On.HUD.FoodMeter.MeterCircle.orig_AddCircles orig, FoodMeter.MeterCircle self)
        {
            orig(self);
            if (self.ReqFoodPip())
            {
                self.circles[1].sprite.MoveBehindOtherNode(self.circles[0].sprite);
                self.circles[1].snapRad = self.circles[0].snapRad - 1;
            }
        }

        private static void MeterCircle_Draw(On.HUD.FoodMeter.MeterCircle.orig_Draw orig, FoodMeter.MeterCircle self, float timeStacker)
        {
            bool isVoidMeter = self.ReqFoodPip();
            if (isVoidMeter)
            {
                if (self.foodPlopped)
                {
                    self.circles[0].circleShader = self.meter.hud.rainWorld.Shaders["VectorCircle"];
                    self.circles[1].circleShader = self.meter.hud.rainWorld.Shaders["VectorCircle"];
                }
                else
                {
                    self.circles[0].circleShader = self.meter.hud.rainWorld.Shaders["VectorCircleFadable"];
                    self.circles[1].circleShader = self.meter.hud.rainWorld.Shaders["VectorCircleFadable"];
                }
            }
            orig(self, timeStacker);
            if (isVoidMeter && self.foodPlopped)
            {
                self.circles[0].sprite.color = new(1f, 0.86f, 0f);
                self.circles[1].sprite.color = new(0, 0, 0.005f);
                if (self.circles[1].sprite.shader == self.circles[1].basicShader)
                {
                    self.circles[1].sprite.scale = (self.circles[1].snapRad + 4.6f) / 8f;
                }
            }
        }
    }
}
