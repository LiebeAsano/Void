namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
    public static void Hook()
    {
        ColdImmunityPatch.Hook();
        Dreams.Hook();
        Grabability.Hook();
        SpearmasterAntiMechanic.Hook();
    }
}
