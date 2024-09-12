using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.PlayerMechanics.Karma11Foundation;

namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
    public static void Hook()
    {
        KarmaLadderTweaks.Hook();
        NoVisualMalnourishment.Startup();
        Karma11FoodChange.Hook();

        ColdImmunityPatch.Hook();
        DontBiteMimic.Hook();
        DontEatVoid.Hook();
        DreamManager.RegisterMaps();
        DreamCustom.Hook();
        DreamManager.Hook();
        ExtendedLungs.Hook();
        Grabability.Hook();
        GraspSave.Hook();
        HealthSpear.Hook();
        MalnourishmentDeath.Hook();
        RockHitSomething.Hook();
        SaintArenaKarma.Hook();
        SaintArenaSpears.Hook();
        SaintKarmaImmunity.Hook();
        SpearmasterAntiMechanic.Hook();
        SwallowObjects.Hook();

    }
}
