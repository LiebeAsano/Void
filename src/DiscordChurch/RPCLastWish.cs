using Discord;
using Fisobs;
using Menu;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics;
using static AssetBundles.AssetBundleManager;
using static VoidTemplate.Useful.Utils;

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
        private static readonly float forceUpdateInterval = 1f / 4;

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


        private static readonly long _gameStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static void Hook()
        {
            On.Menu.MainMenu.Update += MainMenu_Update;
            On.Player.Update += Player_Update;
            On.Player.Destroy += Player_Destroy;
            On.Player.ctor += Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            leftshelter[self.playerState.playerNumber] = false;
        }

        private static int[] killScores;
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

            var sandboxUnlockID = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconData);
            if (sandboxUnlockID == null)
            {
                return 0;
            }

            var scores = KillScores();
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

            int baseScore = s.totFood
                          + deathData.survives * 10
                          + s.kills.Sum(kvp => KillScore(kvp.Key) * kvp.Value)
                          - (deathData.deaths * 3 + deathData.quits * 3 + s.totTime / 60)
                          + (deathData.ascended ? 300 : 0)
                          + (s.miscWorldSaveData.moonRevived ? 100 : 0)
                          + (s.miscWorldSaveData.pebblesSeenGreenNeuron ? 40 : 0);

            int bonusScore = (!isArtificer ? deathData.friendsSaved * 15 : 0)
                          + (!isRed ? s.miscWorldSaveData.SLOracleState.significantPearls.Count * 20 : 0)
                          + (!isRed && !isArtificer && s.miscWorldSaveData.SSaiConversationsHad > 0 ? 40 : 0)
                          + (!isRed && !isArtificer && s.miscWorldSaveData.SLOracleState.playerEncounters > 0 ? 40 : 0)
                          + (deathData.winState.GetTracker(MoreSlugcatsEnums.EndgameID.Gourmand, false) is WinState.GourFeastTracker tracker && tracker.GoalFullfilled ? 300 : 0);

            return baseScore + bonusScore;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            timeSinceLastForceUpdate += Time.deltaTime;
            if (timeSinceLastForceUpdate < forceUpdateInterval || OptionAccessors.DisableRPC || self.playerState.permaDead) return;

            if (!discordInited)
            {
                TryInitiateDiscord();
                timeSinceLastForceUpdate = 0f;
                return;
            }

            TryDiscordCallBack();

            var activity = new Activity
            {
                Timestamps = { Start = _gameStartTimestamp },
                Assets = { LargeImage = self.SlugCatClass.value.ToLower() }
            };

            UpdateActivityBasedOnGameSession(self, ref activity);

            activityManager.UpdateActivity(activity, result =>
            {
                if (result != Result.Ok)
                    Debug.LogError($"Discord RP update failed: {result}");
            });

            timeSinceLastForceUpdate = 0f;
        }

        private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            dead = true;
            if (!OptionAccessors.DisableRPC)
            {
                if (!discordInited)
                {
                    TryInitiateDiscord();
                    timeSinceLastForceUpdate = 0f;
                    return;
                }

                TryDiscordCallBack();

                var activity = new Activity
                {
                    Timestamps = { Start = _gameStartTimestamp },
                    Assets = { LargeImage = self.SlugCatClass.value.ToLower() }
                };

                UpdateActivityBasedOnGameSession(self, ref activity);

                activityManager.UpdateActivity(activity, result =>
                {
                    if (result != Result.Ok)
                        Debug.LogError($"Discord RP update failed: {result}");
                });
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
            }
        }

        private static string GetSlugMode(Player self)
        {
            if (self.Stunned) return "Stunned";
            if (self.dead || dead) return "Dead";

            if (self.room?.abstractRoom?.shelter ?? false)
            {
                if (dead) return "Dead";
                if (starvation) return leftshelter[self.playerState.playerNumber] ? "Starving" : "Waking up";
                if (sleeping) return "Sleeping";
            }

            return BodyModeToSlugMode.TryGetValue(self.bodyMode, out var mode) ? mode : "Standing";
        }

        private static void UpdateStorySessionActivity(Player self, StoryGameSession story, ref Activity activity, string slugMode)
        {
            string regionName = self.room?.abstractRoom?.subregionName ??
                               (self.room?.world != null ? Region.GetRegionFullName(self.room.world.name, story.saveStateNumber) : "Depths"); 

            UpdateShelterStatus(self, story);

            activity.State = $"{slugMode} in {regionName}";
            activity.Details = BuildActivityDetails(self, story);
            activity.Assets.LargeText = $"Story: The {SlugcatStats.getSlugcatName(story.saveStateNumber)}";
            if (!self.room?.abstractRoom?.shelter ?? false)
            {
                activity.Assets.SmallImage = oldSmallImage = $"{(self.KarmaCap == 10 ? $"protection{story.saveState.GetKarmaToken()}" : $"karma{self.Karma}{(self.Karma < 5 ? "" : $"{self.KarmaCap}")}")}";
                activity.Assets.SmallText = oldSmallText = $"{(story.saveState.deathPersistentSaveData.karma < 10 ? "Karma" : "Protection")}: " +
                       $"{(story.saveState.deathPersistentSaveData.karma < 10 ?
                           $"[{story.saveState.deathPersistentSaveData.karma + 1}/{story.saveState.deathPersistentSaveData.karmaCap + 1}]" :
                           $"[{story.saveState.GetKarmaToken()}/5]")}";
            }
            else
            {
                activity.Assets.SmallImage = oldSmallImage;
                activity.Assets.SmallText = oldSmallText;
            }
        }

        private static void UpdateShelterStatus(Player self, StoryGameSession story)
        {
            if (self.room?.abstractRoom?.shelter ?? false)
            {
                if (self.Consious)
                {
                    var rainCycle = story.game.world.rainCycle;
                    if (self.abstractCreature.world.game.GetStorySession.saveState.malnourished && rainCycle.cycleLength - rainCycle.timer <= 0)
                        dead = true;
                    else if (self.FoodInStomach < self.slugcatStats.foodToHibernate)
                        starvation = true;
                    else if (self.readyForWin)
                        sleeping = true;
                }
            }
            else
            {
                leftshelter[self.playerState.playerNumber] = true;
                starvation = false;
                sleeping = false;
                dead = false;
            }
        }

        private static string BuildActivityDetails(Player self, StoryGameSession story)
        {
            var saveState = story.saveState;
            var rainCycle = story.game.world.rainCycle;
            var timeToRain = (rainCycle.cycleLength - rainCycle.timer) / (40 * 60);
            var minutesText = timeToRain > 1 ? "mins" : "min";
            var rainText = timeToRain <= 0 ? "Rain is coming" : $"Rain in {timeToRain} {minutesText}";

            return $"Food: [{self.FoodInStomach}/{self.slugcatStats.foodToHibernate}] | " +
                   $"{rainText} | " +
                   $"Score: {GetTotalScore(saveState)} | " +
                   $"Cycles: {saveState.cycleNumber} | " +
                   $"Deaths: {saveState.deathPersistentSaveData.deaths}";
        }

        private static void MainMenu_Update(On.Menu.MainMenu.orig_Update orig, Menu.MainMenu self)
        {
            orig(self);

            timeSinceLastForceUpdate += Time.deltaTime;
            if (timeSinceLastForceUpdate < forceUpdateInterval || OptionAccessors.DisableRPC) return;

            if (!discordInited)
            {
                TryInitiateDiscord();
                timeSinceLastForceUpdate = 0f;
                return;
            }

            TryDiscordCallBack();

            activityManager.UpdateActivity(new Activity
            {
                Details = "Wandering in Main Menu",
                Timestamps = { Start = _gameStartTimestamp },
                Assets = { LargeImage = "lastwish_rpc_thumbnail" }
            }, _ => { });

            timeSinceLastForceUpdate = 0f;
        }

        public static void TryDiscordCallBack()
        {
            try { discord.RunCallbacks(); }
            catch { discordInited = false; }
        }

        public static void TryInitiateDiscord()
        {
            try
            {
                discord = new Discord.Discord(1393296386568753202, (ulong)CreateFlags.NoRequireDiscord);
                discordInited = discord != null;
                if (discordInited)
                {
                    discord.SetLogHook(LogLevel.Info, (level, message) =>
                        UnityEngine.Debug.Log($"[DISCORD RPC {level}] {message}"));
                    activityManager = discord.GetActivityManager();
                }
            }
            catch { discordInited = false; }
        }
    }
}
