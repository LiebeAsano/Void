using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Fisobs;
using Menu;
using MoreSlugcats;
using UnityEngine;
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

        private static readonly long _gameStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static void Hook()
        {
            On.Menu.MainMenu.Update += MainMenu_Update;
            On.Player.Update += Player_Update;
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
            return sandboxUnlockID != null ? KillScores()[sandboxUnlockID.Index] : 0;
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

            if (!discordInited)
            {
                TryInitiateDiscord();
                return;
            }

            TryDiscordCallBack();

            var activity = new Activity
            {
                Timestamps = { Start = _gameStartTimestamp },
                Assets = { LargeImage = self.SlugCatClass.value.ToLower() }
            };

            var bodyMode = self.bodyMode;
            string slugMode = "Standing";
            if (bodyMode == Player.BodyModeIndex.Default) slugMode = "Jumping";
            if (bodyMode == Player.BodyModeIndex.Crawl) slugMode = "Crawling";
            if (bodyMode == Player.BodyModeIndex.Stand) slugMode = "Standing";
            if (bodyMode == Player.BodyModeIndex.CorridorClimb) slugMode = "Corridor climbing";
            if (bodyMode == Player.BodyModeIndex.ClimbIntoShortCut) slugMode = "Short cutting";
            if (bodyMode == Player.BodyModeIndex.WallClimb) slugMode = "Wall climbing";
            if (bodyMode == Player.BodyModeIndex.ClimbingOnBeam) slugMode = "Climbing on beam";
            if (bodyMode == Player.BodyModeIndex.Swimming) slugMode = "Swimming";
            if (bodyMode == Player.BodyModeIndex.ZeroG) slugMode = "Levitating";
            if (bodyMode == Player.BodyModeIndex.Stunned) slugMode = "Stunned";
            if (bodyMode == Player.BodyModeIndex.Dead) slugMode = "Dead";
            if (bodyMode == BodyModeIndexExtension.CeilCrawl) slugMode = "Ceil climbing";

            if (self.abstractCreature?.world?.game?.session is StoryGameSession story)
            {
                string regionName = self.room?.abstractRoom?.subregionName
                                ?? Region.GetRegionFullName(self.room.world.name, story.saveStateNumber);

                string minutes = "minutes";

                if ((story.game.world.rainCycle.cycleLength - story.game.world.rainCycle.timer) / (40 * 60) > 1)
                {
                    minutes = "minutes";
                }
                else if ((story.game.world.rainCycle.cycleLength - story.game.world.rainCycle.timer) / (40 * 60) > 0)
                {
                    minutes = "minute";
                }
                activity.State = $"{slugMode} in {regionName}";
                activity.Details = $"Food: [{self.FoodInStomach}/{self.slugcatStats.foodToHibernate}] | " +
                    $"{(story.saveState.deathPersistentSaveData.karma < 10 ? "Karma" : "Protection")}: " +
                    $"{(story.saveState.deathPersistentSaveData.karma < 10 ? $"[{story.saveState.deathPersistentSaveData.karma + 1}/{story.saveState.deathPersistentSaveData.karmaCap + 1}]" : $"[{story.saveState.GetKarmaToken()/2}/5]")} | " +
                    $"Cycles: {story.saveState.cycleNumber} | " +
                    $"Deaths: {story.saveState.deathPersistentSaveData.deaths} | " +
                    $"Score: {GetTotalScore(story.saveState)} | " +
                    $"{((story.game.world.rainCycle.cycleLength - story.game.world.rainCycle.timer)/(40 * 60) == 0 ? "Rain is coming" : $"Rain in {(story.game.world.rainCycle.cycleLength - story.game.world.rainCycle.timer) / (40 * 60)}" + $"{minutes}")} | " +
                    $"Story: The {SlugcatStats.getSlugcatName(story.saveStateNumber)}";
            }
            else if (self.abstractCreature?.world?.game?.session is ArenaGameSession arena)
            {
                activity.Details = $"{slugMode} in Arena";
            }

            activityManager.UpdateActivity(activity, result =>
            {
                if (result != Result.Ok) Debug.LogError($"Discord RP update failed: {result}");
            });
        }

        private static void MainMenu_Update(On.Menu.MainMenu.orig_Update orig, Menu.MainMenu self)
        {
            orig(self);

            if (!discordInited)
            {
                TryInitiateDiscord();
                return;
            }

            TryDiscordCallBack();

            activityManager.UpdateActivity(new Activity
            {
                Details = "Wandering in Main Menu",
                Timestamps = { Start = _gameStartTimestamp },
                Assets = { LargeImage = "lastwish_rpc_thumbnail" }
            }, _ => { });
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
