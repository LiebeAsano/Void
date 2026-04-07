using Discord;
using Menu;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.DiscordChurch
{
    internal static class RPCLastWish
    {
        public static Discord.Discord discord;
        public static ActivityManager activityManager;
        public static bool discordInited;
        public static bool starvation = false;
        public static bool sleeping = false;
        public static bool dead = false;
        public static bool[] leftshelter = new bool[32];
        public static string oldSmallImage;
        public static string oldSmallText;

        private static float timeSinceLastForceUpdate = 0f;
        private static readonly float forceUpdateInterval = 1f / 4f;
        private static readonly long _gameStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static bool hooksInstalled;

        private static readonly Dictionary<Player.BodyModeIndex, string> BodyModeToSlugMode = new()
        {
            { Player.BodyModeIndex.Default, "Jumping" },
            { Player.BodyModeIndex.Crawl, "Crawling" },
            { Player.BodyModeIndex.Stand, "Standing" },
            { Player.BodyModeIndex.CorridorClimb, "Corridor climbing" },
            { Player.BodyModeIndex.ClimbIntoShortCut, "Short cutting" },
            { Player.BodyModeIndex.WallClimb, "Wall climbing" },
            { Player.BodyModeIndex.ClimbingOnBeam, "Climbing on beam" },
            { Player.BodyModeIndex.Swimming, "Swimming" },
            { Player.BodyModeIndex.ZeroG, "Levitating" },
            { Player.BodyModeIndex.Stunned, "Stunned" },
            { Player.BodyModeIndex.Dead, "Dead" },
            { BodyModeIndexExtension.CeilCrawl, "Ceil climbing" }
        };

        private static int[] killScores;

        public static void Hook()
        {
            if (hooksInstalled) return;
            hooksInstalled = true;

            On.Menu.MainMenu.Update += MainMenu_Update;
            On.Player.Update += Player_Update;
            On.Player.Destroy += Player_Destroy;
            On.Player.ctor += Player_ctor;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            try
            {
                ShutdownDiscord();
            }
            finally
            {
                orig(self);
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            int playerNumber = GetPlayerNumberSafe(self);
            if (playerNumber >= 0 && playerNumber < leftshelter.Length)
            {
                leftshelter[playerNumber] = false;
            }
        }

        private static int[] KillScores()
        {
            int count = ExtEnum<MultiplayerUnlocks.SandboxUnlockID>.values.Count;
            if (killScores == null || killScores.Length != count)
            {
                killScores = new int[count];

                for (int i = 0; i < killScores.Length; i++)
                {
                    killScores[i] = 1;
                }

                SandboxSettingsInterface.DefaultKillScores(ref killScores);
                killScores[(int)MultiplayerUnlocks.SandboxUnlockID.Slugcat] = 1;
            }

            return killScores;
        }

        public static int KillScore(IconSymbol.IconSymbolData iconData)
        {
            if (!CreatureSymbol.DoesCreatureEarnATrophy(iconData.critType))
            {
                return 0;
            }

            int num = StoryGameStatisticsScreen.GetNonSandboxKillscore(iconData.critType);
            if (num != 0)
            {
                return num;
            }

            MultiplayerUnlocks.SandboxUnlockID sandboxUnlockID = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconData);
            if (sandboxUnlockID == null)
            {
                return 0;
            }

            int[] scores = KillScores();
            if (sandboxUnlockID.Index >= 0 && sandboxUnlockID.Index < scores.Length)
            {
                return scores[sandboxUnlockID.Index];
            }

            return 0;
        }

        private static int GetTotalScore(SaveState s)
        {
            if (s == null)
            {
                return 0;
            }

            var deathData = s.deathPersistentSaveData;
            bool isRed = s.saveStateNumber == SlugcatStats.Name.Red;
            bool isArtificer = s.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer;

            int baseScore =
                s.totFood +
                deathData.survives * 10 +
                s.kills.Sum(kvp => KillScore(kvp.Key) * kvp.Value) -
                (deathData.deaths * 3 + deathData.quits * 3 + s.totTime / 60) +
                (deathData.ascended ? 300 : 0) +
                (s.miscWorldSaveData.moonRevived ? 100 : 0) +
                (s.miscWorldSaveData.pebblesSeenGreenNeuron ? 40 : 0);

            int bonusScore =
                (!isArtificer ? deathData.friendsSaved * 15 : 0) +
                (!isRed ? s.miscWorldSaveData.SLOracleState.significantPearls.Count * 20 : 0) +
                (!isRed && !isArtificer && s.miscWorldSaveData.SSaiConversationsHad > 0 ? 40 : 0) +
                (!isRed && !isArtificer && s.miscWorldSaveData.SLOracleState.playerEncounters > 0 ? 40 : 0) +
                (deathData.winState.GetTracker(MoreSlugcatsEnums.EndgameID.Gourmand, false) is WinState.GourFeastTracker tracker && tracker.GoalFullfilled ? 300 : 0);

            return baseScore + bonusScore;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (self == null || self.playerState == null || self.playerState.permaDead)
            {
                return;
            }

            if (!ShouldUpdateNow())
            {
                return;
            }

            if (!EnsureDiscordReady())
            {
                return;
            }

            UpdatePresenceForPlayer(self);
        }

        private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            dead = true;

            if (!OptionAccessors.DisableRPC && self != null)
            {
                if (EnsureDiscordReady())
                {
                    UpdatePresenceForPlayer(self);
                }
            }

            orig(self);
        }

        private static void UpdateActivityBasedOnGameSession(Player self, ref Activity activity)
        {
            string slugMode = GetSlugMode(self);

            if (self.abstractCreature?.world?.game?.session is StoryGameSession story)
            {
                UpdateStorySessionActivity(self, story, ref activity, slugMode);
            }
            else if (self.abstractCreature?.world?.game?.session is ArenaGameSession)
            {
                activity.Details = $"{slugMode} in Arena";
                activity.State = string.Empty;
                activity.Assets.LargeText = "Arena";
            }
            else
            {
                activity.Details = slugMode;
                activity.State = string.Empty;
            }
        }

        private static string GetSlugMode(Player self)
        {
            if (self == null)
            {
                return "Standing";
            }

            if (self.Stunned)
            {
                return "Stunned";
            }

            if (self.dead || dead)
            {
                return "Dead";
            }

            bool inShelter = self.room?.abstractRoom?.shelter ?? false;
            int playerNumber = GetPlayerNumberSafe(self);

            if (inShelter)
            {
                if (dead)
                {
                    return "Dead";
                }

                if (starvation)
                {
                    bool hasLeftShelter = playerNumber >= 0 &&
                                          playerNumber < leftshelter.Length &&
                                          leftshelter[playerNumber];

                    return hasLeftShelter ? "Starving" : "Waking up";
                }

                if (sleeping)
                {
                    return "Sleeping";
                }
            }

            return BodyModeToSlugMode.TryGetValue(self.bodyMode, out string mode) ? mode : "Standing";
        }

        private static void UpdateStorySessionActivity(Player self, StoryGameSession story, ref Activity activity, string slugMode)
        {
            string regionName = self.room?.abstractRoom?.subregionName;

            if (string.IsNullOrEmpty(regionName))
            {
                regionName = self.room?.world != null
                    ? Region.GetRegionFullName(self.room.world.name, story.saveStateNumber)
                    : "Depths";
            }

            UpdateShelterStatus(self, story);

            activity.State = $"{slugMode} in {regionName}";
            activity.Details = BuildActivityDetails(self, story);
            activity.Assets.LargeText = $"Story: The {SlugcatStats.getSlugcatName(story.saveStateNumber)}";

            bool inShelter = self.room?.abstractRoom?.shelter ?? false;
            if (!inShelter)
            {
                activity.Assets.SmallImage = oldSmallImage = GetSmallImage(self, story);
                activity.Assets.SmallText = oldSmallText = GetSmallText(story);
            }
            else
            {
                activity.Assets.SmallImage = oldSmallImage ?? string.Empty;
                activity.Assets.SmallText = oldSmallText ?? string.Empty;
            }
        }

        private static void UpdateShelterStatus(Player self, StoryGameSession story)
        {
            bool inShelter = self.room?.abstractRoom?.shelter ?? false;
            int playerNumber = GetPlayerNumberSafe(self);

            if (inShelter)
            {
                if (self.Consious)
                {
                    RainCycle rainCycle = story.game.world.rainCycle;

                    if (story.saveState.malnourished && rainCycle.cycleLength - rainCycle.timer <= 0)
                    {
                        dead = true;
                        starvation = false;
                        sleeping = false;
                    }
                    else if (self.FoodInStomach < self.slugcatStats.foodToHibernate)
                    {
                        starvation = true;
                        sleeping = false;
                        dead = false;
                    }
                    else if (self.readyForWin)
                    {
                        sleeping = true;
                        starvation = false;
                        dead = false;
                    }
                    else
                    {
                        starvation = false;
                        sleeping = false;
                    }
                }
            }
            else
            {
                if (playerNumber >= 0 && playerNumber < leftshelter.Length)
                {
                    leftshelter[playerNumber] = true;
                }

                starvation = false;
                sleeping = false;
                dead = false;
            }
        }

        private static string BuildActivityDetails(Player self, StoryGameSession story)
        {
            SaveState saveState = story.saveState;
            RainCycle rainCycle = story.game.world.rainCycle;

            int timeToRain = (rainCycle.cycleLength - rainCycle.timer) / (40 * 60);
            string minutesText = timeToRain == 1 ? "min" : "mins";
            string rainText = timeToRain <= 0 ? "Rain is coming" : $"Rain in {timeToRain} {minutesText}";

            return $"Food: [{self.FoodInStomach}/{self.slugcatStats.foodToHibernate}] | " +
                   $"{rainText} | " +
                   $"Score: {GetTotalScore(saveState)} | " +
                   $"Cycles: {saveState.cycleNumber} | " +
                   $"Deaths: {saveState.deathPersistentSaveData.deaths}";
        }

        private static void MainMenu_Update(On.Menu.MainMenu.orig_Update orig, Menu.MainMenu self)
        {
            orig(self);

            if (!ShouldUpdateNow())
            {
                return;
            }

            if (!EnsureDiscordReady())
            {
                return;
            }

            try
            {
                activityManager.UpdateActivity(new Activity
                {
                    Details = "Wandering in Main Menu",
                    Timestamps = { Start = _gameStartTimestamp },
                    Assets = { LargeImage = "lastwish_rpc_thumbnail" }
                }, result =>
                {
                    if (result != Result.Ok)
                    {
                        Debug.LogError($"Discord RP update failed: {result}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DISCORD RPC] Main menu update exception: {e}");
                ShutdownDiscord();
            }
        }

        public static void TryDiscordCallBack()
        {
            if (!discordInited || discord == null)
            {
                return;
            }

            try
            {
                discord.RunCallbacks();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DISCORD RPC] Callback failed: {e}");
                ShutdownDiscord();
            }
        }

        public static void TryInitiateDiscord()
        {
            if (OptionAccessors.DisableRPC)
            {
                ShutdownDiscord();
                return;
            }

            if (discordInited && discord != null && activityManager != null)
            {
                return;
            }

            ShutdownDiscord();

            try
            {
                discord = new Discord.Discord(1393296386568753202, (ulong)CreateFlags.NoRequireDiscord);
                activityManager = discord.GetActivityManager();
                discordInited = discord != null && activityManager != null;

                if (discordInited)
                {
                    discord.SetLogHook(LogLevel.Info, (level, message) =>
                        Debug.Log($"[DISCORD RPC {level}] {message}"));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DISCORD RPC] Init failed: {e}");
                ShutdownDiscord();
            }
        }

        private static bool ShouldUpdateNow()
        {
            if (OptionAccessors.DisableRPC)
            {
                return false;
            }

            timeSinceLastForceUpdate += Time.deltaTime;
            if (timeSinceLastForceUpdate < forceUpdateInterval)
            {
                return false;
            }

            timeSinceLastForceUpdate = 0f;
            return true;
        }

        private static bool EnsureDiscordReady()
        {
            if (!discordInited || discord == null || activityManager == null)
            {
                TryInitiateDiscord();
            }

            if (!discordInited || discord == null || activityManager == null)
            {
                return false;
            }

            TryDiscordCallBack();
            return discordInited && discord != null && activityManager != null;
        }

        private static void UpdatePresenceForPlayer(Player self)
        {
            try
            {
                Activity activity = new()
                {
                    Timestamps = { Start = _gameStartTimestamp },
                    Assets = { LargeImage = self.SlugCatClass.value.ToLowerInvariant() }
                };

                UpdateActivityBasedOnGameSession(self, ref activity);

                activityManager.UpdateActivity(activity, result =>
                {
                    if (result != Result.Ok)
                    {
                        Debug.LogError($"Discord RP update failed: {result}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DISCORD RPC] Player update exception: {e}");
                ShutdownDiscord();
            }
        }

        private static void ShutdownDiscord()
        {
            discordInited = false;
            activityManager = null;

            if (discord != null)
            {
                try
                {
                    discord.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DISCORD RPC] Dispose failed: {e}");
                }
                finally
                {
                    discord = null;
                }
            }
        }

        private static int GetPlayerNumberSafe(Player self)
        {
            return self?.playerState?.playerNumber ?? -1;
        }

        private static string GetSmallImage(Player self, StoryGameSession story)
        {
            if (self.KarmaCap == 10)
            {
                return $"protection{story.saveState.GetKarmaToken()}";
            }

            return self.Karma < 5
                ? $"karma{self.Karma}"
                : $"karma{self.Karma}{self.KarmaCap}";
        }

        private static string GetSmallText(StoryGameSession story)
        {
            if (story.saveState.deathPersistentSaveData.karma < 10)
            {
                return $"Karma: [{story.saveState.deathPersistentSaveData.karma + 1}/{story.saveState.deathPersistentSaveData.karmaCap + 1}]";
            }

            return $"Protection: [{story.saveState.GetKarmaToken()}/5]";
        }
    }
}