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
using IL.Menu.Remix;

namespace VoidTemplate.MenuTinkery;

internal static class SelectScreenScenes
{
    public static void Hook()
    {
        On.Menu.MenuScene.BuildScene += CustomSelectScene;

        On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
    }
    private static void loginf(object e) => _Plugin.logger.LogInfo(e);

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
            else if (save.GetEndingEncountered()) self.sceneID = SelectEndingScene;
            else if (save.miscWorldSaveData.SSaiConversationsHad >= 6) self.sceneID = SelectFPScene;
            else if (save.deathPersistentSaveData.karmaCap > 3) self.sceneID = SelectKarma5Scene; 
            //if none of them work, the default scene happens, which is default ready to play slugcat
        }
        orig(self);
    }
    private static void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
    {
        if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber == StaticStuff.TheVoid &&
            self.GetSaveGameData(self.slugcatPageIndex) != null &&
            self.GetSaveGameData(self.slugcatPageIndex).redsExtraCycles)
        {
            self.startButton.menuLabel.text = self.Translate("STATISTICS");
        }
        else
            orig(self);
    }
    private static void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    {
        if (storyGameCharacter == StaticStuff.TheVoid && self.saveGameData[storyGameCharacter].redsExtraCycles)
        {
            self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
            self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
            self.PlaySound(SoundID.MENU_Switch_Page_Out);
            return;
        }
        orig(self, storyGameCharacter);

    }


}
