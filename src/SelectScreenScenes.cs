using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Menu;
using Newtonsoft.Json;
using VoidTemplate;
using static VoidTemplate.StaticStuff;
using static VoidTemplate.SaveManager;

namespace VoidTemplate;

internal static class SelectScreenScenes
{
    private const string slugbasedivider = "_SlugBaseSaveData_";
    public static ConditionalWeakTable<SlugcatSelectMenu.SaveGameData, List<SaveStateMiner.Result>> associatedSaveData = new();
    private static bool ParseSlugbaseData<T>(this string str, out T value)
    {
        value = default(T);
        string preparedString = Encoding.UTF8.GetString(Convert.FromBase64String(str));
        try
        {
            value = JsonConvert.DeserializeObject<T>(preparedString);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError($"Failed to parse slugbase data of type {nameof(T)}");
            return false;
        }

        
    }
    private static bool TryGetDataFromMine<T>(this List<SaveStateMiner.Result> list, string key, out T value)
    {
        value = default;
        foreach (var item in list)
        {
            if (item.name == key && item.data.ParseSlugbaseData<T>(out T val))
            {
                value = val;
                Plugin.logger.LogMessage($"TRY GET DATA found the matching data for key {key}: {val}");
                return true;
            }
        }
        return false;
    }
    public static void Hook()
    {
        On.Menu.MenuScene.BuildScene += CustomSelectScene;
        On.Menu.SlugcatSelectMenu.MineForSaveData += SlugcatSelectMenu_MineForSaveData;
    }

    private static SlugcatSelectMenu.SaveGameData SlugcatSelectMenu_MineForSaveData(On.Menu.SlugcatSelectMenu.orig_MineForSaveData orig, ProcessManager manager, SlugcatStats.Name slugcat)
    {
        var result = orig(manager, slugcat);
        if(slugcat != StaticStuff.TheVoid) return result;
        string[] progLinesFromMemory = manager.rainWorld.progression.GetProgLinesFromMemory();
        for (int i = 0; i < progLinesFromMemory.Length; i++)
        {
            string[] array = Regex.Split(progLinesFromMemory[i], "<progDivB>");
            if (array.Length == 2 && array[0] == "SAVE STATE" && BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) == TheVoid)
            {
                List<SaveStateMiner.Target> list =
                [
                    new SaveStateMiner.Target(SaveManager.endingDone, slugbasedivider, "<mwA>", 1024),
                    new SaveStateMiner.Target(SaveManager.VisitedFP6times, slugbasedivider, "<mwA>", 1024)
                ];
                List<SaveStateMiner.Result> results = SaveStateMiner.Mine(manager.rainWorld, array[1], list);
                Plugin.logger.LogMessage(array[1]);
                Plugin.logger.LogMessage($"miner found {results.Count} results");
                associatedSaveData.Add(result, results);
            }
        }
        return result;
    }

    private static void CustomSelectScene(On.Menu.MenuScene.orig_BuildScene orig, Menu.MenuScene self)
    {
        if(self.owner is SlugcatSelectMenu.SlugcatPageNewGame page 
            && page.slugcatNumber == TheVoid 
            && !SlugcatStats.SlugcatUnlocked(TheVoid, RWCustom.Custom.rainWorld))
        {
            self.sceneID = LockedSlugcat;
        }
        if(self.owner is SlugcatSelectMenu.SlugcatPageContinue page2
            && page2.slugcatNumber == TheVoid
            && !(self.sceneID == KarmaDeath11 || self.sceneID == KarmaDeath)) //these two are already handled
        {
            SaveState save = RWCustom.Custom.rainWorld.progression.GetOrInitiateSaveState(StaticStuff.TheVoid, null, self.menu.manager.menuSetup, false);
            if (save.GetEndingEncountered()) self.sceneID = DeathSceneID;
            else if (save.GetVisitedPebblesSixTimes()) self.sceneID = SelectFPScene;
            /*if (associatedSaveData.TryGetValue(page2.saveGameData, out var subdata))
            {
                Plugin.logger.LogMessage("found the associated data");
                if (subdata.TryGetDataFromMine<bool>(SaveManager.endingDone, out bool ended) && ended) self.sceneID = Sub;
                else if (subdata.TryGetDataFromMine<bool>(SaveManager.VisitedFP6times, out bool visited) && visited) self.sceneID = SelectFPScene;

            }*/
        }
        orig(self);
    }


}
