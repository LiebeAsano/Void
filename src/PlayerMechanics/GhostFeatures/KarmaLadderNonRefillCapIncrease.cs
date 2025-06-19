using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
	public static class KarmaLadderNonRefillCapIncrease
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
				iLCursor.EmitDelegate(IsForVoidOrViy);
				iLCursor.Emit(OpCodes.Brtrue, dontMoveToPreGhostKarmaCapLabel);
			}
			else
			{
				LogExErr("First failed to match KarmaLadder karma cap increase animation (pre-add scroll), " +
					"karma ladder screen will display wrong data for the Void");
			}

			iLCursor = new(il);

			if (iLCursor.TryGotoNext(MoveType.After,
				c => c.MatchLdarg(0),
				c => c.MatchLdarg(0),
				c => c.MatchCall(typeof(KarmaLadder).GetProperty(nameof(KarmaLadder.displayKarmaCap)).GetGetMethod()),
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
				LogExErr("Second failed to match KarmaLadder karma cap increase animation (post-add scroll), " +
					"karma ladder screen will display wrong data for the Void");
			}
		}

		private static bool IsForVoidOrViy(KarmaLadder self)
		{
			return (self.menu as KarmaLadderScreen).saveState.saveStateNumber == VoidEnums.SlugcatID.Void || (self.menu as KarmaLadderScreen).saveState.saveStateNumber == VoidEnums.SlugcatID.Viy;
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
