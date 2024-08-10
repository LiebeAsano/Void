using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate;
using Unity.IO.LowLevel.Unsafe;
using static VoidTemplate.VoidEnums.DreamID;
using static VoidTemplate.VoidEnums.SceneID;
using static VoidTemplate.SaveManager;

namespace VoidTemplate;

public static class Dreams
{
    public static void RegisterMaps()
    {
        DreamSceneMap = new()
        {
            {FarmDream, Farm},
            {MoonDream, Moon},
            {NSHDream, NSH},
            {PebbleDream, Pebble},
            {RotDream, Rot},
            {SkyDream, Sky},
            {SubDream, Sub},
            {Void_BodyDream, Void_Body},
            {Void_HeartDream, Void_Heart},
            {Void_NSHDream, Void_NSH},
            {Void_SeaDream, Void_Sea}
        };
        DreamEnumMapper = new()
        {
            {Dream.Farm, FarmDream},
            {Dream.Moon, MoonDream},
            {Dream.NSH, NSHDream},
            {Dream.Pebble, PebbleDream},
            {Dream.Rot, RotDream},
            {Dream.Sky, SkyDream},
            {Dream.Sub, SubDream},
            {Dream.VoidBody, Void_BodyDream},
            {Dream.VoidHeart, Void_HeartDream},
            {Dream.VoidNSH, Void_NSHDream},
            {Dream.VoidSea, Void_SeaDream}
        };
    }
    private static Dictionary<DreamsState.DreamID, Menu.MenuScene.SceneID> DreamSceneMap;
    /// <summary>
    /// since using dreamstate directly in savng is impossible due to overridden ToString, we have to stick to our enum. to map, this dictionary is used
    /// </summary>
    public static Dictionary<Dream, DreamsState.DreamID> DreamEnumMapper;
    /// <summary>
    /// first is more priority
    /// and also if there's no dream here, it won't appear
    /// </summary>
    private static Dream[] DreamPriority = [Dream.Farm, Dream.Moon, Dream.NSH, Dream.Pebble, Dream.Rot, Dream.Sky, Dream.Sub, Dream.VoidBody, Dream.VoidHeart, Dream.VoidNSH, Dream.VoidSea];
    
    public static void Hook()
    {
        On.DreamsState.StaticEndOfCycleProgress += ScheduleDream;
        On.Menu.DreamScreen.SceneFromDream += DreamScreen_SceneFromDream;
        On.SaveState.ctor += SaveState_ctor;

    }
    /// <summary>
    /// vanilla only adds dreamsState to select few cats. and dislikes summoning dream process from those without one. so here we go, tracking family loss
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="saveStateNumber"></param>
    /// <param name="progression"></param>
    private static void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
    {
        orig(self, saveStateNumber, progression);
        if (saveStateNumber == VoidEnums.SlugcatID.TheVoid) self.dreamsState = new();
    }
    private static Menu.MenuScene.SceneID DreamScreen_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, Menu.DreamScreen self, DreamsState.DreamID dreamID)
    {
        return DreamSceneMap.ContainsKey(dreamID) ? DreamSceneMap[dreamID] : orig(self, dreamID);
    }

    private static void ScheduleDream(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
    {
        if(saveState.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
        {
            var dreamtoshow = DreamPriority.FirstOrDefault(dream =>
            {
                var data = saveState.GetDreamData(dream);
                return data.HasShowConditions && !data.WasShown;
            });
            if (dreamtoshow != default)
            {
                saveState.SetDreamData(dreamtoshow, new(true, true));
                eventDream = DreamEnumMapper[dreamtoshow];
            }
        }
        orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
        if (saveState.saveStateNumber == VoidEnums.SlugcatID.TheVoid && !DreamEnumMapper.Values.Contains(upcomingDream)) upcomingDream = null;
    }
}
