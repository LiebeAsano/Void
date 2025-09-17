using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery
{
    public class FoodMeterPipsChange
    {
        public static void Hook()
        {
            On.HUD.FoodMeter.MeterCircle.Draw += MeterCircle_Draw;
            On.HUD.FoodMeter.MeterCircle.AddCircles += MeterCircle_AddCircles;
        }

        private static void MeterCircle_AddCircles(On.HUD.FoodMeter.MeterCircle.orig_AddCircles orig, FoodMeter.MeterCircle self)
        {
            orig(self);
            self.circles[1].sprite.MoveBehindOtherNode(self.circles[0].sprite);
        }

        private static void MeterCircle_Draw(On.HUD.FoodMeter.MeterCircle.orig_Draw orig, FoodMeter.MeterCircle self, float timeStacker)
        {
            orig(self, timeStacker);
            if (self.meter.hud.owner is Player player && player.IsVoid() && !self.meter.IsPupFoodMeter && self.number >= self.meter.survivalLimit && self.foodPlopped)
            {
                self.circles[0].sprite.color = Utils.VoidColors[0];
                self.circles[1].sprite.color = new(0, 0, 0.005f);
                self.circles[1].sprite.scale = (self.circles[0].rad + 3) / 8f;
            }
        }
    }
}
