namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
    public static void Hook()
    {
        ColdImmunityPatch.Hook();
        DreamManager.RegisterMaps();
        DreamManager.Hook();
        ExtendedLungs.Hook();
        Grabability.Hook();
        SpearmasterAntiMechanic.Hook();
    }
}
