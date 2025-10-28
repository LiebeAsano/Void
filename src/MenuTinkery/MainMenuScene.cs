using Kittehface.Framework20;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using VoidTemplate.OptionInterface;
using Watcher;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery;

public static class MainMenuScene
{
    public static void Hook()
    {
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
        On.Menu.MainMenu.BackgroundScene += MainMenu_BackgroundScene;
        IL.Menu.MainMenu.ctor += MainMenu_ctor;
    }

    public static void MainMenu_ctor(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel cancel = c.DefineLabel();

        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdsfld<ModManager>("Watcher"),
            x => x.MatchBrfalse(out cancel)))
        {
            c.Emit(OpCodes.Call, typeof(OptionAccessors).GetProperty("DisableMenuBackGround").GetGetMethod());
            c.Emit(OpCodes.Brtrue, cancel);

            c.MoveAfterLabels();
            c.Emit(OpCodes.Br, cancel);
        }
        else logerr($"{nameof(MenuTinkery)}.{nameof(MainMenuScene)}.{nameof(MainMenu_ctor)}: match error");
    }

    public static MenuScene.SceneID MainMenu_BackgroundScene(On.Menu.MainMenu.orig_BackgroundScene orig, MainMenu self)
    {
        MenuScene.SceneID scene;
        scene = VoidEnums.SceneID.MainMenuSceneMonkSurvHunt;
        if (!OptionAccessors.DisableMenuBackGround && (SaveManager.ExternalSaveData.MonkAscended || SaveManager.ExternalSaveData.SurvAscended || SaveManager.ExternalSaveData.ViyUnlocked))
        {
            if (SaveManager.ExternalSaveData.MonkAscended && SaveManager.ExternalSaveData.SurvAscended && SaveManager.ExternalSaveData.ViyUnlocked)
                scene = VoidEnums.SceneID.MainMenuSceneMonkSurvHunt;
            else if (SaveManager.ExternalSaveData.MonkAscended && SaveManager.ExternalSaveData.SurvAscended)
                scene = VoidEnums.SceneID.MainMenuSceneMonkSurv;
            else if (SaveManager.ExternalSaveData.MonkAscended && SaveManager.ExternalSaveData.ViyUnlocked)
                scene = VoidEnums.SceneID.MainMenuSceneMonkHunt;
            else if (SaveManager.ExternalSaveData.SurvAscended && SaveManager.ExternalSaveData.ViyUnlocked)
                scene = VoidEnums.SceneID.MainMenuSceneSurvHunt;
            else if (SaveManager.ExternalSaveData.ViyUnlocked)
                scene = VoidEnums.SceneID.MainMenuSceneHunt;
            else if (SaveManager.ExternalSaveData.MonkAscended)
                scene = VoidEnums.SceneID.MainMenuSceneMonk;
            else if (SaveManager.ExternalSaveData.SurvAscended)
                scene = VoidEnums.SceneID.MainMenuSceneSurv;
            return scene;
        }
        return orig(self);   
    }

    public static void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);
        if (ID == ProcessManager.ProcessID.MainMenu)
        {
            MenuScene.SceneID scene;
            scene = VoidEnums.SceneID.MainMenuSceneMonkSurvHunt;
            if (self.rainWorld.progression.miscProgressionData.monkEndingID == 1 && !SaveManager.ExternalSaveData.MonkAscended)
            {
                SaveManager.ExternalSaveData.MonkAscended = true;
            }
            if (self.rainWorld.progression.miscProgressionData.survivorEndingID == 1 && !SaveManager.ExternalSaveData.SurvAscended)
            {
                SaveManager.ExternalSaveData.SurvAscended = true;
            }
            SaveState save = self.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, self.menuSetup, false);
            if (save.GetVoidCatDead() && save.deathPersistentSaveData.karmaCap == 10)
            {
                SaveManager.ExternalSaveData.VoidDead = true;
                SaveManager.ExternalSaveData.VoidKarma11 = true;
                SaveManager.ExternalSaveData.ViyUnlocked = true;
            }
            if (!OptionAccessors.DisableMenuBackGround && (SaveManager.ExternalSaveData.MonkAscended || SaveManager.ExternalSaveData.SurvAscended || SaveManager.ExternalSaveData.ViyUnlocked))
            {
                if (SaveManager.ExternalSaveData.MonkAscended && SaveManager.ExternalSaveData.SurvAscended && SaveManager.ExternalSaveData.ViyUnlocked)
                    scene = VoidEnums.SceneID.MainMenuSceneMonkSurvHunt;
                else if (SaveManager.ExternalSaveData.MonkAscended && SaveManager.ExternalSaveData.SurvAscended)
                    scene = VoidEnums.SceneID.MainMenuSceneMonkSurv;
                else if (SaveManager.ExternalSaveData.MonkAscended && SaveManager.ExternalSaveData.ViyUnlocked)
                    scene = VoidEnums.SceneID.MainMenuSceneMonkHunt;
                else if (SaveManager.ExternalSaveData.SurvAscended && SaveManager.ExternalSaveData.ViyUnlocked)
                    scene = VoidEnums.SceneID.MainMenuSceneSurvHunt;
                else if (SaveManager.ExternalSaveData.ViyUnlocked)
                    scene = VoidEnums.SceneID.MainMenuSceneHunt;
                else if (SaveManager.ExternalSaveData.MonkAscended)
                    scene = VoidEnums.SceneID.MainMenuSceneMonk;
                else if (SaveManager.ExternalSaveData.SurvAscended)
                    scene = VoidEnums.SceneID.MainMenuSceneSurv;
            }
            else
            {
                if (ModManager.MMF)
                {
                    if (scene == VoidEnums.SceneID.MainMenuSceneMonkSurvHunt
                        || scene == VoidEnums.SceneID.MainMenuSceneMonkSurv
                        || scene == VoidEnums.SceneID.MainMenuSceneMonkHunt
                        || scene == VoidEnums.SceneID.MainMenuSceneSurvHunt
                        || scene == VoidEnums.SceneID.MainMenuSceneHunt
                        || scene == VoidEnums.SceneID.MainMenuSceneMonk
                        || scene == VoidEnums.SceneID.MainMenuSceneSurv)
                    {
                        scene = MenuScene.SceneID.MainMenu;
                    }
                    else
                    {
                        scene = self.rainWorld.options.titleBackground;
                    }
                }
                else if (!self.rainWorld.dlcsInstalled.Contains("downpour"))
                {
                    scene = MenuScene.SceneID.MainMenu;
                }
                else 
                {
                    scene = MenuScene.SceneID.MainMenu_Downpour;
                }
            }
            self.rainWorld.options.titleBackground = scene;
            self.rainWorld.options.subBackground = scene;
        }
    }
}
