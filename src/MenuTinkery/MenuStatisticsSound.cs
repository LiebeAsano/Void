using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IL.Menu.MenuScene;

namespace VoidTemplate.MenuTinkery;

internal static class MenuStatisticsSound
{
    static Dictionary<string, SoundID> StatisticSoundMap;
    public static void Hook()
    {
        On.Menu.StoryGameStatisticsScreen.GetDataFromGame += GetDataFromGame_Update;

        StatisticSoundMap = new()
        {
            { "Static_Death_Void",  VoidEnums.SoundID.StaticDeathSound },
            { "Static_Death_Void11",  VoidEnums.SoundID.StaticDeathSound11 },
            { "Static_End_Scene_Void", VoidEnums.SoundID.StaticEndSound },
            { "Static_End_Scene_Void11", VoidEnums.SoundID.StaticEndSound11 },

        }; 
    }

    private static void GetDataFromGame_Update(On.Menu.StoryGameStatisticsScreen.orig_GetDataFromGame orig, Menu.StoryGameStatisticsScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        if (package.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            string currentScene = self.scene.sceneID.value;

            if (StatisticSoundMap.Keys.Contains(currentScene))
            {
                self.PlaySound(StatisticSoundMap[currentScene]);
            }
        }
        orig(self, package);
    }
}
