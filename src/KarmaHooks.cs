using System;
using System.IO;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using VoidTemplate.Useful;
using UnityEngine;


namespace VoidTemplate
{
    static class KarmaHooks
    {
        public static void Hook()
        {

            //On.Menu.SleepAndDeathScreen.AddBkgIllustration += SleepAndDeathScreen_AddBkgIllustration;
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

            IL.World.SpawnGhost += KarmaReqTinker;



            On.Menu.KarmaLadder.ctor += KarmaLadder_ctor;
            On.Menu.KarmaLadder.GoToKarma += KarmaLadder_GoToKarma;

            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;

            //IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctorIL;
        }

        private static void KarmaReqTinker(ILContext il)
        {
            ILCursor c = new(il);
            // bool flag = this.game.setupValues.ghosts > 0
            // || GhostWorldPresence.SpawnGhost(ghostID,
            // (this.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma <replace with karmacap, method thinks void is always at max karma>,
            // (this.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap,
            // num,
            // this.game.StoryCharacter == SlugcatStats.Name.Red <OR VOID> );
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<DeathPersistentSaveData>(nameof(DeathPersistentSaveData.karma))))
            {
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<World, int>>((self) =>
                {
                    return (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
                });
            }
            else logerr(new System.Diagnostics.StackTrace());
            if(c.TryGotoNext(MoveType.After, x => x.MatchCall("ExtEnum`1<SlugcatStats/Name>", "op_Equality")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, World, bool>>((orig, world) =>
                {
                    return orig || world.game.StoryCharacter == VoidEnums.SlugcatID.TheVoid;
                });
            }
            else logerr(new System.Diagnostics.StackTrace());
        }

        private static void loginf(object e) => _Plugin.logger.LogInfo(e);
        private static void logerr(object e) => _Plugin.logger.LogError(e);

        private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
        {
            orig(self, saveStateNumber);
            if (saveStateNumber == VoidEnums.SlugcatID.TheVoid)
            {
                ForceFailed = false;
                RainWorld rainWorld = self.rainWorld;
                SaveState save = rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.TheVoid, null, self.rainWorld.processManager.menuSetup, false);
                save.SetVoidCatDead(false);
                save.SetEndingEncountered(false);
            }
        }

        public static bool ForceFailed = false;

        private static void KarmaLadder_GoToKarma(On.Menu.KarmaLadder.orig_GoToKarma orig, KarmaLadder self, int newGoalKarma, bool displayMetersOnRest)
        {
            orig(self, newGoalKarma, displayMetersOnRest);
            if (self.karmaSymbols[0].sprites[self.karmaSymbols[0].KarmaSprite].element.name.Contains("blank"))
            {
                self.movementShown = true;
                self.showEndGameMetersCounter = 85;
            }
        }

        private static void KarmaLadder_ctor(On.Menu.KarmaLadder.orig_ctor orig, KarmaLadder self, Menu.Menu menu, MenuObject owner, Vector2 pos, HUD.HUD hud, IntVector2 displayKarma, bool reinforced)
        {
            var screen = menu as KarmaLadderScreen;
            bool needInsert = false;
            var lastScreen = screen.ID;

            if (screen.saveState.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
            {
                if ((screen.saveState.redExtraCycles || ForceFailed) && screen.saveState.deathPersistentSaveData.karmaCap != 10)
                {
                    screen.ID = MoreSlugcatsEnums.ProcessID.KarmaToMinScreen;
                    needInsert = true;
                }
                else
                {
                    loginf("here save string should have been logged");
                }
            }

            orig(self, menu, owner, pos, hud, displayKarma, reinforced);
            if (needInsert)
            {
                self.karmaSymbols.Insert(0, new KarmaLadder.KarmaSymbol(menu, self,
                    new Vector2(0f, 0f), self.containers[self.MainContainer],
                    self.containers[self.FadeCircleContainer], new IntVector2(-1, 0)));
                self.subObjects.Add(self.karmaSymbols[0]);
                self.karmaSymbols[0].sprites[self.karmaSymbols[0].KarmaSprite].MoveBehindOtherNode(
                    self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);
                self.karmaSymbols[0].sprites[self.karmaSymbols[0].RingSprite].MoveBehindOtherNode(
                    self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);
                self.karmaSymbols[0].sprites[self.karmaSymbols[0].LineSprite].MoveBehindOtherNode(
                    self.karmaSymbols[1].sprites[self.karmaSymbols[1].KarmaSprite]);

                self.karmaSymbols[0].sprites[self.karmaSymbols[0].GlowSprite(0)].MoveBehindOtherNode(
                    self.karmaSymbols[1].sprites[self.karmaSymbols[1].GlowSprite(0)]);
                self.karmaSymbols[0].sprites[self.karmaSymbols[0].GlowSprite(1)].MoveBehindOtherNode(
                    self.karmaSymbols[1].sprites[self.karmaSymbols[1].GlowSprite(0)]);
                foreach (var symbol in self.karmaSymbols)
                    symbol.displayKarma.x++;
                self.displayKarma.x++;
                self.scroll = self.displayKarma.x;
                self.lastScroll = self.displayKarma.x;
            }
        }

        private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self, package);
            MenuScene.SceneID sceneID = null;
            if (self.saveState?.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
            {
                if (self.IsSleepScreen && self.saveState.deathPersistentSaveData.karmaCap != 10)
                {
                    sceneID = VoidEnums.SceneID.SleepScene;
                }
                else if (self.IsSleepScreen && self.saveState.deathPersistentSaveData.karmaCap == 10)
                {
                    sceneID = VoidEnums.SceneID.SleepScene11;
                }
                else if ((self.IsDeathScreen || self.IsStarveScreen) && self.saveState.deathPersistentSaveData.karmaCap != 10)
                {
                    sceneID = VoidEnums.SceneID.DeathScene;
                }
                else if ((self.IsDeathScreen || self.IsStarveScreen) && self.saveState.deathPersistentSaveData.karmaCap == 10)
                {
                    sceneID = VoidEnums.SceneID.DeathScene11;
                }
                
                if (sceneID != null && sceneID.Index != -1)
                {
                    self.scene.RemoveSprites();
                    self.pages[0].subObjects.RemoveAll(i => i is InteractiveMenuScene);
                    self.scene = new InteractiveMenuScene(self, self.pages[0], sceneID);
                    self.pages[0].subObjects.Add(self.scene);
                    for (int i = self.scene.depthIllustrations.Count - 1; i >= 0; i--)
                        self.scene.depthIllustrations[i].sprite.MoveToBack();
                }
            }
        }

        

        /*private static void SlugcatPageContinue_ctorIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                while (c.TryGotoNext(MoveType.After, i => i.MatchLdarg(4),
                           i => i.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
                           i => i.MatchLdfld<IntVector2>("x")))
                {
                    c.Emit(OpCodes.Ldarg_S, (byte)4);
                    c.Emit(OpCodes.Ldarg_0);

                    c.EmitDelegate<Func<int, SlugcatStats.Name, SlugcatSelectMenu.SlugcatPageContinue, int>>((x, name, self) =>
                    {
                        if (name == _Plugin.TheVoid && self.saveGameData.karma == 10)
                            return 9;
                        return x;
                    });
                }
                ILCursor c2 = new ILCursor(il);
                while (c2.TryGotoNext(MoveType.After, i => i.MatchLdarg(4),
                           i => i.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
                           i => i.MatchLdfld<IntVector2>("y")))
                {
                    c2.Emit(OpCodes.Ldarg_S, (byte)4);
                    c2.Emit(OpCodes.Ldarg_0);

                    c2.EmitDelegate<Func<int, SlugcatStats.Name, SlugcatSelectMenu.SlugcatPageContinue, int>>((y, name, self) =>
                    {
                        if (name == _Plugin.TheVoid && self.saveGameData.karma == 10)
                            return 6;
                        return y;
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }*/
    }

}
