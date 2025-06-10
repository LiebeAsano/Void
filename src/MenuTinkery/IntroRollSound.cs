using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.MenuTinkery
{
    internal class IntroRollSound
    {
        public static void Hook()
        {
            On.Music.IntroRollMusic.ctor += IntroRollMusic_ctor;
        }

        private static void IntroRollMusic_ctor(On.Music.IntroRollMusic.orig_ctor orig, Music.IntroRollMusic self, Music.MusicPlayer musicPlayer)
        {
            orig(self, musicPlayer);
            self.subTracks.RemoveAt(self.subTracks.Count - 1);
            self.subTracks.Add(new(self, 1, "StaticEndSound11"));
        }
    }
}
