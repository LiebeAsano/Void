namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class _Karma11FoundationMeta
{
	public static void Hook()
	{
		KarmaLadderTweaks.Hook();
		NoKarmaDecreaseOnDeath.Initiate();
		TokenSystem.Initiate();
		Karma11Symbol.Startup();
	}
}
