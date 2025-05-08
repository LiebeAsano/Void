namespace VoidTemplate.CreatureInteractions;

public static class _CreatureInteractionsMeta
{
	public static void Hook()
	{
		AntiSpiderStun.Hook();
		DLLindigestion.Hook();
		LeechIndigestion.Hook();
		BigJellyfishStunImmunity.Hook();
		BigMothDrinks.Hook();
		CorruptionEaten.Hook();
		LoachEaten.Hook();
        OverseerBehavior.Hook();
		SandGrubEaten.Hook();
        WormGrassEaten.Hook();
	}
}
