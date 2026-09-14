using HUD;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;

namespace VoidTemplate.RainCycleChanges
{
    public static class PostRainCycle
    {
        private static readonly ConditionalWeakTable<RainCycle, RainCycleExt> rainCycleExt = new();

        public static RainCycleExt GetRainCycleExt(this RainCycle rainCycle) =>
            rainCycleExt.GetValue(rainCycle, _ => new RainCycleExt(rainCycle));


        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
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

        private static readonly bool[] startMalnourished = new bool[32];

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            startMalnourished[self.playerState.playerNumber] = self.Malnourished;
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
            orig(self);

            if (self.world.game.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void)
                self.GetRainCycleExt().AfterCycleUpdate();
        }

        public class RainCycleExt
        {
            public const int postAfterCyceleTicks =
                72000;

            public RainCycle owner;

            public int postCycleLength;

            public int timer;

            public int stunned;

            public int subtractedFood;

            public bool metersReplaced;

            public bool foodPlanInitialized;

            public bool fullFoodChangeCycle;

            public int foodToConsumeThisCycle;

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
                    return timer > 2400;
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
                    if (foodPlanInitialized) return foodToConsumeThisCycle;

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

                postCycleLength = postAfterCyceleTicks - owner.cycleLength;

                if (postCycleLength <= 0) postCycleLength = 14400;
            }

            public bool ShouldHighlightFoodPip(int pipNumber, int currentFood)
            {
                if (!PostCycleStarted) return false;

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

            private void InitializeFoodPlan()
            {
                if (foodPlanInitialized) return;

                RainWorldGame game = owner.world.game;

                if (game.GetStorySession == null) return;

                AbstractCreature absPlayer = game.FirstAlivePlayer ?? game.FirstAnyPlayer;

                if (absPlayer?.state is not PlayerState playerState) return;

                StoryGameSession session = game.GetStorySession;
                SaveState save = session.saveState;

                int maxFood = session.characterStats.maxFood;

                fullFoodChangeCycle = save.VoidFullAnd11Karma(playerState.foodInStomach, 0, maxFood);

                if (fullFoodChangeCycle) foodToConsumeThisCycle = maxFood;
                else foodToConsumeThisCycle = session.characterStats.foodToHibernate;

                foodToConsumeThisCycle = Mathf.Max(0, foodToConsumeThisCycle);

                foodPlanInitialized = true;
            }

            private void UpdateFoodConsumption(Player player)
            {
                if (!AllowToSubtractFood || !foodPlanInitialized || foodToConsumeThisCycle <= 0 || player == null || !player.playerState.alive)
                    return;

                const int startOffset = 4800;
                int end = postCycleLength - 4800;
                int duration = Mathf.Max(1, end - startOffset);
                int elapsed = Mathf.Clamp(timer - startOffset, 0, duration);
                int shouldBeConsumed = Mathf.FloorToInt((float)elapsed / duration * foodToConsumeThisCycle);

                if (timer >= end) shouldBeConsumed = foodToConsumeThisCycle;

                while (subtractedFood < shouldBeConsumed)
                {
                    ConsumeOneFood(player);
                    subtractedFood++;

                    if (!player.playerState.alive) break;
                }
            }

            private void ConsumeOneFood(Player player)
            {
                if (player.FoodInStomach > 0)
                {
                    SaveState save = owner.world.game.GetStorySession.saveState;

                    int totalFood = save.totFood;

                    player.SubtractFood(1);

                    save.totFood = totalFood;
                    return;
                }

                SaveState saveState = owner.world.game.GetStorySession.saveState;

                int playerNumber = player.playerState.playerNumber;

                if (!saveState.GetVoidMarkV3() || startMalnourished[playerNumber])
                {
                    player.Die();
                    return;
                }

                if (!player.Malnourished)
                {
                    player.SetMalnourished(true);
                    return;
                }

                stunned++;
                player.stun += 40 * stunned;
            }

            public void AfterCycleUpdate()
            {
                if (shelterFinder == null)
                {
                    shelterFinder = new(owner.world);

                    new Thread(StartToMapThread).Start();
                }

                if (!PostCycleStarted) return;

                InitializeFoodPlan();

                if (!shelterFinder.done) return;

                if (!metersReplaced)
                {
                    for (int i = 0; i < owner.world.game.cameras.Length; i++)
                    {
                        if (owner.world.game.cameras[i].hud is HUD.HUD { rainMeter: not null } hud && !hud.rainMeter.GetAfterCycleMode().Value)
                        {
                            hud.rainMeter.slatedForDeletion = true;
                            hud.AddPart(new RainMeter(hud, hud.fContainers[1]));
                        }
                    }

                    metersReplaced = true;
                }

                timer++;

                AbstractCreature absPlayer = owner.world.game.FirstAlivePlayer;

                if (absPlayer?.realizedCreature is Player player && player.playerState.alive)
                    UpdateFoodConsumption(player);

                if (TimeToStartNewCycle > 0)
                    return;

                RainWorldGame game = owner.world.game;

                if (!PermadeathConditions.TryPrepareVoidCycleAdvance(game))
                    return;

                absPlayer = game.FirstAlivePlayer;

                bool survived = absPlayer != null && absPlayer.state.alive;

                if (survived && fullFoodChangeCycle && foodPlanInitialized && subtractedFood >= foodToConsumeThisCycle)
                {
                    FoodChange.ApplyFullFoodProgression(game.GetStorySession.saveState);
                    FoodChange.RefreshLiveFoodStats(game);
                }

                float newCycleLength = Mathf.Lerp(game.rainWorld.setup.cycleTimeMin, game.rainWorld.setup.cycleTimeMax, Random.value);

                RainCycle newRainCycle = new(owner.world, newCycleLength);

                newRainCycle.GetRainCycleExt().shelterFinder = shelterFinder;

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

                        if (shortcut.shortCutType != ShortcutData.Type.RoomExit) continue;

                        AbstractRoom leadingRoom = room.world.GetAbstractRoom(room.abstractRoom.connections[shortcut.destNode]);

                        if (leadingRoom != null && leadingRoom.shelter && !leadingRoom.world.brokenShelters[leadingRoom.shelterIndex])
                            room.lockedShortcuts.RemoveAt(j);
                    }
                }

                if (absPlayer != null && absPlayer.state.alive)
                {
                    SaveProgress();

                    for (int i = 0; i < game.cameras.Length; i++)
                    {
                        if (game.cameras[i].hud is HUD.HUD { karmaMeter: var karmaMeter })
                            karmaMeter.reinforceAnimation = 1;
                    }
                }
            }

            public void SaveProgress()
            {
                RainWorldGame game = owner.world.game;
                StoryGameSession session = game.GetStorySession;
                SaveState saveState = session.saveState;

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

                if (saveState.deathPersistentSaveData.karma < saveState.deathPersistentSaveData.karmaCap)
                    saveState.deathPersistentSaveData.karma++;

                if (!game.session.characterStats.malnourished)
                    RainWorldGame.ForceSaveNewDenLocation(game, ComputeNearestShelter(), true);
                else
                    game.rainWorld.progression.SaveWorldStateAndProgression(false);

                ResetSessionRecords(game);
                UpdateCycleHUD(game);
            }

            private void ResetSessionRecords(RainWorldGame game)
            {
                StoryGameSession session = game.GetStorySession;

                for (int i = 0; i < game.Players.Count; i++)
                {
                    if (game.Players[i]?.state is not PlayerState playerState) continue;

                    session.playerSessionRecords[playerState.playerNumber] = new PlayerSessionRecord(playerState.playerNumber);
                }

                if (!game.world.singleRoomWorld && session.playerSessionRecords[0] != null)
                    session.playerSessionRecords[0].wokeUpInRegion = game.world.region.name;
            }


            private void UpdateCycleHUD(RainWorldGame game)
            {
                for (int i = 0; i < game.cameras.Length; i++)
                {
                    if (game.cameras[i].hud?.map?.cycleLabel == null) continue;

                    game.cameras[i].hud.map.cycleLabel.UpdateCycleText();
                }
            }

            public string ComputeNearestShelter()
            {
                AbstractCreature player = owner.world.game.FirstAlivePlayer ?? owner.world.game.FirstAnyPlayer;

                if (player != null && player.Room != null)
                {
                    float minDistance = float.MaxValue;
                    int nearestShelterIndex = -1;

                    for (int i = 0; i < player.world.shelters.Length; i++)
                    {
                        for (int j = 0; j < player.Room.connections.Length; j++)
                        {
                            float distance = shelterFinder.DistanceToShelter(i, new WorldCoordinate(player.Room.index, -1, -1, j));

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

                return owner.world.GetAbstractRoom(owner.world.shelters[Random.Range(0, owner.world.shelters.Length)]).name;
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