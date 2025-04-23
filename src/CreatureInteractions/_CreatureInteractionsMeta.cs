namespace VoidTemplate.CreatureInteractions;

public static class _CreatureInteractionsMeta
{
	public static void Hook()
	{
		AntiSpiderStun.Hook();
		DLLindigestion.Hook();
		LeechIndigestion.Hook();
		BigJellyfishStunImmunity.Hook();
		//OverseerBehavior.Hook();
		WormGrassEatenHook.Hook();
	}
}
