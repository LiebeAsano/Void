using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.OptionInterface;

namespace VoidTemplate.MenuTinkery
{
    public class IntroRollSound
    {
        public static void Hook()
        {
            On.Music.IntroRollMusic.ctor += IntroRollMusic_ctor;
        }

        private static void IntroRollMusic_ctor(On.Music.IntroRollMusic.orig_ctor orig, Music.IntroRollMusic self, Music.MusicPlayer musicPlayer)
        {
            orig(self, musicPlayer);
            if (!OptionAccessors.DisableMenuBackGround && (SaveManager.ExternalSaveData.MonkAscended || SaveManager.ExternalSaveData.SurvAscended || SaveManager.ExternalSaveData.VoidDead && SaveManager.ExternalSaveData.VoidKarma11))
            {
                self.subTracks.RemoveAt(self.subTracks.Count - 1);
                self.subTracks.Add(new(self, 1, "MainMenuTheme"));
                self.Loop = true;
            }
        }

    }
}
