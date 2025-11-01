using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.OptionInterface;
using Music;
using MonoMod.Cil;
using System.CodeDom;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery
{
    public class IntroRollSound
    {
        public const string MainMenuTheme = "MainMenuTheme";
        public const string MainMenuThemeViy = "MainMenuThemeViy";

        public static bool VoidThemeCond
        {
            get
            {
                return !OptionAccessors.DisableMenuBackGround && (SaveManager.ExternalSaveData.MonkAscended
                || SaveManager.ExternalSaveData.SurvAscended || SaveManager.ExternalSaveData.ViyUnlocked);
            }
        }

        public static string CurrentMainMenuTheme
        {
            get
            {
                if (SaveManager.ExternalSaveData.ViyUnlocked)
                {
                    return MainMenuThemeViy;
                }
                return MainMenuTheme;
            }
        }

        public static void Hook()
        {
            On.Music.IntroRollMusic.ctor += IntroRollMusic_ctor;
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            IL.Menu.OptionsMenu.SliderSetValue += OptionsMenu_SliderSetValue;
            On.Music.MusicPlayer.MenuRequestsSong += MusicPlayer_MenuRequestsSong;
        }

        private static void MusicPlayer_MenuRequestsSong(On.Music.MusicPlayer.orig_MenuRequestsSong orig, MusicPlayer self, string name, float priority, float fadeInTime)
        {
            orig(self, name, priority, fadeInTime);
            string currentTheme = CurrentMainMenuTheme;

            if (self.song != null && self.song.name == currentTheme)
            {
                self.song.Loop = true;
            }
            else if (self.nextSong != null && self.nextSong.name == currentTheme)
            {
                self.nextSong.Loop = true;
            }
        }

        private static void OptionsMenu_SliderSetValue(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("RW_8 - Sundown")))
            {
                c.EmitDelegate((string rainWorldTheme) =>
                {
                    if (VoidThemeCond)
                    {
                        return CurrentMainMenuTheme;
                    }
                    return rainWorldTheme;
                });
            }
            else LogExErr("Cannot emit void menu theme");
        }

        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            if (VoidThemeCond && manager.musicPlayer is MusicPlayer player)
            {
                string currentTheme = CurrentMainMenuTheme;

                if (player.song != null && (player.song.name == currentTheme || player.song is IntroRollMusic))
                {
                    return;
                }
                if (player.nextSong != null && (player.nextSong.name == currentTheme || player.nextSong is IntroRollMusic))
                {
                    return;
                }
                if (player.song == null)
                {
                    player.song = new VoidMenuTheme(manager.musicPlayer, true);
                    return;
                }
                player.nextSong = new VoidMenuTheme(manager.musicPlayer);
            }
        }

        private static void IntroRollMusic_ctor(On.Music.IntroRollMusic.orig_ctor orig, IntroRollMusic self, MusicPlayer musicPlayer)
        {
            orig(self, musicPlayer);
            if (VoidThemeCond)
            {
                self.subTracks.RemoveAt(self.subTracks.Count - 1);
                self.subTracks.Add(new(self, 1, CurrentMainMenuTheme));
                self.Loop = true;
            }
        }

        public class VoidMenuTheme : Song
        {
            public VoidMenuTheme(MusicPlayer musicPlayer, bool playWhenReady = false) : base(musicPlayer, CurrentMainMenuTheme, MusicPlayer.MusicContext.Menu)
            {
                this.playWhenReady = playWhenReady;
                Loop = true;
            }
        }
    }
}
