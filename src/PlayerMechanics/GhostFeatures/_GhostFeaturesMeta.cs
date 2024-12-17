namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
	internal static class _GhostFeaturesMeta
	{
		public static void Hook()
		{
			ConversationPath.Hook();
			EncounterIL.Hook();
			KarmaLadderNonRefillCapIncrease.Hook();
			MSGhostForTheVoid.Hook();
			NoGhostFlakes.Hook();
			NoGhostHunch.Hook();
			UpdateIL.Hook();
		}
	}
}
