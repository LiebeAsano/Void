namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class _Karma11FeaturesMeta
{
    public static void Hook()
    {
        EatMeatUpdate.Hook();
        FoodChange.Hook();
        Karma11Update.Hook();
        NourishmentOfObjectEaten.Hook();
    }
}
