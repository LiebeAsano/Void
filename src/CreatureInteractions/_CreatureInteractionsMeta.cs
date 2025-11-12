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
		BlizzardBeamNerf.Hook();
		CentipedeColour.Hook();
		CorruptionEaten.Hook();
		LoachEaten.Hook();
        OverseerBehavior.Hook();
		SandGrubEaten.Hook();
        WormGrassEaten.Hook();
		EdibleMoths.Hook();
		FallDamage.CreatureFallDamage.Hook();
		HunterDaddyGraphicsHooks.Hook();
	}
}
