using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kittehface.Framework20;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Nutils.hook;
using RWCustom;
using TheVoid;
using UnityEngine;
using LevelSelector = IL.Menu.LevelSelector;

namespace VoidTemplate
{
    static class KarmaHooks
    {
        public static void Hook()
        {
            IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctorIL;
            IL.SaveState.GhostEncounter += SaveState_GhostEncounterIL;

            On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;
            On.StoryGameSession.ctor += StoryGameSession_ctor;

            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.Menu.SleepAndDeathScreen.AddBkgIllustration += SleepAndDeathScreen_AddBkgIllustration;
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

            On.GhostWorldPresence.SpawnGhost += GhostWorldPresence_SpawnGhost;
            On.GhostConversation.AddEvents += GhostConversation_AddEvents;
            IL.Ghost.Update += Ghost_UpdateIL;

            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;

            On.SlugcatStats.SlugcatUnlocked += SlugcatStats_SlugcatUnlocked;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;

            On.Menu.KarmaLadder.ctor += KarmaLadder_ctor;
            On.Menu.KarmaLadder.GoToKarma += KarmaLadder_GoToKarma;

            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;

        }

        private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
        {
            orig(self, saveStateNumber);
            if (saveStateNumber == Plugin.TheVoid)
                ForceFailed = false;
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

            if (screen.saveState.saveStateNumber == Plugin.TheVoid)
            {
                if (screen.saveState.redExtraCycles || ForceFailed)
                {
                    screen.ID = MoreSlugcatsEnums.ProcessID.KarmaToMinScreen;
                    needInsert = true;
                }
                else
                {
                    Debug.Log($"[The Void] {screen.saveState.SaveToString()}");
                }
            }

            orig(self,menu, owner, pos, hud, displayKarma, reinforced);
            if (needInsert)
            {
                self.karmaSymbols.Insert(0,new KarmaLadder.KarmaSymbol(menu, self, 
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

        private static void Player_ClassMechanicsSaint(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                    i=>i.MatchLdcI4(1),
                    i => i.MatchStloc(15),
                    i => i.MatchLdloc(18),
                    i => i.MatchIsinst<Creature>());

                var label = c.DefineLabel();
                var label2 = c.DefineLabel();
                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Func<Creature, bool>>((self) =>
                    self is Player player && player.slugcatStats.name == Plugin.TheVoid && player.KarmaCap == 10);
                c.Emit(OpCodes.Brtrue_S, label);
                c.GotoNext(MoveType.After,
                    i => i.MatchCallvirt<Creature>("Die"));
                c.Emit(OpCodes.Br, label2);
                c.MarkLabel(label);
                c.Emit(OpCodes.Pop);
                c.MarkLabel(label2);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += SlugcatPageNewGame_ctor;
        }
        private static void Ghost_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After,i => i.MatchLdfld<StoryGameSession>("saveStateNumber"),
                    i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
                    i => i.MatchCall(out var call) && call.Name.Contains("op_Equality"));
                var label = (ILLabel)c.Next.Operand;
                Debug.Log(label.Target);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, Ghost, bool>>((re, self) =>
                    re || ((self.room.game.session is StoryGameSession session) &&
                           session.saveStateNumber == Plugin.TheVoid));

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static string GetGhostConversationPath(InGameTranslator.LanguageID id,Conversation.ID convId,bool hasMark)
        {
            var translator = Custom.rainWorld.inGameTranslator;
            var path = $"{translator.SpecificTextFolderDirectory(id)}/{convId}_";
            path += hasMark ? "mark.txt" : "nomark.txt";
            return path;
        }



        //dialogue path : text/text_{language id}/ghost_{ghost region name (lower)}_{mark/nomark}.txt
        //eg: text/text_rus/ghost_sb_mark.txt
        //If the corresponding language dialogue cannot be found, the <English> version will be read.
        //If it is still not found, read the original in-game text (a prompt will be added for DEBUG)
        private static void GhostConversation_AddEvents(On.GhostConversation.orig_AddEvents orig, GhostConversation self)
        {
            if (self.ghost.room.game.session is StoryGameSession session &&
                session.saveStateNumber == Plugin.TheVoid)
            {
                var path = AssetManager.ResolveFilePath(GetGhostConversationPath(Custom.rainWorld.inGameTranslator.currentLanguage, self.id,
                    session.saveState.deathPersistentSaveData.theMark));
                if (!File.Exists(path))
                {
                    path = AssetManager.ResolveFilePath(GetGhostConversationPath(InGameTranslator.LanguageID.English, self.id,
                        session.saveState.deathPersistentSaveData.theMark));
                }

                if (File.Exists(path))
                {
                    Debug.Log($"[The Void] Load Conv In file : {path}");
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var split = LocalizationTranslator.ConsolidateLineInstructions(line);
                        if(split.Length == 3)
                            self.events.Add(new Conversation.TextEvent(self, int.Parse(split[0]), 
                                split[1], int.Parse(split[2])));
                        else
                            self.events.Add(new Conversation.TextEvent(self,0,line,0));
                    }

                    return;
                }
                Debug.Log($"[The Void] Load FallBack");

                //TODO : DELETE THIS AFTER TEST
                self.events.Add(new Conversation.TextEvent(self,0,$"Can't find conv at {GetGhostConversationPath(InGameTranslator.LanguageID.English, self.id,
                    session.saveState.deathPersistentSaveData.theMark)}<LINE> for {self.id}",0));

            }
            orig(self);
        }

        private static bool GhostWorldPresence_SpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.session is StoryGameSession session &&
                session.saveStateNumber == Plugin.TheVoid)
                return true;
            var re =orig(ghostID,karma,karmaCap,ghostPreviouslyEncountered,playingAsRed);
            return re;
        }

        private static void SaveState_GhostEncounterIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdcI4(9));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, SaveState,int>>((re, self) =>
                    self.saveStateNumber == Plugin.TheVoid ? 10 : re);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            if (self.sceneID == new MenuScene.SceneID("Slugcat_Void") &&
                !SlugcatStats.SlugcatUnlocked(Plugin.TheVoid, Custom.rainWorld))
                self.sceneID = new MenuScene.SceneID("Slugcat_Void_Dark");
            orig(self);
        }

        private static void SlugcatPageNewGame_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self,menu, owner, pageIndex, slugcatNumber);
            if (slugcatNumber == Plugin.TheVoid && !(menu as SlugcatSelectMenu).SlugcatUnlocked(slugcatNumber))
                self.infoLabel.text = self.menu.Translate("Clear the game as Hunter to unlock.");
        }

        private static bool SlugcatStats_SlugcatUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugcatStats.Name i, RainWorld rainWorld)
        {
            var re = orig(i,rainWorld);
            if (i == Plugin.TheVoid &&
                !rainWorld.progression.miscProgressionData.beaten_Hunter)
                return Plugin.DevEnabled;
            return re;
        }

        private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self,package);
            MenuScene.SceneID sceneID = null;

            if (self.saveState?.saveStateNumber == Plugin.TheVoid && self.IsSleepScreen)
            {
                if (self.karmaLadder.displayKarma.y == 10)
                    sceneID = new MenuScene.SceneID("Sleep_Void_Karma11");
                else
                    sceneID = new MenuScene.SceneID("Sleep_Void");
                Debug.Log($"[The Void] Karma Sleep Scene, Karma : {self.karmaLadder.displayKarma.y}");
            }
            if (sceneID != null && sceneID.Index != -1)
            {
                self.scene.RemoveSprites();
                self.pages[0].subObjects.RemoveAll(i => i is InteractiveMenuScene);
                self.scene = new InteractiveMenuScene(self, self.pages[0], sceneID);
                self.pages[0].subObjects.Add(self.scene);
                for(int i = self.scene.depthIllustrations.Count-1;i>0;i--)
                    self.scene.depthIllustrations[i].sprite.MoveToBack();
            }
        }

        private static void SleepAndDeathScreen_AddBkgIllustration(On.Menu.SleepAndDeathScreen.orig_AddBkgIllustration orig, SleepAndDeathScreen self)
        {
            if (self.manager.currentMainLoop is RainWorldGame game &&
                game.session.characterStats.name == Plugin.TheVoid)
            {
                return;
            }
            orig(self);
        }

        private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig(self, saveStateNumber, game);
            if (self.characterStats.name == Plugin.TheVoid && self.saveState.deathPersistentSaveData.karma == 10)
            {
                self.characterStats.foodToHibernate = 6;
                self.characterStats.maxFood = 9;
                Debug.Log("Adjust food to hibernate");
            }
        }

        private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.session is StoryGameSession session &&
                session.characterStats.name == Plugin.TheVoid && session.saveState.deathPersistentSaveData.karma == 10)
            {
                return orig(slugcatIndex, eatenobject) * 2;
            }

            return orig(slugcatIndex, eatenobject);
        }




        private static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, RWCustom.IntVector2 k)
        {
            if (!small && k.x == -1)
                return "atlas-void/karma_blank";
            int min = 0;
            if (ModManager.MSC && small)
            {
                min = -1;
            }
            if (k.x < 5)
            {
                return (small ? "smallKarma" : "karma") + Mathf.Clamp(k.x, min, 4);
            }
            return (small ? "smallKarma" : "karma") + Mathf.Clamp(k.x, 5, 10)+ "-" + Mathf.Clamp(k.y, k.x, 10);
        }




        private static void SlugcatPageContinue_ctorIL(ILContext il)
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

                    c.EmitDelegate<Func<int, SlugcatStats.Name,SlugcatSelectMenu.SlugcatPageContinue, int>>((x, name, self) =>
                    {
                        if (name == Plugin.TheVoid && self.saveGameData.karma == 10)
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
                        if (name == Plugin.TheVoid && self.saveGameData.karma == 10)
                            return 6;
                        return y;
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
 
    }

}
