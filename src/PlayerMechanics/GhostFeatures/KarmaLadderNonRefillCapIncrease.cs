using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
	internal static class KarmaLadderNonRefillCapIncrease
	{
		public static void Hook()
		{
			IL.Menu.KarmaLadder.Update += KarmaLadder_Update;
		}

		private static void KarmaLadder_Update(ILContext il)
		{
			ILCursor iLCursor = new(il);

			ILLabel dontMoveToPreGhostKarmaCapLabel = null;

			if (iLCursor.TryGotoNext(MoveType.Before,
				c => c.MatchLdarg(0),
				c => c.MatchLdflda<KarmaLadder>(nameof(KarmaLadder.displayKarma)),
				c => c.MatchLdfld<IntVector2>(nameof(IntVector2.x)),
				c => c.MatchLdarg(0),
				c => c.MatchLdfld<MenuObject>(nameof(MenuObject.menu)),
				c => c.MatchIsinst<KarmaLadderScreen>(),
				c => c.MatchLdfld<KarmaLadderScreen>(nameof(KarmaLadderScreen.preGhostEncounterKarmaCap)),
				c => c.MatchBge(out dontMoveToPreGhostKarmaCapLabel)))
			{
				iLCursor.Emit(OpCodes.Ldarg_0);
				iLCursor.EmitDelegate(SkipMoveToPreGhostEncounterMax);
				iLCursor.Emit(OpCodes.Brtrue, dontMoveToPreGhostKarmaCapLabel);
			}
			else
			{
				logerr("Failed to match KarmaLadder karma cap increase animation (pre-add scroll), " +
					"karma ladder screen will display wrong data for the Void");
			}

			iLCursor = new(il);

			if (iLCursor.TryGotoNext(MoveType.After,
				c => c.MatchLdarg(0),
				c => c.MatchLdarg(0),
				c => c.MatchLdflda<KarmaLadder>(nameof(KarmaLadder.displayKarma)),
				c => c.MatchLdfld<IntVector2>(nameof(IntVector2.y)),
				c => c.Match(OpCodes.Ldc_I4_1),
				c => c.MatchCall<KarmaLadder>(nameof(KarmaLadder.GoToKarma)))
				&&
				iLCursor.TryGotoPrev(MoveType.Before,
				c => c.Match(OpCodes.Ldc_I4_1),
				c => c.MatchCall<KarmaLadder>(nameof(KarmaLadder.GoToKarma))
				))
			{
				iLCursor.Emit(OpCodes.Ldarg_0);
				iLCursor.EmitDelegate(TargetGoToKarma);
			}
			else
			{
				logerr("Failed to match KarmaLadder karma cap increase animation (post-add scroll), " +
					"karma ladder screen will display wrong data for the Void");
			}
		}

		// Does the skip internally to avoid tainting IL for other matches. Leaves a flag on eval stack to decide which branch to follow.
		private static bool SkipMoveToPreGhostEncounterMax(KarmaLadder self)
		{
			return (self.menu as KarmaLadderScreen).saveState.saveStateNumber == VoidEnums.SlugcatID.Void;
		}

		private static int TargetGoToKarma(int unmodifiedGoToKarma, KarmaLadder self)
		{
			KarmaLadderScreen karmaLadderScreen = self.menu as KarmaLadderScreen;

			if (karmaLadderScreen.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
			{
				return karmaLadderScreen.saveState.deathPersistentSaveData.karma;
			}

			return unmodifiedGoToKarma;
		}
	}
}
