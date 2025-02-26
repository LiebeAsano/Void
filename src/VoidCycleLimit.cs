using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.OptionInterface;

namespace VoidTemplate
{
    internal static class VoidCycleLimit
    {
        public static int GetVoidCycleLimit(SaveState saveState)
        {
            return OptionAccessors.PermaDeathCycle + saveState.GetVoidExtraCycles();
        }

        public static bool GetCycleLimitLifted(SaveState saveState)
        {
            return saveState.deathPersistentSaveData.karmaCap >= 10 || saveState.miscWorldSaveData.SSaiConversationsHad >= 8;
        }

        public static int GetDisplayCycleNumber(SaveState saveState)
        {
            int actualCycleNumber = saveState.cycleNumber;

            return GetCycleLimitLifted(saveState) ? actualCycleNumber : GetVoidCycleLimit(saveState) - actualCycleNumber;
        }

        public static void Hook()
        {
            On.HUD.Map.Update += Map_Update;

            IL.HUD.Map.CycleLabel.UpdateCycleText += CycleLabel_UpdateCycleText;
            IL.HUD.SubregionTracker.Update += SubregionTracker_Update;

            IL.Menu.DialogBackupSaveInfo.PopulateSaveSlotInfoDisplay += DialogBackupSaveInfo_PopulateSaveSlotInfoDisplay;
            IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
        }

        private static void SlugcatPageContinue_ctor(ILContext il)
        {
            ILCursor insertVoidDisplayCycleNumber = new(il);

            if (insertVoidDisplayCycleNumber.TryGotoNext(
                MoveType.Before,
                op => op.MatchLdloca(3),
                op => op.MatchCall<int>(nameof(int.ToString))))
            {
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldarg_1);
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldarg, 4);
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc_3);

                insertVoidDisplayCycleNumber.EmitDelegate(YieldVoidCycleDisplayNumberWithMainLoopProcess);

                insertVoidDisplayCycleNumber.Emit(OpCodes.Stloc_3);
            }
        }

        private static void DialogBackupSaveInfo_PopulateSaveSlotInfoDisplay(ILContext il)
        {
            ILCursor insertVoidDisplayCycleNumber = new(il);

            if (insertVoidDisplayCycleNumber.TryGotoNext(
                MoveType.After,
                op => op.MatchStloc(5)))
            {
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldarg_0);
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc_3);
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc, 5);

                insertVoidDisplayCycleNumber.EmitDelegate(YieldVoidCycleDisplayNumberWithMainLoopProcess);

                insertVoidDisplayCycleNumber.Emit(OpCodes.Stloc, 5);
            }
        }

        private static void SubregionTracker_Update(ILContext il)
        {
            ILCursor insertVoidDisplayCycleNumber = new(il);

            if (insertVoidDisplayCycleNumber.TryGotoNext(
                MoveType.After,
                op => op.MatchLdfld<SaveState>(nameof(SaveState.cycleNumber)),
                op => op.MatchStloc(3)))
            {
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc_0);
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc_3);

                insertVoidDisplayCycleNumber.EmitDelegate(YieldVoidCycleDisplayNumberWithPlayer);

                insertVoidDisplayCycleNumber.Emit(OpCodes.Stloc_3);
            }
        }

        private static void CycleLabel_UpdateCycleText(ILContext il)
        {
            ILCursor insertVoidDisplayCycleNumber = new(il);

            if (insertVoidDisplayCycleNumber.TryGotoNext(
                MoveType.After,
                op => op.MatchLdfld<SaveState>(nameof(SaveState.cycleNumber)),
                op => op.MatchStloc(1)))
            {
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc_0);
                insertVoidDisplayCycleNumber.Emit(OpCodes.Ldloc_1);

                insertVoidDisplayCycleNumber.EmitDelegate(YieldVoidCycleDisplayNumberWithPlayer);

                insertVoidDisplayCycleNumber.Emit(OpCodes.Stloc_1);
            }
        }

        private static void Map_Update(On.HUD.Map.orig_Update orig, HUD.Map self)
        {
            orig(self);


            if (self.cycleLabel == null && self.mapTexture != null &&
                self.hud.owner is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void && !self.hud.rainWorld.ExpeditionMode)
            {
                self.cycleLabel = new(self);
            }
        }

        private static int YieldVoidCycleDisplayNumberWithPlayer(Player player, int originalCycleNumber)
        {
            SaveState saveState = player.abstractCreature.world.game.GetStorySession.saveState;

            if (saveState.saveStateNumber != VoidEnums.SlugcatID.Void) return originalCycleNumber;

            return GetDisplayCycleNumber(saveState);
        }

        private static int YieldVoidCycleDisplayNumberWithMainLoopProcess(MainLoopProcess mainLoopProcess, SlugcatStats.Name slugcatId, int originalCycleNumber)
        {
            if (slugcatId != VoidEnums.SlugcatID.Void) return originalCycleNumber;

            SaveState saveState = mainLoopProcess.manager.rainWorld.progression.GetOrInitiateSaveState(slugcatId, null, mainLoopProcess.manager.menuSetup, false);

            if (saveState == null) return originalCycleNumber;

            return GetDisplayCycleNumber(saveState);
        }
    }
}
