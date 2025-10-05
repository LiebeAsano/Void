namespace VoidTemplate.PlayerMechanics.Karma11Features;

public static class _Karma11FeaturesMeta
{
	public static void Hook()
	{
		EatMeatUpdate.Hook();
		FoodChange.Hook();
		Karma11Update.Hook();
		NourishmentOfObjectEaten.Hook();
		VoidSpawnGraphics.Hook();
        FoodMeterPipsChange.Hook();
    }
}
