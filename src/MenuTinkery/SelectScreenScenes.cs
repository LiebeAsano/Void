using Menu;
using static VoidTemplate.VoidEnums.SceneID;
using static VoidTemplate.VoidEnums.SlugcatID;

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
			else if (save.GetEndingEncountered() && save.deathPersistentSaveData.karmaCap == 10) self.sceneID = SelectEnding11Scene;
			else if (save.deathPersistentSaveData.karmaCap == 10) self.sceneID = SelectKarma11Scene;
			else if (save.GetEndingEncountered()) self.sceneID = SelectEndingScene;
			else if (save.GetVoidMeetMoon()) self.sceneID = SelectFPScene;
			else if (save.deathPersistentSaveData.karmaCap >= 4) self.sceneID = SelectKarma5Scene;
			//if none of them work, the default scene happens, which is default ready to play slugcat
		}
		orig(self);
	}
	private static void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
	{
		if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber == VoidEnums.SlugcatID.Void &&
			self.GetSaveGameData(self.slugcatPageIndex) != null &&
			self.GetSaveGameData(self.slugcatPageIndex).redsExtraCycles)
		{
			var text = self.restartChecked ? "NEW GAME" : "STATISTICS";
			self.startButton.menuLabel.text = self.Translate(text);
		}
		else
			orig(self);
	}
	private static void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
	{
		if (storyGameCharacter == VoidEnums.SlugcatID.Void && self.saveGameData[storyGameCharacter].redsExtraCycles)
		{
			self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
			self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
			self.PlaySound(SoundID.MENU_Switch_Page_Out);
			return;
		}
		orig(self, storyGameCharacter);

	}


}
