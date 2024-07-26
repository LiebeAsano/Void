using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase.Assets;
using VoidTemplate;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Threading;
using System.Timers;

namespace VoidTemplate;

static class DeathHooks
{
    public static void Hook()
    {
        On.RainWorldGame.GameOver += GenericGameOver;
        On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
        On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
        On.Menu.KarmaLadder.KarmaSymbol.Update += PulsateKarmaSymbol;
        //On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
        On.RainWorldGame.ExitToMenu += ExitToMenuGameOver;
        Application.quitting += ApplicationQuitGameOver;    
        

        IL.Menu.KarmaLadderScreen.GetDataFromGame += KarmaLadderScreen_GetDataFixMSCStupidBug;

        if (_Plugin.DevEnabled)
        {
            On.Menu.KarmaLadder.KarmaSymbol.ctor +=
                (orig, self, menu, owner, pos, container, foregroundContainer, karma) =>
                {
                    orig(self, menu, owner, pos, container, foregroundContainer, karma);
                    Debug.Log(
                        $"[The Void] {karma}, {(menu as KarmaLadderScreen).karma}, {(menu as KarmaLadderScreen).preGhostEncounterKarmaCap}");
                };
        }
    }

    private static void KarmaLadderScreen_GetDataFixMSCStupidBug(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if(c.TryGotoNext(MoveType.After, i => i.MatchLdarg(0),
            i => i.MatchLdcI4(4)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<int, KarmaLadderScreen, KarmaLadderScreen.SleepDeathScreenDataPackage, int>>(
            (re, self, package) =>
            {
                if (package.saveState != null && package.saveState.saveStateNumber == StaticStuff.TheVoid)
                    if (self.ID == ProcessManager.ProcessID.GhostScreen)
                        return self.preGhostEncounterKarmaCap;
                    else
                        return self.karma.y;
                return re;
            });
        }

    }

    private static void SlugcatPage_AddImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, SlugcatSelectMenu.SlugcatPage self, bool ascended)
    {
        if (self.slugcatNumber == StaticStuff.TheVoid && self is SlugcatSelectMenu.SlugcatPageContinue menu &&
            menu.saveGameData.redsExtraCycles)
        {
            self.imagePos = new Vector2(683f, 484f);
            self.slugcatDepth = 3f;
            self.sceneOffset = Vector2.zero;
            self.sceneOffset.x = (1366f - self.menu.manager.rainWorld.options.ScreenSize.x) / 2f;

            var id = menu.saveGameData.karmaCap == 10 ? StaticStuff.KarmaDeath11 : StaticStuff.KarmaDeath;
            self.slugcatImage = new InteractiveMenuScene(self.menu, self, id);

            self.subObjects.Add(self.slugcatImage);

            if (CustomScene.Registry.TryGet(id, out var customScene))
            {
                self.markOffset = (customScene.MarkPos ?? self.markOffset);
                self.glowOffset = (customScene.GlowPos ?? self.glowOffset);
                self.sceneOffset = (customScene.SelectMenuOffset ?? self.sceneOffset);
                self.slugcatDepth = (customScene.SlugcatDepth ?? self.slugcatDepth);
            }
            if (self.HasMark)
            {
                self.markSquare = new FSprite("pixel")
                {
                    scale = 14f,
                    color = Color.Lerp(self.effectColor, Color.white, 0.7f)
                };
                self.Container.AddChild(self.markSquare);
                self.markGlow = new FSprite("Futile_White")
                {
                    shader = self.menu.manager.rainWorld.Shaders["FlatLight"],
                    color = self.effectColor
                };
                self.Container.AddChild(self.markGlow);
            }

            return;
        }

        orig(self, ascended);
    }

    private static void PulsateKarmaSymbol(On.Menu.KarmaLadder.KarmaSymbol.orig_Update orig, KarmaLadder.KarmaSymbol self)
    {
        
        var flag = ModManager.MSC 
            && self.parent.displayKarma.x == self.parent.moveToKarma 
            && (self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.KarmaToMinScreen || self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.VengeanceGhostScreen || (ModManager.Expedition 
                && self.menu.manager.rainWorld.ExpeditionMode 
                && self.parent.moveToKarma == 0));
        if (!flag && ModManager.MSC && self.parent.displayKarma.x == self.parent.moveToKarma &&
            self.menu is KarmaLadderScreen screen && screen.saveState?.saveStateNumber == StaticStuff.TheVoid
            && self.parent.moveToKarma == 0 && self.parent.menu.ID == ProcessManager.ProcessID.DeathScreen)
        {
            self.waitForAnimate++;
            if (self.waitForAnimate >= 50)
                if (self.displayKarma.x == 0)
                    self.pulsateCounter++;
        }
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

    private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
    {

        if (self.GetStorySession.saveState.saveStateNumber == StaticStuff.TheVoid && (!ModManager.Expedition || !self.rainWorld.ExpeditionMode))
        {
            if (self.manager.upcomingProcess != null) return;

            self.manager.musicPlayer?.FadeOutAllSongs(20f);
            self.GetStorySession.saveState.redExtraCycles = true;
            SaveState save = self.rainWorld.progression.GetOrInitiateSaveState(StaticStuff.TheVoid, null, self.manager.menuSetup, false);
            save.SetVoidCatDead(true);
            KarmaHooks.ForceFailed = true;
            if (ModManager.CoopAvailable)
            {
                int num = 0;
                using IEnumerator<Player> enumerator =
                    (from x in self.session.game.Players select x.realizedCreature as Player).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Player player = enumerator.Current;
                    self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
                    num++;
                }
            }
            else
                self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);


            self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
            self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics, 10f);
            return;
        }
        orig(self);
    }
    #region GameOverConditions

    private static void ApplicationQuitGameOver()
    {
        RainWorld rainWorld = Object.FindObjectOfType<RainWorld>();
        if (rainWorld != null
            && rainWorld.processManager is ProcessManager manager
            && manager.currentMainLoop is RainWorldGame game
            && VoidSpecificGameOverCondition(game))
            game.GoToRedsGameOver();
    }
    private static bool VoidSpecificGameOverCondition(RainWorldGame rainWorldGame)
    {
        return rainWorldGame.session is StoryGameSession session
            && session.characterStats.name == StaticStuff.TheVoid
            && (session.saveState.deathPersistentSaveData.karma == 0 || session.saveState.deathPersistentSaveData.karma == 10)
            && (!ModManager.Expedition || !rainWorldGame.rainWorld.ExpeditionMode);
    }

    private static void ExitToMenuGameOver(On.RainWorldGame.orig_ExitToMenu orig, RainWorldGame self)
    {
        if(VoidSpecificGameOverCondition(self)) self.GoToRedsGameOver();
        orig(self);
    }
    private static void GenericGameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
    {
        if (VoidSpecificGameOverCondition(self) && dependentOnGrasp == null)
        {
            self.GoToRedsGameOver();
        }
        orig(self, dependentOnGrasp);
    }
    #endregion
}