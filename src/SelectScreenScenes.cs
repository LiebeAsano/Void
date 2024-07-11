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
    public static void Hook()
    {
        On.Menu.MenuScene.BuildScene += CustomSelectScene;
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
            && page2.slugcatNumber == TheVoid)
        {
            SaveState save = RWCustom.Custom.rainWorld.progression.GetOrInitiateSaveState(StaticStuff.TheVoid, null, self.menu.manager.menuSetup, false);
            if (save.GetVoidCatDead() && page2.saveGameData.karmaCap == 10) self.sceneID = KarmaDeath11;
            else if (save.GetVoidCatDead() && page2.saveGameData.karmaCap < 10) self.sceneID = KarmaDeath;
            else if (save.GetEndingEncountered()) self.sceneID = DeathSceneID;
            else if (save.GetVisitedPebblesSixTimes()) self.sceneID = SelectFPScene;
            //if none of them work, the default scene happens
        }
        orig(self);
    }


}
