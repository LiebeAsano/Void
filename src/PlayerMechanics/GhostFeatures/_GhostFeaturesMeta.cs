namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
	internal static class _GhostFeaturesMeta
	{
		public static void Hook()
		{
			ConversationPath.Hook();
			EncounterIL.Hook();
			KarmaLadderNonRefillCapIncrease.Hook();
			UpdateIL.Hook();
			MSGhostForTheVoid.Hook();
			NoGhostFlakes.Hook();
		}
	}
}
