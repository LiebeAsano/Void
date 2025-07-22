using HUD;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics;
using static HUD.Map;
using static Menu.SlugcatSelectMenu;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate
{
    public static class VoidCycleLimit
    {
        public static int GetVoidCycleLimit(SaveState saveState)
        {
            return OptionAccessors.PermaDeathCycle + saveState.GetVoidExtraCycles();
        }

        public static bool GetCycleLimitLifted(SaveState saveState)
        {
            return saveState.deathPersistentSaveData.karmaCap >= 10 || saveState.GetVoidMarkV3();
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
            On.HUD.SubregionTracker.Update += SubregionTracker_Update;

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
            else
            {
                logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(VoidCycleLimit)}.{nameof(SlugcatPageContinue_ctor)}: first match failed");
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
            else
            {
                logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(VoidCycleLimit)}.{nameof(DialogBackupSaveInfo_PopulateSaveSlotInfoDisplay)}: first match failed");
            }
        }

        private static void SubregionTracker_Update(On.HUD.SubregionTracker.orig_Update orig, HUD.SubregionTracker self)
        {
            Player player = self.textPrompt.hud.owner as Player;
            int num = 0;
            if (player.room != null && !player.room.world.singleRoomWorld && player.room.world.region != null)
            {
                for (int i = 1; i < player.room.world.region.subRegions.Count; i++)
                {
                    if (player.room.abstractRoom.subregionName == player.room.world.region.subRegions[i])
                    {
                        num = i;
                        break;
                    }
                }
            }
                if (!self.DEVBOOL && num != 0 && player.room.game.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.Dev)
                {
                    self.lastShownRegion = num;
                    self.DEVBOOL = true;
                }
                if (num != self.lastShownRegion && player.room != null && num != 0 && self.lastRegion == num && self.textPrompt.show == 0f)
                {
                if (player.room.world.game.IsVoidStoryCampaign())
                {
                    bool flag = false;
                    for (int j = 0; j < player.room.warpPoints.Count; j++)
                    {
                        if (player.room.warpPoints[j].timeWarpTearClosed <= 20)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag || self.counter == 1 || self.counter == 75)
                    {
                        self.counter++;
                    }
                    if (self.counter > 80)
                    {
                        if ((num > 1 || self.lastShownRegion == 0 || (player.room.world.region.name != "SS" && player.room.world.region.name != "DM")) && num < player.room.world.region.subRegions.Count)
                        {
                            if (self.showCycleNumber && player.room.game.IsStorySession && player.room.game.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.Load)
                            {
                                int num2 = YieldVoidCycleDisplayNumberWithPlayer(player, player.room.game.GetStorySession.saveState.cycleNumber);
                                string s = player.room.world.region.subRegions[num];
                                if (num < player.room.world.region.altSubRegions.Count && player.room.world.region.altSubRegions[num] != null)
                                {
                                    s = player.room.world.region.altSubRegions[num];
                                }
                                self.textPrompt.AddMessage(string.Concat(new string[]
                                {
                                self.textPrompt.hud.rainWorld.inGameTranslator.Translate("Cycle"),
                                " ",
                                num2.ToString(),
                                " ~ ",
                                self.textPrompt.hud.rainWorld.inGameTranslator.Translate(s)
                                }), 0, 160, false, true);
                            }
                            else
                            {
                                string s2 = player.room.world.region.subRegions[num];
                                if (num < player.room.world.region.altSubRegions.Count && player.room.world.region.altSubRegions[num] != null)
                                {
                                    s2 = player.room.world.region.altSubRegions[num];
                                }
                                self.textPrompt.AddMessage(self.textPrompt.hud.rainWorld.inGameTranslator.Translate(s2), 0, 160, false, true);
                            }
                        }
                        self.showCycleNumber = false;
                        self.lastShownRegion = num;
                    }
                }
                else
                {
                    self.counter = 0;
                }
                self.lastRegion = num;
            }
            else
            {
                orig(self);
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
            else
            {
                logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(VoidCycleLimit)}.{nameof(CycleLabel_UpdateCycleText)}: first match failed");
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

        public static int YieldVoidCycleDisplayNumberWithPlayer(Player player, int originalCycleNumber)
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
