namespace VoidTemplate.RainCycleChanges
{
    public class _RainCycleMeta
    {
        public static void Init()
        {
            DeadlessRain.Hook();
            PostRainCycle.Hook();
            RainFoodRequirement.Hook();
            ShortcutHooks.Hook();
            RainMeterHooks.Hook();
            FoodMeterHooks.Hook();
            WaterGateHooks.Hook();
        }
    }
}
