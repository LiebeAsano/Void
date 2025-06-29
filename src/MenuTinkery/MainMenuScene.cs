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
        if (!OptionAccessors.DisableMenuBackGround)
            return VoidEnums.SceneID.MainMenuSceneVoid;
        return orig(self);   
    }

    public static void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);
        MenuScene.SceneID scene;
        scene = VoidEnums.SceneID.MainMenuSceneVoid;
        if (!OptionAccessors.DisableMenuBackGround)
        {
            scene = VoidEnums.SceneID.MainMenuSceneVoid;
        }
        else
        {
            if (ModManager.MMF)
            {
                if (scene == VoidEnums.SceneID.MainMenuSceneVoid)
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
