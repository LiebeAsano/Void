using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Nutils.hook;
using SlugBase.Assets;
using TheVoid;
using UnityEngine;

namespace VoidTemplate
{
    static class DeathHooks
    {
        public static void Hook()
        {
            On.RainWorldGame.GameOver += RainWorldGame_GameOver;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            On.Menu.KarmaLadder.KarmaSymbol.Update += KarmaSymbol_Update;
            On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
            On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;

            IL.Menu.KarmaLadderScreen.GetDataFromGame += KarmaLadderScreen_GetDataFixMSCStupidBug;

            if (Plugin.DevEnabled)
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
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdarg(0),
                    i => i.MatchLdcI4(4));
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<int,KarmaLadderScreen,KarmaLadderScreen.SleepDeathScreenDataPackage,int>>(
                    (re,self,package) =>
                {
                    if (package.saveState != null && package.saveState.saveStateNumber == Plugin.TheVoid)
                        if (self.ID == ProcessManager.ProcessID.GhostScreen)
                            return self.preGhostEncounterKarmaCap;
                        else
                            return self.karma.y;
                    return re;
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
        {
            if (self.bites < 2 && grasp.grabber is Player player && player.slugcatStats.name == Plugin.TheVoid)
            {
                self.bites--;
                self.room.PlaySound((self.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk.pos);
                self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
                grasp.Release();
                self.Destroy();
                return;
            }
            orig(self,grasp, eu);
        }

        private static void SlugcatPage_AddImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, SlugcatSelectMenu.SlugcatPage self, bool ascended)
        {
            if (self.slugcatNumber == Plugin.TheVoid && self is SlugcatSelectMenu.SlugcatPageContinue menu &&
                menu.saveGameData.redsExtraCycles)
            {
                self.imagePos = new Vector2(683f, 484f);
                self.slugcatDepth = 3f;
                self.sceneOffset = Vector2.zero;
                self.sceneOffset.x = (1366f - self.menu.manager.rainWorld.options.ScreenSize.x) / 2f;

                var id = menu.saveGameData.karmaCap == 10 ? new MenuScene.SceneID("karma_death_void_karma11") : new MenuScene.SceneID("karma_death_void");
                Debug.Log($"[The Void] Load Image {id}");
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

            orig(self,ascended);
        }

        private static void KarmaSymbol_Update(On.Menu.KarmaLadder.KarmaSymbol.orig_Update orig, KarmaLadder.KarmaSymbol self)
        {
            var flag = ModManager.MSC && self.parent.displayKarma.x == self.parent.moveToKarma &&
                       (self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.KarmaToMinScreen ||
                        self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.VengeanceGhostScreen ||
                        (ModManager.Expedition && self.menu.manager.rainWorld.ExpeditionMode &&
                         self.parent.moveToKarma == 0));
            if (!flag && ModManager.MSC && self.parent.displayKarma.x == self.parent.moveToKarma && 
                self.menu is KarmaLadderScreen screen && screen.saveState?.saveStateNumber == Plugin.TheVoid 
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
            if (storyGameCharacter == Plugin.TheVoid && self.saveGameData[storyGameCharacter].redsExtraCycles)
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
            if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber == Plugin.TheVoid &&
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

            if (self.GetStorySession.saveState.saveStateNumber == Plugin.TheVoid && (!ModManager.Expedition  || !self.rainWorld.ExpeditionMode))
            {
                if (self.manager.upcomingProcess != null) return;

                self.manager.musicPlayer?.FadeOutAllSongs(20f);
                self.GetStorySession.saveState.redExtraCycles = true;
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
                Debug.Log("[The Void] Exit to Statistics");
                return;
            }
            orig(self);
        }

        private static void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
        {
            if (self.session is StoryGameSession session && session.characterStats.name == Plugin.TheVoid
                && (session.saveState.deathPersistentSaveData.karma == 0 || session.saveState.deathPersistentSaveData.karma == 10) && (!ModManager.Expedition || !self.rainWorld.ExpeditionMode))
            {
                self.GoToRedsGameOver();
                return;
            }
            orig(self, dependentOnGrasp);
        }

    }
}
