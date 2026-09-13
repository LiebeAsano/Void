using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.Useful;
using static VoidTemplate.RainCycleChanges.PostRainCycle;

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

            if (c.TryGotoNext(x =>x.MatchCallvirt<FoodMeter>("get_IsPupFoodMeter")) &&
                c.TryGotoNext(MoveType.After,x =>x.MatchStfld<HUDCircle>("color")))
            {
                c.MoveAfterLabels();

                c.Emit(OpCodes.Ldarg_0);

                c.EmitDelegate(
                    (FoodMeter.MeterCircle self) =>
                    {
                        if (self.meter.IsPupFoodMeter || self.meter.hud.owner is not Player player)
                            return;
                        
                        RainWorldGame game = player.abstractCreature?.world?.game;

                        if (game == null || (!game.IsVoidWorld() && !game.IsViyWorld()))
                            return;

                        RainCycleExt cycleExt = player.abstractCreature.world.rainCycle.GetRainCycleExt();
                        bool red = cycleExt.ShouldHighlightFoodPip(self.number, player.FoodInStomach);
                        int color = red ? 1 : 0;

                        self.circles[0].color =color;
                        self.circles[1].color =color;
                    });
            }
            else Utils.LogExErr("FoodMeterHooks.MeterCircle_Update: matching error!");
            
        }
    }
}