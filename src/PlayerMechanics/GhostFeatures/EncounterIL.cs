using IL.Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidTemplate.OptionInterface;
using static VoidTemplate.SaveManager;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
	internal static class EncounterIL
	{
		public static void Hook()
		{
			IL.SaveState.GhostEncounter += SaveState_GhostEncounterIL;
		}

		private static void SaveState_GhostEncounterIL(ILContext il)
		{

			ILCursor c2 = new ILCursor(il);
			if (c2.TryGotoNext(MoveType.Before,
				i => i.MatchStfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.karma)))))
			{
				c2.Emit(OpCodes.Ldarg_0);
				c2.Emit(OpCodes.Ldarg_1);
				c2.EmitDelegate(KarmaRefillControl);
			}
			else
			{
				_Plugin.logger.LogError("Ghost encounter karma update match failed. The Void's echo vs karma interaction will be broken.");
				return;
			}

			if (c2.TryGotoPrev(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<SaveState>(nameof(SaveState.deathPersistentSaveData)),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<SaveState>(nameof(SaveState.deathPersistentSaveData)),
                i => i.MatchLdfld<DeathPersistentSaveData>(nameof(DeathPersistentSaveData.karmaCap))))
			{
				List<ILLabel> beginLabels = c2.IncomingLabels.ToList();

                c2.Emit(OpCodes.Ldarg_0);

				foreach (ILLabel beginLabel in beginLabels)
				{
					beginLabel.Target = c2.Previous;
				}

                c2.Emit(OpCodes.Ldarg_1);
                c2.EmitDelegate(HandleVoidGhostEncounter);
            }
			else
			{
				logerr("Failed to match for ghost encounter karma update. Void will fail to receive karma cap of 10 from MS Ghost and extra cycles from other ghosts.");
			}
		}

		private static void HandleVoidGhostEncounter(SaveState saveState, GhostWorldPresence.GhostID ghostId)
		{
			if (saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
			{
				if (ghostId == MoreSlugcatsEnums.GhostID.MS)
				{
					saveState.deathPersistentSaveData.karmaCap = 10;
					ExternalSaveData.VoidKarma11 = true;
                }
				else
				{
					saveState.SetVoidExtraCycles(saveState.GetVoidExtraCycles() + OptionAccessors.EchoDeathCycle);
				}
			}
        }

		private static int KarmaRefillControl(int unmodifiedNewKarma, SaveState self, GhostWorldPresence.GhostID ghostID)
		{
			if (self.saveStateNumber == VoidEnums.SlugcatID.Void
				&& ghostID != MoreSlugcatsEnums.GhostID.MS)
			{
				return Custom.IntClamp(self.deathPersistentSaveData.karma + 1, 0, self.deathPersistentSaveData.karmaCap);
            }

			return unmodifiedNewKarma;
		}
	}
}
