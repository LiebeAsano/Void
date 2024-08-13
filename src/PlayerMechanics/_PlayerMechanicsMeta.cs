namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
    public static void Hook()
    {
        ColdImmunityPatch.Hook();
        DreamManager.Hook();
        ExtendedLungs.Hook();
        Grabability.Hook();
        SpearmasterAntiMechanic.Hook();
    }
}
