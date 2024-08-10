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
            IL.SaveState.GhostEncounter += SaveState_GhostEncounterIL;

            On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;

            On.Menu.SleepAndDeathScreen.AddBkgIllustration += SleepAndDeathScreen_AddBkgIllustration;
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

            IL.World.SpawnGhost += KarmaReqTinker;
            On.GhostConversation.AddEvents += GhostConversation_AddEvents;
            IL.Ghost.Update += Ghost_UpdateIL;

            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;

            On.Menu.KarmaLadder.ctor += KarmaLadder_ctor;
            On.Menu.KarmaLadder.GoToKarma += KarmaLadder_GoToKarma;

            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;

            // Механики 11 кармы.

            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.Player.EatMeatUpdate += Player_EatMeatUpdate;
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
                if (screen.saveState.redExtraCycles || ForceFailed)
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

        private static void Player_ClassMechanicsSaint(ILContext il)
        {

            try
            {
                ILCursor c = new ILCursor(il);
                //if (physicalObject is Creature)
                //{
                //    if (!(physicalObject as Creature).dead)
                //    {
                //        flag2 = true;
                //    }
                //    (physicalObject as Creature) <if void and karma 11 TO label > .Die();
                //    <TO label2
                //    label
                //    //this is a bubble for the condition "void and karma 11"
                //    POP creature
                //    if victim is thevoid stun for 11 seconds
                //    label2>
                //}
                c.GotoNext(MoveType.After,
                    i => i.MatchLdcI4(1),
                    i => i.MatchStloc(15),
                    i => i.MatchLdloc(18),
                    i => i.MatchIsinst<Creature>());

                var label = c.DefineLabel();
                var label2 = c.DefineLabel();
                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Func<Creature, bool>>((self) =>
                    self is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.TheVoid);
                c.Emit(OpCodes.Brtrue_S, label);
                c.GotoNext(MoveType.After,
                    i => i.MatchCallvirt<Creature>("Die"));
                c.Emit(OpCodes.Br, label2);
                c.MarkLabel(label);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldloc, 18);
                c.EmitDelegate((PhysicalObject PhysicalObject) =>
                {
                    if (PhysicalObject is Player p && p.slugcatStats.name == VoidEnums.SlugcatID.TheVoid) p.Stun(Utils.TicksPerSecond * 5);
                });
                c.MarkLabel(label2);
            }
            catch (Exception e)
            {
                _Plugin.logger.LogError(e);
                throw;
            }
        }

        private static void Ghost_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdfld<StoryGameSession>("saveStateNumber"),
                    i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
                    i => i.MatchCall(out var call) && call.Name.Contains("op_Equality"));
                var label = (ILLabel)c.Next.Operand;
                Debug.Log(label.Target);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, Ghost, bool>>((re, self) =>
                    re || ((self.room.game.session is StoryGameSession session) &&
                           session.saveStateNumber == VoidEnums.SlugcatID.TheVoid));

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static string GetGhostConversationPath(InGameTranslator.LanguageID id, Conversation.ID convId, bool hasMark)
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
                session.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
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
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var split = LocalizationTranslator.ConsolidateLineInstructions(line);
                        if (split.Length == 3)
                            self.events.Add(new Conversation.TextEvent(self, int.Parse(split[0]),
                                split[1], int.Parse(split[2])));
                        else
                            self.events.Add(new Conversation.TextEvent(self, 0, line, 0));
                    }

                    return;
                }

                self.events.Add(new Conversation.TextEvent(self, 0, $"Can't find conv at {GetGhostConversationPath(InGameTranslator.LanguageID.English, self.id,
                    session.saveState.deathPersistentSaveData.theMark)}<LINE> for {self.id}", 0));

            }
            orig(self);
        }

        private static bool GhostWorldPresence_SpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.session is StoryGameSession session &&
                session.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
                return true;
            var re = orig(ghostID, karma, karmaCap, ghostPreviouslyEncountered, playingAsRed);
            return re;
            
        }

        private static void SaveState_GhostEncounterIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdcI4(9));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, SaveState, int>>((re, self) =>
                    self.saveStateNumber == VoidEnums.SlugcatID.TheVoid ? 10 : re);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }



        private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self, package);
            MenuScene.SceneID sceneID = null;

            if (self.saveState?.saveStateNumber == VoidEnums.SlugcatID.TheVoid && self.IsSleepScreen)
            {
                if (self.IsStarveScreen) sceneID = VoidEnums.SceneID.DeathSceneID;
                else if (self.karmaLadder.displayKarma.y == 10) sceneID = VoidEnums.SceneID.StaticEnd;
                else sceneID = VoidEnums.SceneID.SleepSceneID;
                Debug.Log($"[The Void] Karma Sleep Scene, Karma : {self.karmaLadder.displayKarma.y}");
            }
            if (sceneID != null && sceneID.Index != -1)
            {
                self.scene.RemoveSprites();
                self.pages[0].subObjects.RemoveAll(i => i is InteractiveMenuScene);
                self.scene = new InteractiveMenuScene(self, self.pages[0], sceneID);
                self.pages[0].subObjects.Add(self.scene);
                for (int i = self.scene.depthIllustrations.Count - 1; i > 0; i--)
                    self.scene.depthIllustrations[i].sprite.MoveToBack();
            }
        }

        private static void SleepAndDeathScreen_AddBkgIllustration(On.Menu.SleepAndDeathScreen.orig_AddBkgIllustration orig, SleepAndDeathScreen self)
        {
            if (self.manager.currentMainLoop is RainWorldGame game &&
                game.session.characterStats.name == VoidEnums.SlugcatID.TheVoid)
            {
                return;
            }
            orig(self);
        }


        //Механика связанная с 11 кармой.

        private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {

            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.session is StoryGameSession session &&
                session.characterStats.name == VoidEnums.SlugcatID.TheVoid &&
                session.saveState.deathPersistentSaveData.karma == 10)
            {

                string objectId = eatenobject.ToString();

                Debug.Log("Object ID: " + objectId);

                if (objectId is "Fly" or "DangleFruit" or "WaterNut" or
                    "SlimeMold" or "SSOracleSwarmer" or "MoreSlugcats.GooieDuck" or
                    "MoreSlugcats.LillyPuck" or "MoreSlugcats.DandelionPeach" or "MoreSlugcats.GlowWeed" or
                    "MoreSlugcats.Seed")
                {
                    return orig(slugcatIndex, eatenobject);
                }
                else
                {
                    return orig(slugcatIndex, eatenobject) * 2;
                }
            }

            return orig(slugcatIndex, eatenobject);
        }

        //Механика связанная с 11 кармой.
        private static void Player_EatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
        {
            if (self.slugcatStats.name == VoidEnums.SlugcatID.TheVoid)
            {
                if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is Creature creature))
                {
                    return;
                }

                if (self.eatMeat > 20)
                {
                    if (ModManager.MSC)
                    {
                        if (creature.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
                        {
                            creature.bodyChunks[0].mass = 0.5f;
                            creature.bodyChunks[1].mass = 0.3f;
                            creature.bodyChunks[2].mass = 0.05f;
                        }

                        if (SlugcatStats.SlugcatCanMaul(self.SlugCatClass) && creature is Vulture vulture && self.grasps[graspIndex].grabbedChunk.index == 4 && vulture.abstractCreature.state is Vulture.VultureState vultureState && vultureState.mask)
                        {
                            vulture.DropMask(Custom.RNV());
                            self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                            self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                            for (int i = UnityEngine.Random.Range(8, 14); i >= 0; i--)
                            {
                                self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(creature.firstChunk.pos, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
                            }
                        }
                    }

                    self.standing = false;
                    self.Blink(5);
                    if (self.eatMeat % 5 == 0)
                    {
                        Vector2 b = Custom.RNV() * 3f;
                        self.mainBodyChunk.pos += b;
                        self.mainBodyChunk.vel += b;
                    }

                    Vector2 vector = self.grasps[graspIndex].grabbedChunk.pos * self.grasps[graspIndex].grabbedChunk.mass;
                    float num = self.grasps[graspIndex].grabbedChunk.mass;
                    for (int j = 0; j < self.grasps[graspIndex].grabbed.bodyChunkConnections.Length; j++)
                    {
                        if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1 == self.grasps[graspIndex].grabbedChunk)
                        {
                            vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
                            num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
                        }
                        else if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2 == self.grasps[graspIndex].grabbedChunk)
                        {
                            vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
                            num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
                        }
                    }
                    vector /= num;
                    self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.5f;
                    self.bodyChunks[1].vel -= Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.6f;

                    if (self.graphicsModule != null && (self.grasps[graspIndex].grabbed as Creature).State.meatLeft > 0 && self.FoodInStomach < self.MaxFoodInStomach)
                    {
                        if (!Custom.DistLess(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos, self.grasps[graspIndex].grabbedChunk.rad))
                        {
                            (self.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos) * (self.grasps[graspIndex].grabbedChunk.rad - Vector2.Distance(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos));
                        }
                        else if (self.eatMeat % 5 == 3)
                        {
                            (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
                        }

                        if (self.eatMeat > 40 && self.eatMeat % 15 == 3)
                        {
                            self.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * 4f;
                            self.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector, self.mainBodyChunk.pos) * 0.9f / self.grasps[graspIndex].grabbedChunk.mass;
                            for (int k = UnityEngine.Random.Range(0, 3); k >= 0; k--)
                            {
                                self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(vector, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
                            }

                            if (self.SessionRecord != null)
                            {
                                self.SessionRecord.AddEat(self.grasps[graspIndex].grabbed);
                            }

                            (self.grasps[graspIndex].grabbed as Creature).State.meatLeft--;

                            if (self.KarmaCap == 10)
                            {
                                self.AddFood(1);
                            }
                            else
                            {
                                self.AddQuarterFood();
                                self.AddQuarterFood();
                            }

                            self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                            return;
                        }

                        if (self.eatMeat % 15 == 3)
                        {
                            self.room.PlaySound(SoundID.Slugcat_Eat_Meat_A, self.mainBodyChunk);
                        }
                    }
                }
            }

            orig(self, graspIndex);
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
            return (small ? "smallKarma" : "karma") + Mathf.Clamp(k.x, 5, 10) + "-" + Mathf.Clamp(k.y, k.x, 10);
        }

    }

}
