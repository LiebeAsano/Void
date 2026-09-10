using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges
{
    public class FoodMeterHooks
    {
        public static void Hook()
        {
            IL.HUD.FoodMeter.MeterCircle.Update += MeterCircle_Update;
        }

        private static void MeterCircle_Update(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(x => x.MatchCallvirt<FoodMeter>("get_IsPupFoodMeter")) &&
                c.TryGotoNext(MoveType.After, x => x.MatchStfld<HUDCircle>("color")))
            {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);

                c.EmitDelegate((FoodMeter.MeterCircle self) =>
                {
                    if (self.meter.IsPupFoodMeter || self.meter.hud.owner is not Player player) return;

                    var game = player.abstractCreature.world.game;

                    if (!game.IsVoidWorld() && !game.IsViyWorld()) return;

                    var cycleExt = player.abstractCreature.world.rainCycle.GetRainCycleExt();

                    bool red = false;

                    if (cycleExt.AllowToSubtractFood)
                    {
                        int currentFood = self.meter.lastCount;

                        int requiredFood = Mathf.Max(0,self.meter.survivalLimit - cycleExt.subtractedFood);

                        int firstRed;
                        int lastRed;

                        if (currentFood >= requiredFood)
                        {
                            firstRed = currentFood - requiredFood;
                            lastRed = currentFood;
                        }
                        else
                        {
                            firstRed = 0;
                            lastRed = requiredFood;
                        }

                        red = self.number >= firstRed && self.number < lastRed;
                    }

                    int color = red ? 1 : 0;

                    self.circles[0].color = color;
                    self.circles[1].color = color;
                });
            }
            else
            {
                Utils.LogExErr("FoodMeterHooks.MeterCircle_Update: match failed!");
            }
        }
    }
}