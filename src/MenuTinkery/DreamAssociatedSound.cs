using System.Collections.Generic;
using static VoidTemplate.VoidEnums.DreamID;

namespace VoidTemplate.MenuTinkery;

internal static class DreamAssociatedSound
{

    //This dictionary associates DreamID with the ID of sound to be played. so yeah it does support custom sounds
    static Dictionary<DreamsState.DreamID, SoundID> DreamSoundMap;

    public static void Startup()
    {
        On.Menu.Menu.PlaySound_SoundID += Menu_PlaySound_SoundID;
        DreamSoundMap = new()
        {
            { Void_NSHDream, VoidEnums.SoundID.VoidNSHDreamSound },
            { NSHDream, VoidEnums.SoundID.NSHDreamSound },
            { SkyDream, VoidEnums.SoundID.SkyDreamSound },
            { SubDream, VoidEnums.SoundID.SubDreamSound },
            { FarmDream, VoidEnums.SoundID.FarmDreamSound },
            { MoonDream, VoidEnums.SoundID.MoonDreamSound },
            { PebbleDream, VoidEnums.SoundID.PebbleDreamSound },
            { RotDream, VoidEnums.SoundID.RotDreamSound },
            { Void_SeaDream, VoidEnums.SoundID.VoidSeaDreamSound },
            { Void_BodyDream, VoidEnums.SoundID.VoidBodyDreamSound },
            { Void_HeartDream, VoidEnums.SoundID.VoidHeartDreamSound },
        };
    }

    private static void Menu_PlaySound_SoundID(On.Menu.Menu.orig_PlaySound_SoundID orig, Menu.Menu self, SoundID soundID)
    {
        if (self is Menu.DreamScreen screen && soundID == SoundID.MENU_Dream_Switch && DreamSoundMap.ContainsKey(screen.dreamID)) orig(self, DreamSoundMap[screen.dreamID]);
        else orig(self, soundID);
    }
}
