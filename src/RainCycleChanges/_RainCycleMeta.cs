namespace VoidTemplate.RainCycleChanges
{
    public class _RainCycleMeta
    {
        public static void Init()
        {
            DeadlessRain.Hook();
            MartyrPassageFix.Hook();
            PostRainCycle.Hook();
            PostCycleDawn.Hook();
            RainFoodRequirement.Hook();
            ShortcutHooks.Hook();
            RainMeterHooks.Hook();
            FoodMeterHooks.Hook();
            WaterGateHooks.Hook();
        }
    }
}
