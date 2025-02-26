namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class _Karma11FoundationMeta
{
    public static void Hook()
    {
        IngameHUDTokenBump.Startup();
        Karma11Symbol.Startup();
        KarmaLadderTokenDecrease.Initiate();
        KarmaLadderTweaks.Hook();
        NoKarmaDecreaseOnDeath.Initiate();
        TokenSystem.Initiate();
    }
}
