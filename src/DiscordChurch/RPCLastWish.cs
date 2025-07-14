using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.DiscordChurch
{
    internal static class RPCLastWish
    {
        public static Discord.Discord discord;

        public static ActivityManager activityManager;

        public static bool discordInited;

        public static void Hook()
        {
            On.Menu.MainMenu.Update += MainMenu_Update;
            On.Player.Update += Player_Update;
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

            var activity = new Activity();

            activity.Assets.LargeImage = self.SlugCatClass.value.ToLower();

            if (self.abstractCreature?.world?.game?.session is StoryGameSession story)
            {
                string regionName = self.room?.abstractRoom?.subregionName
                                  ?? Region.GetRegionFullName(self.room.world.name, story.saveStateNumber);

                activity.State = $"Wandering in {regionName}";
                activity.Details = $"Story: The {SlugcatStats.getSlugcatName(story.saveStateNumber)} | " +
                                 $"Cycles: {story.saveState.cycleNumber} | " +
                                 $"{(story.saveState.deathPersistentSaveData.karma < 11 ? "Karma" : "Protection")}: " +
                                 $"{story.saveState.deathPersistentSaveData.karma + 1}";
            }
            else if (self.abstractCreature?.world?.game?.session is ArenaGameSession arena)
            {
                activity.Details = $"Wandering in Arena";
            }

            activityManager.UpdateActivity(activity, result =>
            {
                if (result != Result.Ok)
                {
                    Debug.LogError($"Discord Rich Presence update failed: {result}");
                }
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

            activityManager.UpdateActivity(new Activity()
            {
                Details = "Wandering in Main Menu",
                Assets = new ActivityAssets()
                {
                    LargeImage = "lastwish_rpc_thumbnail"
                }
            }, x => { });
        }

        public static void TryDiscordCallBack()
        {
            try
            {
                discord.RunCallbacks();
            }
            catch
            {
                discordInited = false;
            }
        }

        public static void TryInitiateDiscord()
        {
            try
            {
                discord = new Discord.Discord(1393296386568753202, (ulong)CreateFlags.NoRequireDiscord);
                discordInited = discord != null;
                if (discordInited)
                {
                    discord.SetLogHook(LogLevel.Info, (level, message) => UnityEngine.Debug.Log($"[DISCORD LAST WISH RPC {level.ToString().ToUpper()}] {message}"));
                    activityManager = discord.GetActivityManager();
                }
            }
            catch
            {
                discordInited = false;
            }
        }
    }
}
