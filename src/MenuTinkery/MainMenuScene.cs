using Kittehface.Framework20;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Watcher;

namespace VoidTemplate.MenuTinkery;

internal static class MainMenuScene
{
    public static void Hook()
    {
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
        On.Menu.MainMenu.BackgroundScene += MainMenu_BackgroundScene;
    }

    private static MenuScene.SceneID MainMenu_BackgroundScene(On.Menu.MainMenu.orig_BackgroundScene orig, MainMenu self)
    {
        return VoidEnums.SceneID.MainMenuSceneVoid;
    }

    private static void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);

        var scene = VoidEnums.SceneID.MainMenuSceneVoid;
        
        self.rainWorld.options.titleBackground = scene;

        self.rainWorld.options.subBackground = scene;
    }
}
