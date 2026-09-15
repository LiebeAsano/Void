using Menu;
using RWCustom;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static VoidTemplate.Useful.Utils;
using ReadStatus = VoidTemplate.CampaignStatisticsStore.ReadStatus;
using Snapshot = VoidTemplate.CampaignStatisticsStore.Snapshot;

namespace VoidTemplate;

public static class CampaignStatisticsSave
{
    private sealed class ScreenState
    {
        public bool FromMenu;
        public bool ArchiveView;
        public bool Delivered;
        public bool CaptureAttempted;
        public bool Failed;
        public bool WarningShown;
        public SaveState ReplaySave;
    }

    private static readonly ConditionalWeakTable<StoryGameStatisticsScreen, ScreenState> screenStates = new();
    private static bool hooked;

    public static bool IsRestoring { get; private set; }

    public static void Hook()
    {
        if (hooked) return;
        hooked = true;

        On.Menu.StoryGameStatisticsScreen.GetDataFromGame += StoryGameStatisticsScreen_GetDataFromGame;
        On.Menu.StoryGameStatisticsScreen.CommunicateWithUpcomingProcess += StoryGameStatisticsScreen_CommunicateWithUpcomingProcess;
        On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess += SlugcatSelectMenu_CommunicateWithUpcomingProcess;
        On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
        On.StoryGameSession.ctor += StoryGameSession_ctor;
    }

    private static bool Eligible(PlayerProgression progression)
    {
        return progression?.rainWorld?.options != null
            && !progression.rainWorld.ExpeditionMode
            && !progression.rainWorld.safariMode
            && progression.rainWorld.options.saveSlot >= 0;
    }

    private static bool IsKnownTerminal(SaveState save)
    {
        if (save?.deathPersistentSaveData == null) return false;
        if (save.saveStateNumber == VoidEnums.SlugcatID.Void) return save.GetVoidCatDead();

        return save.saveStateNumber == SlugcatStats.Name.Red && save.deathPersistentSaveData.redsDeath;
    }

    private static SlugcatStats.Name SelectedCampaign(SlugcatSelectMenu menu)
    {
        if (menu?.slugcatPages == null || menu.slugcatPageIndex < 0 || menu.slugcatPageIndex >= menu.slugcatPages.Count) return null;
        return menu.slugcatPages[menu.slugcatPageIndex].slugcatNumber;
    }

    public static bool Capture(SaveState save)
    {
        if (save?.saveStateNumber == null || !Eligible(save.progression)) return false;

        int slot = save.progression.rainWorld.options.saveSlot;

        try
        {
            bool success = CampaignStatisticsStore.Write(slot, save.saveStateNumber, save.SaveToString(), IsKnownTerminal(save), out string error);

            if (!success)
            {
                LogExErr($"[CampaignStatistics] Save failed: slot={slot}, campaign={save.saveStateNumber.value}. {error}");
                return false;
            }

            LogResult("Saved", slot, save);
            return true;
        }
        catch (Exception exception)
        {
            LogExErr($"[CampaignStatistics] Serialization failed: {exception}");
            return false;
        }
    }

    public static bool TryLoad(PlayerProgression progression, SlugcatStats.Name campaign, out SaveState save, out bool snapshotFound)
    {
        save = null;
        snapshotFound = false;

        if (!Eligible(progression) || campaign == null) return false;

        int slot = progression.rainWorld.options.saveSlot;
        ReadStatus status = CampaignStatisticsStore.Read(slot, campaign, out Snapshot snapshot, out string error);

        snapshotFound = status != ReadStatus.Missing;

        if (status == ReadStatus.Missing) return false;

        if (status == ReadStatus.Error)
        {
            LogExErr($"[CampaignStatistics] Load failed: slot={slot}, campaign={campaign.value}. {error}");
            return false;
        }

        try
        {
            save = Restore(progression, campaign, snapshot.SaveData);
            LogResult("Loaded", slot, save);
            return true;
        }
        catch (Exception exception)
        {
            LogExErr($"[CampaignStatistics] Restore failed: slot={slot}, campaign={campaign.value}. {exception}");
            return false;
        }
    }

    private static SaveState Restore(PlayerProgression progression, SlugcatStats.Name campaign, string data)
    {
        var randomState = UnityEngine.Random.state;
        int loadedWorldVersion = RainWorld.loadedWorldVersion;
        bool wasRestoring = IsRestoring;

        IsRestoring = true;

        try
        {
            SaveState restored = new(campaign, progression);
            restored.LoadGame(data, null);

            if (restored.saveStateNumber != campaign || restored.deathPersistentSaveData == null)
                throw new InvalidOperationException("The restored statistics belong to another campaign.");

            return restored;
        }
        finally
        {
            IsRestoring = wasRestoring;
            UnityEngine.Random.state = randomState;
            RainWorld.loadedWorldVersion = loadedWorldVersion;
        }
    }

    public static void Clear(PlayerProgression progression, SlugcatStats.Name campaign)
    {
        if (!Eligible(progression) || campaign == null) return;

        int slot = progression.rainWorld.options.saveSlot;

        if (!CampaignStatisticsStore.Clear(slot, campaign, out string error))
            LogExErr($"[CampaignStatistics] Clear failed: slot={slot}, campaign={campaign.value}. {error}");
    }

    public static bool IsArchiveView(StoryGameStatisticsScreen screen)
    {
        return screen != null && screenStates.TryGetValue(screen, out ScreenState state) && state.ArchiveView;
    }

    public static bool TryOpenFromMainButton(SlugcatSelectMenu menu, SlugcatStats.Name campaign)
    {
        if (menu == null || campaign == null || menu.restartChecked || !Eligible(menu.manager.rainWorld.progression)) return false;
        if (SelectedCampaign(menu) != campaign || menu.startButton?.menuLabel?.text != menu.Translate("STATISTICS")) return false;

        if (!TryLoad(menu.manager.rainWorld.progression, campaign, out SaveState save, out _))
        {
            return true;
        }

        if (menu.manager.upcomingProcess != null) return true;

        menu.redSaveState = save;
        RainWorld.lastActiveSaveSlot = campaign;
        menu.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = campaign;

        menu.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
        menu.PlaySound(SoundID.MENU_Switch_Page_Out);
        return true;
    }

    private static void StoryGameStatisticsScreen_GetDataFromGame(On.Menu.StoryGameStatisticsScreen.orig_GetDataFromGame orig, StoryGameStatisticsScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        if (!Eligible(self.manager.rainWorld.progression))
        {
            orig(self, package);
            return;
        }

        ScreenState state = screenStates.GetValue(self, _ => new ScreenState());

        if (state.Delivered || state.Failed) return;

        if (state.FromMenu)
        {
            if (state.ReplaySave == null)
            {
                RejectStatistics(self, state);
                return;
            }

            state.ArchiveView = true;
            state.Delivered = true;

            orig(self, CreatePackage(state.ReplaySave));
            return;
        }

        if (package?.saveState?.saveStateNumber == null || package.saveState.deathPersistentSaveData == null)
        {
            RejectStatistics(self, state);
            return;
        }

        bool written = true;

        if (!state.CaptureAttempted)
        {
            state.CaptureAttempted = true;
            written = Capture(package.saveState);
        }

        state.Delivered = true;
        orig(self, package);

    }

    private static KarmaLadderScreen.SleepDeathScreenDataPackage CreatePackage(SaveState save)
    {
        DeathPersistentSaveData data = save.deathPersistentSaveData;
        IntVector2 karma = new(data.karma, data.karmaCap);

        if (ModManager.Watcher && data.rippleLevel >= 1f)
        {
            int level = (int)((Mathf.Max(1f, data.rippleLevel) - 1f) * 2f);
            karma = new IntVector2(level, 100 + level);
        }

        return new KarmaLadderScreen.SleepDeathScreenDataPackage(
            save.food,
            karma,
            data.reinforcedKarma,
            -1,
            Vector2.zero,
            null,
            save,
            new SlugcatStats(save.saveStateNumber, save.malnourished),
            new PlayerSessionRecord(0),
            save.lastMalnourished,
            save.malnourished);
    }

    private static void SlugcatSelectMenu_CommunicateWithUpcomingProcess(On.Menu.SlugcatSelectMenu.orig_CommunicateWithUpcomingProcess orig, SlugcatSelectMenu self, MainLoopProcess nextProcess)
    {
        if (nextProcess is not StoryGameStatisticsScreen statistics || !Eligible(self.manager.rainWorld.progression))
        {
            orig(self, nextProcess);
            return;
        }

        ScreenState state = screenStates.GetValue(statistics, _ => new ScreenState());
        state.FromMenu = true;

        SlugcatStats.Name campaign = self.redSaveState?.saveStateNumber ?? SelectedCampaign(self);
        SaveState replay = null;

        if (campaign != null)
            TryLoad(self.manager.rainWorld.progression, campaign, out replay, out _);

        if (replay == null)
        {
            RejectStatistics(statistics, state);
            return;
        }

        state.ArchiveView = true;
        state.ReplaySave = replay;

        self.redSaveState = replay;
        RainWorld.lastActiveSaveSlot = replay.saveStateNumber;

        orig(self, nextProcess);

        if (!state.Delivered && !state.Failed)
            statistics.GetDataFromGame(CreatePackage(replay));
    }

    private static void StoryGameStatisticsScreen_CommunicateWithUpcomingProcess(On.Menu.StoryGameStatisticsScreen.orig_CommunicateWithUpcomingProcess orig, StoryGameStatisticsScreen self, MainLoopProcess nextProcess)
    {
        bool archiveView = screenStates.TryGetValue(self, out ScreenState state) && state.FromMenu;

        if (archiveView && nextProcess is SlugcatSelectMenu menu)
        {
            menu.UpdateStartButtonText();
            return;
        }

        orig(self, nextProcess);
    }

    private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
    {
        bool endingCleanup = self.rainWorld?.processManager?.currentMainLoop is StoryGameStatisticsScreen statistics
            && statistics.saveState?.saveStateNumber == saveStateNumber;

        orig(self, saveStateNumber);

        if (!endingCleanup)
            Clear(self, saveStateNumber);
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        bool newGame = Eligible(game.rainWorld.progression)
            && !game.wasAnArtificerDream
            && game.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.New;

        orig(self, saveStateNumber, game);

        if (newGame)
            Clear(game.rainWorld.progression, saveStateNumber);
    }

    private static void RejectStatistics(StoryGameStatisticsScreen screen, ScreenState state)
    {
        if (state.Failed) return;

        state.Failed = true;

        LogExErr("[CampaignStatistics] Statistics has no valid archived data source.");

        const string text = "The statistics archive is missing or could not be read.\nReturn to character selection.";

        screen.manager.ShowDialog(new DialogNotify(
            screen.Translate(text),
            new Vector2(640f, 180f),
            screen.manager,
            () => screen.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlugcatSelect)));
    }

    private static void LogResult(string action, int slot, SaveState save)
    {
        _Plugin.logger.LogInfo(
            $"[CampaignStatistics] {action}: slot={slot}, campaign={save.saveStateNumber.value}, cycle={save.cycleNumber}, food={save.totFood}, time={save.totTime}, survives={save.deathPersistentSaveData.survives}, deaths={save.deathPersistentSaveData.deaths}");
    }
}