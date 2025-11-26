using Menu;
using static VoidTemplate.VoidEnums.SceneID;
using static VoidTemplate.VoidEnums.SlugcatID;
using static VoidTemplate.Useful.Utils;
using Newtonsoft.Json.Linq;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.MenuTinkery;

public static class SelectScreenScenes
{
	public static void Hook()
	{
		On.Menu.MenuScene.BuildScene += CustomSelectScene;

		On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
	}

	private static void CustomSelectScene(On.Menu.MenuScene.orig_BuildScene orig, Menu.MenuScene self)
	{
		if (self.owner is SlugcatSelectMenu.SlugcatPageNewGame page
			&& page.slugcatNumber == Void
			&& !SlugcatStats.SlugcatUnlocked(Void, RWCustom.Custom.rainWorld))
		{
			self.sceneID = LockedSlugcat;
		}
		if (self.owner is SlugcatSelectMenu.SlugcatPageContinue page2
			&& page2.slugcatNumber == Void)
		{
			SaveState save = RWCustom.Custom.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, self.menu.manager.menuSetup, false);
            if (save.GetVoidCatDead() && page2.saveGameData.karmaCap == 10) self.sceneID = KarmaDeath11;
			else if (save.GetVoidCatDead() && page2.saveGameData.karmaCap < 10) self.sceneID = KarmaDeath;
            else if (save.GetVoidEndingTree() && save.deathPersistentSaveData.karmaCap == 10) self.sceneID = SelectSlugpups11Scene;
            else if (save.GetEndingEncountered() && save.deathPersistentSaveData.karmaCap == 10) self.sceneID = SelectEnding11Scene;
			else if (save.deathPersistentSaveData.karmaCap == 10) self.sceneID = SelectKarma11Scene;
            else if (save.GetVoidEndingTree()) self.sceneID = SelectSlugpupsScene;
            else if (save.GetEndingEncountered()) self.sceneID = SelectEndingScene;
			else if (save.GetVoidMeetMoon()) self.sceneID = SelectFPScene;
			else if (save.deathPersistentSaveData.karmaCap >= 4) self.sceneID = SelectKarma5Scene;
			//if none of them work, the default scene happens, which is default ready to play slugcat
		}
		orig(self);
	}
	private static void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
	{
		if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber == Void &&
			self.GetSaveGameData(self.slugcatPageIndex) is not null)
		{
            RainWorld rainWorld = self.manager.rainWorld;
            SaveState save = rainWorld.progression.GetOrInitiateSaveState(Void, null, self.manager.menuSetup, false);
            string text = "CONTINUE";
			if (self.restartChecked) text = "NEW GAME";
			else if (save.GetVoidCatDead()) text = "STATISTICS";
			
			self.startButton.menuLabel.text = self.Translate(text);
		}
		else
			orig(self);
	}



}
