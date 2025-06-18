using Kittehface.Framework20;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Watcher;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery;

internal static class MainMenuScene
{
    public static void Hook()
    {
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
        On.Menu.MainMenu.BackgroundScene += MainMenu_BackgroundScene;
        IL.Menu.MainMenu.ctor += MainMenu_ctor;
    }

    private static void MainMenu_ctor(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel cancel = c.DefineLabel();
        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdsfld<ModManager>("Watcher"),
            x => x.MatchBrfalse(out cancel)))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Br, cancel);
        }
        else logerr($"{nameof(MenuTinkery)}.{nameof(MainMenuScene)}.{nameof(MainMenu_ctor)}: match error");
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
