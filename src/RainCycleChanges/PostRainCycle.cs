using HUD;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.Karma11Features;

namespace VoidTemplate.RainCycleChanges
{
    public static class PostRainCycle
    {
        private static readonly ConditionalWeakTable<RainCycle, RainCycleExt> rainCycleExt = new();
        private static readonly bool[] startMalnourished = new bool[32];

        public static RainCycleExt GetRainCycleExt(this RainCycle rainCycle) =>
            rainCycleExt.GetValue(rainCycle, _ => new RainCycleExt(rainCycle));

        public static void Hook()
        {
            On.RainCycle.Update += RainCycle_Update;
            On.GlobalRain.ResetRain += GlobalRain_ResetRain;
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
            On.GlobalRain.Update += GlobalRain_Update;
        }

        private static void GlobalRain_Update(On.GlobalRain.orig_Update orig, GlobalRain self)
        {
            orig(self);

            RainCycleExt ext = self.game.world.rainCycle.GetRainCycleExt();

            if (ext.PostCycleStarted)
            {
                if (self.TryGetState(out var st) && st.smoothI < 0.5 && self.flood >= 0)
                    self.flood -= 2 * self.floodSpeed;

                self.flood = Mathf.Lerp(self.flood, 0, Mathf.InverseLerp(0.85f, 1f, ext.Progress));
            }
        }

        private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
        {
            World oldWorld = self.activeWorld;
            World newWorld = self.worldLoader?.ReturnWorld();

            orig(self, warpUsed);

            if (oldWorld == null || newWorld == null) return;

            RainCycleExt oldExt = oldWorld.rainCycle.GetRainCycleExt();
            RainCycleExt newExt = newWorld.rainCycle.GetRainCycleExt();

            newExt.timer = oldExt.timer;
            newExt.subtractedFood = oldExt.subtractedFood;
            newExt.stunned = oldExt.stunned;
            newExt.metersReplaced = oldExt.metersReplaced;
            newExt.foodPlanInitialized = oldExt.foodPlanInitialized;
            newExt.fullFoodChangeCycle = oldExt.fullFoodChangeCycle;
            newExt.foodToConsumeThisCycle = oldExt.foodToConsumeThisCycle;
            newExt.starvationRequirement = oldExt.starvationRequirement;
            newExt.consumedRainFoodThisCycle = oldExt.consumedRainFoodThisCycle;
            newExt.postCycleFailed = oldExt.postCycleFailed;
            newExt.cycleFinalized = oldExt.cycleFinalized;
            newExt.starvationReturnDen = oldExt.starvationReturnDen;
            newExt.starvationReturnLastVanillaDen = oldExt.starvationReturnLastVanillaDen;
        }

        private static void GlobalRain_ResetRain(On.GlobalRain.orig_ResetRain orig, GlobalRain self)
        {
            orig(self);

            RainCycleExt postCycle = self.game.world.rainCycle.GetRainCycleExt();

            postCycle.subtractedFood = 0;
            postCycle.stunned = 0;
            postCycle.timer = 0;
            postCycle.metersReplaced = false;
            postCycle.foodPlanInitialized = false;
            postCycle.fullFoodChangeCycle = false;
            postCycle.foodToConsumeThisCycle = 0;
            postCycle.starvationRequirement = false;
            postCycle.consumedRainFoodThisCycle = false;
            postCycle.postCycleFailed = false;
            postCycle.cycleFinalized = false;

            if (self.TryGetState(out var st))
                st.t = 0f;

            for (int i = 0; i < self.game.cameras.Length; i++)
            {
                if (self.game.cameras[i].hud is HUD.HUD { rainMeter: not null } hud && hud.rainMeter.GetAfterCycleMode().Value)
                {
                    hud.rainMeter.slatedForDeletion = true;
                    hud.AddPart(new RainMeter(hud, hud.fContainers[1]));
                }
            }
        }

        private static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
        {
            RainWorldGame game = self.world?.game;

            RainFoodRequirement.Restore(game);

            try
            {
                orig(self);

                if (game?.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void)
                    self.GetRainCycleExt().AfterCycleUpdate();
            }
            finally
            {
                RainFoodRequirement.Refresh(game);
            }
        }

        public class RainCycleExt
        {
            public const int postAfterCyceleTicks = 72000;

            public RainCycle owner;
            public int postCycleLength;
            public int timer;
            public int stunned;
            public int subtractedFood;
            public bool metersReplaced;
            public bool foodPlanInitialized;
            public bool fullFoodChangeCycle;
            public int foodToConsumeThisCycle;
            public bool starvationRequirement;
            public bool consumedRainFoodThisCycle;
            public bool postCycleFailed;
            public bool cycleFinalized;
            public string starvationReturnDen;
            public string starvationReturnLastVanillaDen;
            public OverseersWorldAI.ShelterFinder shelterFinder;

            public GlobalRain GRain
            {
                get
                {
                    return owner.world.game.globalRain;
                }
            }

            public bool PostCycleStarted
            {
                get
                {
                    return GRain.deathRain?.deathRainMode == GlobalRain.DeathRain.DeathRainMode.Mayhem;
                }
            }

            public float AmountLeft
            {
                get
                {
                    return (float)(postCycleLength - timer) / postCycleLength;
                }
            }

            public float Progress
            {
                get
                {
                    return Mathf.InverseLerp(0, postCycleLength, timer);
                }
            }

            public bool TimeToLockShelters
            {
                get
                {
                    return timer > 1200;
                }
            }

            public bool AllowToSubtractFood
            {
                get
                {
                    return timer > 4800 && timer <= postCycleLength - 4800;
                }
            }

            public int TimeToStartNewCycle
            {
                get
                {
                    return postCycleLength - timer;
                }
            }

            public int FoodCost
            {
                get
                {
                    if (foodPlanInitialized)
                        return foodToConsumeThisCycle;

                    if (owner?.world?.game?.session is StoryGameSession session)
                        return Mathf.Max(0, session.characterStats.foodToHibernate);

                    return 0;
                }
            }

            public int RemainingFoodCost
            {
                get
                {
                    return Mathf.Max(0, FoodCost - subtractedFood);
                }
            }

            public RainCycleExt(RainCycle owner)
            {
                this.owner = owner;

                if (owner.cycleLength < 36000)
                    postCycleLength = postAfterCyceleTicks - owner.cycleLength;
                else
                    postCycleLength = Mathf.RoundToInt(owner.cycleLength * Random.Range(1f, 2f));
            }

            public bool ShouldHighlightFoodPip(int pipNumber, int currentFood)
            {
                if (!PostCycleStarted || postCycleFailed) return false;

                int remaining = RemainingFoodCost;

                if (remaining <= 0) return false;

                currentFood = Mathf.Max(0, currentFood);

                int firstRed;
                int lastRedExclusive;

                if (currentFood >= remaining)
                {
                    firstRed = currentFood - remaining;
                    lastRedExclusive = currentFood;
                }
                else
                {
                    firstRed = 0;
                    lastRedExclusive = remaining;
                }

                return pipNumber >= firstRed && pipNumber < lastRedExclusive;
            }

            private void SnapshotStartMalnourished(RainWorldGame game)
            {
                StoryGameSession session = game.GetStorySession;

                for (int i = 0; i < startMalnourished.Length; i++)
                    startMalnourished[i] = false;

                for (int i = 0; i < game.Players.Count; i++)
                {
                    AbstractCreature absPlayer = game.Players[i];

                    if (absPlayer?.state is not PlayerState playerState)
                        continue;

                    int playerNumber = playerState.playerNumber;

                    if (playerNumber < 0 || playerNumber >= startMalnourished.Length)
                        continue;

                    if (absPlayer.realizedCreature is Player player)
                    {
                        startMalnourished[playerNumber] = player.Malnourished;
                        continue;
                    }

                    SlugcatStats stats = null;

                    if (session.characterStatsJollyplayer != null && playerNumber < session.characterStatsJollyplayer.Length)
                        stats = session.characterStatsJollyplayer[playerNumber];

                    stats ??= session.characterStats;

                    startMalnourished[playerNumber] =
                        stats != null && (stats.malnourished || stats.malnourishedByCreature);
                }
            }

            private void InitializeFoodPlan()
            {
                if (foodPlanInitialized) return;

                RainWorldGame game = owner.world.game;

                if (game.GetStorySession == null) return;

                AbstractCreature absPlayer = game.FirstAlivePlayer ?? game.FirstAnyPlayer;

                if (absPlayer?.state is not PlayerState playerState) return;

                StoryGameSession session = game.GetStorySession;
                SaveState save = session.saveState;

                SnapshotStartMalnourished(game);

                starvationRequirement = false;
                consumedRainFoodThisCycle = false;
                postCycleFailed = false;
                cycleFinalized = false;

                int maxFood = session.characterStats.maxFood;

                if (absPlayer.realizedCreature is Player player)
                    maxFood = player.slugcatStats.maxFood;

                fullFoodChangeCycle = save.VoidFullAnd11Karma(playerState.foodInStomach, 0, maxFood);

                if (fullFoodChangeCycle)
                    foodToConsumeThisCycle = maxFood;
                else
                    foodToConsumeThisCycle = RainFoodRequirement.GetNormalFoodToHibernate(session.characterStats);

                foodToConsumeThisCycle = Mathf.Max(0, foodToConsumeThisCycle);
                foodPlanInitialized = true;
            }

            private void RestoreMaxFood(RainWorldGame game, int maxFood)
            {
                StoryGameSession session = game.GetStorySession;

                if (session.characterStats != null && session.characterStats.name == VoidEnums.SlugcatID.Void)
                {
                    session.characterStats.maxFood = maxFood;
                    session.characterStats.foodToHibernate = maxFood;
                }

                if (session.characterStatsJollyplayer != null)
                {
                    for (int i = 0; i < session.characterStatsJollyplayer.Length; i++)
                    {
                        SlugcatStats stats = session.characterStatsJollyplayer[i];

                        if (stats == null || stats.name != VoidEnums.SlugcatID.Void)
                            continue;

                        stats.maxFood = maxFood;
                        stats.foodToHibernate = maxFood;
                    }
                }

                for (int i = 0; i < game.Players.Count; i++)
                {
                    if (game.Players[i]?.realizedCreature is not Player player || player.slugcatStats.name != VoidEnums.SlugcatID.Void)
                        continue;

                    player.slugcatStats.maxFood = maxFood;
                    player.slugcatStats.foodToHibernate = maxFood;
                }
            }

            private void CaptureStarvationReturnDen(SaveState saveState)
            {
                if (!string.IsNullOrEmpty(starvationReturnDen))
                    return;

                starvationReturnDen = saveState.denPosition;
                starvationReturnLastVanillaDen = saveState.lastVanillaDen;
            }

            private void RestoreStarvationReturnDen(RainWorldGame game)
            {
                if (game?.GetStorySession?.saveState == null || string.IsNullOrEmpty(starvationReturnDen))
                    return;

                SaveState saveState = game.GetStorySession.saveState;

                saveState.denPosition = starvationReturnDen;

                if (starvationReturnLastVanillaDen != null)
                    saveState.lastVanillaDen = starvationReturnLastVanillaDen;

                if (game.rainWorld?.progression != null)
                    game.rainWorld.progression.currentSaveState = saveState;
            }

            private void UpdateFoodConsumption(Player player)
            {
                if (!AllowToSubtractFood || !foodPlanInitialized || foodToConsumeThisCycle <= 0
                    || player == null || !player.playerState.alive || player.dead || postCycleFailed)
                    return;

                const int startOffset = 4800;
                int end = postCycleLength - 4800;
                int duration = Mathf.Max(1, end - startOffset);
                int elapsed = Mathf.Clamp(timer - startOffset, 0, duration);
                int shouldBeConsumed = Mathf.FloorToInt((float)elapsed / duration * foodToConsumeThisCycle);

                if (timer >= end)
                    shouldBeConsumed = foodToConsumeThisCycle;

                while (subtractedFood < shouldBeConsumed)
                {
                    if (!ConsumeOneFood(player))
                        return;

                    subtractedFood++;

                    if (postCycleFailed || player.dead || !player.playerState.alive)
                        return;
                }
            }

            private bool ConsumeOneFood(Player player)
            {
                RainWorldGame game = owner.world.game;
                SaveState saveState = game.GetStorySession.saveState;

                if (player.FoodInStomach > 0)
                {
                    int totalFood = saveState.totFood;

                    player.SubtractFood(1);

                    saveState.totFood = totalFood;
                    consumedRainFoodThisCycle = true;

                    return true;
                }

                int playerNumber = player.playerState.playerNumber;

                bool startedMalnourished =
                    playerNumber >= 0
                    && playerNumber < startMalnourished.Length
                    && startMalnourished[playerNumber];
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
                if (!saveState.GetVoidMarkV3() || startedMalnourished || !consumedRainFoodThisCycle)
                {
                    KillFromRainStarvation(player);
                    return false;
                }

                CaptureStarvationReturnDen(saveState);
                starvationRequirement = true;

                if (!player.Malnourished)
                {
                    int maxFood = player.slugcatStats.maxFood;

                    stunned = Mathf.Max(stunned, 1);

                    KarmaFlowerChanges.RemoveVoidKarmaProtection(game);

                    player.Stun(40);
                    player.SetMalnourished(true);

                    RestoreMaxFood(game, maxFood);
                    RainFoodRequirement.Refresh(game);

                    return true;
                }

                stunned++;
                player.stun += 40 * stunned;

                return true;
            }

            private void KillFromRainStarvation(Player player)
            {
                RainWorldGame game = owner.world.game;

                postCycleFailed = true;
                starvationRequirement = false;

                RestoreStarvationReturnDen(game);
                RainFoodRequirement.Restore(game);

                player.Die();
            }

            public void AfterCycleUpdate()
            {
                if (postCycleFailed || cycleFinalized)
                    return;

                if (shelterFinder == null)
                {
                    shelterFinder = new(owner.world);
                    new Thread(StartToMapThread).Start();
                }

                if (!PostCycleStarted) return;

                InitializeFoodPlan();

                if (postCycleFailed || !shelterFinder.done)
                    return;

                if (!metersReplaced)
                {
                    for (int i = 0; i < owner.world.game.cameras.Length; i++)
                    {
                        if (owner.world.game.cameras[i].hud is HUD.HUD { rainMeter: not null } hud
                            && !hud.rainMeter.GetAfterCycleMode().Value)
                        {
                            hud.rainMeter.slatedForDeletion = true;
                            hud.AddPart(new RainMeter(hud, hud.fContainers[1]));
                        }
                    }

                    metersReplaced = true;
                }

                timer++;

                RainWorldGame game = owner.world.game;
                AbstractCreature absPlayer = game.FirstAlivePlayer;

                if (absPlayer == null || !absPlayer.state.alive)
                {
                    FailPostCycle(game);
                    return;
                }

                if (absPlayer.realizedCreature is Player player)
                {
                    if (player.dead || !player.playerState.alive)
                    {
                        FailPostCycle(game);
                        return;
                    }

                    UpdateFoodConsumption(player);

                    if (postCycleFailed || player.dead || !player.playerState.alive)
                        return;
                }

                if (TimeToStartNewCycle > 0)
                    return;

                absPlayer = game.FirstAlivePlayer;

                if (absPlayer == null || !absPlayer.state.alive)
                {
                    FailPostCycle(game);
                    return;
                }

                if (absPlayer.realizedCreature is Player livePlayer
                    && (livePlayer.dead || !livePlayer.playerState.alive))
                {
                    FailPostCycle(game);
                    return;
                }

                if (!PermadeathConditions.TryPrepareVoidCycleAdvance(game))
                {
                    cycleFinalized = true;
                    return;
                }

                absPlayer = game.FirstAlivePlayer;

                if (absPlayer == null || !absPlayer.state.alive)
                {
                    FailPostCycle(game);
                    return;
                }

                if (absPlayer.realizedCreature is Player survivingPlayer
                    && (survivingPlayer.dead || !survivingPlayer.playerState.alive))
                {
                    FailPostCycle(game);
                    return;
                }

                cycleFinalized = true;

                bool malnourished = CurrentMalnourished(game);

                bool applyFoodProgression =
                    fullFoodChangeCycle
                    && foodPlanInitialized
                    && subtractedFood >= foodToConsumeThisCycle;

                if (applyFoodProgression)
                    FoodChange.ApplyFullFoodProgression(game.GetStorySession.saveState);

                string returnDen = starvationReturnDen;
                string returnLastVanillaDen = starvationReturnLastVanillaDen;

                float newCycleLength = Mathf.Lerp(
                    game.rainWorld.setup.cycleTimeMin,
                    game.rainWorld.setup.cycleTimeMax,
                    Random.value);

                RainCycle newRainCycle = new(owner.world, newCycleLength);

                owner.world.rainCycle = newRainCycle;

                if (newRainCycle.maxPreTimer > 0)
                {
                    newRainCycle.maxPreTimer = 0;
                    newRainCycle.preTimer = 0;
                    newRainCycle.preCycleRainPulse_WaveC = 0;

                    GRain.preCycleRainPulse_Scale = 0;
                    GRain.drainWorldFlood = 0;
                }

                GRain.ResetRain();

                RainCycleExt newExt = newRainCycle.GetRainCycleExt();

                newExt.shelterFinder = shelterFinder;

                if (malnourished)
                {
                    newExt.starvationReturnDen = returnDen;
                    newExt.starvationReturnLastVanillaDen = returnLastVanillaDen;
                }

                WaterGateHooks.RestoreAfterPostRain(owner.world);

                for (int i = 0; i < owner.world.abstractRooms.Length; i++)
                {
                    AbstractRoom room = owner.world.abstractRooms[i];

                    for (int j = 0; j < room.creatures.Count; j++)
                        room.creatures[j].state.CycleTick();

                    for (int j = 0; j < room.entitiesInDens.Count; j++)
                    {
                        if (room.entitiesInDens[j] is AbstractCreature crit)
                            crit.state.CycleTick();
                    }
                }

                for (int i = 0; i < owner.world.activeRooms.Count; i++)
                {
                    Room room = owner.world.activeRooms[i];

                    if (!room.ReadyForPlayer) continue;

                    for (int j = room.lockedShortcuts.Count - 1; j >= 0; j--)
                    {
                        ShortcutData shortcut = room.shortcutData(room.lockedShortcuts[j]);

                        if (shortcut.shortCutType != ShortcutData.Type.RoomExit)
                            continue;

                        AbstractRoom leadingRoom = room.world.GetAbstractRoom(
                            room.abstractRoom.connections[shortcut.destNode]);

                        if (leadingRoom != null && leadingRoom.shelter
                            && !leadingRoom.world.brokenShelters[leadingRoom.shelterIndex])
                            room.lockedShortcuts.RemoveAt(j);
                    }
                }

                absPlayer = game.FirstAlivePlayer;

                if (absPlayer == null || !absPlayer.state.alive)
                {
                    FailPostCycle(game);
                    return;
                }

                if (absPlayer.realizedCreature is Player finalPlayer
                    && (finalPlayer.dead || !finalPlayer.playerState.alive))
                {
                    FailPostCycle(game);
                    return;
                }

                SaveProgress();

                if (postCycleFailed)
                    return;

                FoodChange.RefreshLiveFoodStats(game);

                for (int i = 0; i < game.cameras.Length; i++)
                {
                    if (game.cameras[i].hud is HUD.HUD { karmaMeter: var karmaMeter })
                        karmaMeter.reinforceAnimation = 1;
                }
            }

            private void FailPostCycle(RainWorldGame game)
            {
                postCycleFailed = true;
                starvationRequirement = false;

                RestoreStarvationReturnDen(game);
                RainFoodRequirement.Restore(game);
            }

            private bool CurrentMalnourished(RainWorldGame game)
            {
                if (starvationRequirement)
                    return true;

                AbstractCreature absPlayer = game.FirstAlivePlayer ?? game.FirstAnyPlayer;

                if (absPlayer?.realizedCreature is Player player)
                    return player.Malnourished;

                StoryGameSession session = game.GetStorySession;

                if (absPlayer?.state is PlayerState playerState
                    && session.characterStatsJollyplayer != null
                    && playerState.playerNumber >= 0
                    && playerState.playerNumber < session.characterStatsJollyplayer.Length
                    && session.characterStatsJollyplayer[playerState.playerNumber] is SlugcatStats stats)
                    return stats.malnourished || stats.malnourishedByCreature;

                return session.characterStats.malnourished
                    || session.characterStats.malnourishedByCreature;
            }

            public void SaveProgress()
            {
                RainWorldGame game = owner.world?.game;

                if (postCycleFailed || game?.GetStorySession == null)
                    return;

                AbstractCreature absPlayer = game.FirstAlivePlayer;

                if (absPlayer == null || !absPlayer.state.alive)
                {
                    postCycleFailed = true;
                    return;
                }

                if (absPlayer.realizedCreature is Player player
                    && (player.dead || !player.playerState.alive))
                {
                    postCycleFailed = true;
                    return;
                }

                StoryGameSession session = game.GetStorySession;
                SaveState saveState = session.saveState;

                if (saveState == null)
                    return;

                bool malnourished = CurrentMalnourished(game);

                saveState.lastMalnourished = saveState.malnourished;
                saveState.malnourished = malnourished;

                saveState.cycleNumber++;
                saveState.cyclesInCurrentWorldVersion++;

                for (int i = 0; i < session.playerSessionRecords.Length; i++)
                {
                    PlayerSessionRecord record = session.playerSessionRecords[i];

                    if (record?.kills != null && record.kills.Count > 0)
                        saveState.AppendKills(record.kills);
                }

                session.AppendTimeOnCycleEnd(false);

                RainWorld.lockGameTimer = false;

                saveState.deathPersistentSaveData.survives++;
                saveState.deathPersistentSaveData.winState.CycleCompleted(game);

                if (saveState.deathPersistentSaveData.karma
                    < saveState.deathPersistentSaveData.karmaCap)
                    saveState.deathPersistentSaveData.karma++;

                if (malnourished)
                {

                    CaptureStarvationReturnDen(saveState);

                    string returnDen = starvationReturnDen;
                    string returnLastVanillaDen = starvationReturnLastVanillaDen;

                    saveState.deathPersistentSaveData.reinforcedKarma = false;

                    PlayerProgression progression = game.rainWorld.progression;
                    progression.currentSaveState = saveState;

                    saveState.BringUpToDate(game);

                    if (!string.IsNullOrEmpty(returnDen))
                        saveState.denPosition = returnDen;

                    if (returnLastVanillaDen != null)
                        saveState.lastVanillaDen = returnLastVanillaDen;

                    try
                    {
                        progression.SaveWorldStateAndProgression(true);
                    }
                    finally
                    {
                        progression.currentSaveState = saveState;

                        if (!string.IsNullOrEmpty(returnDen))
                            saveState.denPosition = returnDen;

                        if (returnLastVanillaDen != null)
                            saveState.lastVanillaDen = returnLastVanillaDen;
                    }
                }
                else
                {
                    starvationReturnDen = null;
                    starvationReturnLastVanillaDen = null;

                    RainWorldGame.ForceSaveNewDenLocation(
                        game,
                        ComputeNearestShelter(),
                        true);
                }

                ResetSessionRecords(game);
                UpdateCycleHUD(game);
            }

            private void ResetSessionRecords(RainWorldGame game)
            {
                StoryGameSession session = game.GetStorySession;

                for (int i = 0; i < game.Players.Count; i++)
                {
                    if (game.Players[i]?.state is not PlayerState playerState)
                        continue;

                    session.playerSessionRecords[playerState.playerNumber] =
                        new PlayerSessionRecord(playerState.playerNumber);
                }

                if (!game.world.singleRoomWorld && session.playerSessionRecords[0] != null)
                    session.playerSessionRecords[0].wokeUpInRegion = game.world.region.name;
            }

            private void UpdateCycleHUD(RainWorldGame game)
            {
                for (int i = 0; i < game.cameras.Length; i++)
                {
                    if (game.cameras[i].hud?.map?.cycleLabel == null)
                        continue;

                    game.cameras[i].hud.map.cycleLabel.UpdateCycleText();
                }
            }

            public string ComputeNearestShelter()
            {
                AbstractCreature player =
                    owner.world.game.FirstAlivePlayer ?? owner.world.game.FirstAnyPlayer;

                if (player != null && player.Room != null)
                {
                    float minDistance = float.MaxValue;
                    int nearestShelterIndex = -1;

                    for (int i = 0; i < player.world.shelters.Length; i++)
                    {
                        for (int j = 0; j < player.Room.connections.Length; j++)
                        {
                            float distance = shelterFinder.DistanceToShelter(
                                i,
                                new WorldCoordinate(player.Room.index, -1, -1, j));

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                nearestShelterIndex = i;
                            }
                        }
                    }

                    if (nearestShelterIndex > -1)
                        return owner.world.GetAbstractRoom(owner.world.shelters[nearestShelterIndex]).name;
                }

                return owner.world.GetAbstractRoom(
                    owner.world.shelters[Random.Range(0, owner.world.shelters.Length)]).name;
            }

            public void StartToMapThread()
            {
                for (; ; )
                {
                    if (shelterFinder.done) break;
                    shelterFinder.Update();
                }
            }
        }
    }
}