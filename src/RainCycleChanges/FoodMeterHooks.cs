using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
                    if (!self.meter.IsPupFoodMeter && self.meter.hud.owner is Player player
                    && (player.abstractCreature.world.game.IsVoidWorld() || player.abstractCreature.world.game.IsViyWorld()))
                    {
                        var cycleExt = player.abstractCreature.world.rainCycle.GetRainCycleExt();
                        int col = 0;
                        if (cycleExt.AllowToSubtractFood && self.number >= self.meter.lastCount - self.meter.survivalLimit + cycleExt.subtractedFood && self.foodPlopped)
                        {
                            col = 1;
                        }
                        self.circles[0].color = self.circles[1].color = col;
                    }
                });
            }
            else
            {
                Utils.LogExErr("Matching error!");
            }
        }
    }
}
