namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
    public static void Hook()
    {
        ColdImmunityPatch.Hook();
        DreamManager.RegisterMaps();
        DreamCustom.Hook();
        DreamManager.Hook();
        ExtendedLungs.Hook();
        Grabability.Hook();
        SaintKarmaImmunity.Hook();
        SpearmasterAntiMechanic.Hook();
    }
}
