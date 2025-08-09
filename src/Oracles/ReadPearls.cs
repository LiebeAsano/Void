using System;
using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using System.Linq;
using System.Text;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.IO;
using System.Text.RegularExpressions;
using CustomRegions.Collectables;
using static VoidTemplate.Useful.Utils;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace VoidTemplate.Oracles;

public static class ReadPearls
{
    public static void Hook()
    {
        On.SSOracleBehavior.Update += SSOralceBehavior_Update;
        On.Conversation.LoadEventsFromFile_int_Name_bool_int += Conversation_LoadEventsFromFile_int_Name_bool_int;
        CRSHook();
    }

    public static void CRSHook()
    {
        MethodInfo method = typeof(CustomConvo).GetMethod(nameof(CustomConvo.LoadEventsFromFile), [typeof(Conversation), typeof(string), typeof(Oracle.OracleID), typeof(SlugcatStats.Name), typeof(bool), typeof(int)]);
        new ILHook(method, (ILContext il) =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchStarg(3),
                x => x.MatchNop()))
            {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_3);
                c.EmitDelegate((Conversation self, string fileName, SlugcatStats.Name saveFile) =>
                {
                    if (saveFile == VoidEnums.SlugcatID.Void && self is SLOracleBehaviorHasMark.MoonConversation && self.interfaceOwner is SSOracleBehavior)
                    {
                        InGameTranslator.LanguageID lang = Custom.rainWorld.inGameTranslator.currentLanguage;
                        string fpVoidPath = AssetManager.ResolveFilePath($"{Custom.rainWorld.inGameTranslator.SpecificTextFolderDirectory(lang)}{Path.DirectorySeparatorChar}{fileName}-FP.txt");
                        if (File.Exists(fpVoidPath))
                        {
                            return fileName + "-FP";
                        }
                    }
                    return fileName;
                });
                c.Emit(OpCodes.Starg, 1);
            }
            else
            {
                logerr($"{nameof(Oracles)}.{nameof(ReadPearls)}.{nameof(CRSHook)}: match failed");
            }
        });
    }

    private static void Conversation_LoadEventsFromFile_int_Name_bool_int(On.Conversation.orig_LoadEventsFromFile_int_Name_bool_int orig, Conversation self, int fileName, SlugcatStats.Name saveFile, bool oneRandomLine, int randomSeed)
    {
        if (saveFile == VoidEnums.SlugcatID.Void && self is SLOracleBehaviorHasMark.MoonConversation && self.interfaceOwner is SSOracleBehavior)
        {
            InGameTranslator.LanguageID languageID = Custom.rainWorld.inGameTranslator.currentLanguage;
            string text;
            for (; ; )
            {
                string path = $"{Custom.rainWorld.inGameTranslator.SpecificTextFolderDirectory(languageID)}{Path.DirectorySeparatorChar}{fileName}";
                if (File.Exists(text = AssetManager.ResolveFilePath(path + "-void-fp.txt")) ||
                    File.Exists(text = AssetManager.ResolveFilePath(path + "-void.txt")) ||
                    File.Exists(text = AssetManager.ResolveFilePath(path + ".txt")))
                {
                    goto IL_11E;
                }

                if (languageID == InGameTranslator.LanguageID.English)
                {
                    break;
                }
                languageID = InGameTranslator.LanguageID.English;
            }
            return;
        IL_11E:
            string text3 = File.ReadAllText(text, Encoding.UTF8);
            if (text3[0] != '0')
            {
                text3 = Custom.xorEncrypt(text3, 54 + fileName + (int)Custom.rainWorld.inGameTranslator.currentLanguage * 7);
            }
            string[] array = Regex.Split(text3, "\r\n");
            try
            {
                if (Regex.Split(array[0], "-")[1] == fileName.ToString())
                {
                    if (oneRandomLine)
                    {
                        List<Conversation.TextEvent> list = [];
                        for (int i = 1; i < array.Length; i++)
                        {
                            string[] array2 = LocalizationTranslator.ConsolidateLineInstructions(array[i]);
                            if (array2.Length == 3)
                            {
                                list.Add(new Conversation.TextEvent(self, int.Parse(array2[0]), array2[2], int.Parse(array2[1])));
                            }
                            else if (array2.Length == 1 && array2[0].Length > 0)
                            {
                                list.Add(new Conversation.TextEvent(self, 0, array2[0], 0));
                            }
                        }
                        if (list.Count > 0)
                        {
                            UnityEngine.Random.State state = UnityEngine.Random.state;
                            UnityEngine.Random.InitState(randomSeed);
                            Conversation.TextEvent item = list[UnityEngine.Random.Range(0, list.Count)];
                            UnityEngine.Random.state = state;
                            self.events.Add(item);
                        }
                    }
                    else
                    {
                        for (int j = 1; j < array.Length; j++)
                        {
                            string[] array3 = LocalizationTranslator.ConsolidateLineInstructions(array[j]);
                            if (array3.Length == 3)
                            {
                                if (ModManager.MSC && !int.TryParse(array3[1], out _) && int.TryParse(array3[2], out _))
                                {
                                    self.events.Add(new Conversation.TextEvent(self, int.Parse(array3[0]), array3[1], int.Parse(array3[2])));
                                }
                                else
                                {
                                    self.events.Add(new Conversation.TextEvent(self, int.Parse(array3[0]), array3[2], int.Parse(array3[1])));
                                }
                            }
                            else if (array3.Length == 2)
                            {
                                if (array3[0] == "SPECEVENT")
                                {
                                    self.events.Add(new Conversation.SpecialEvent(self, 0, array3[1]));
                                }
                                else if (array3[0] == "PEBBLESWAIT")
                                {
                                    self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, null, int.Parse(array3[1])));
                                }
                            }
                            else if (array3.Length == 1 && array3[0].Length > 0)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, array3[0], 0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logerr($"{nameof(Oracles)}.{nameof(ReadPearls)}.{nameof(Conversation_LoadEventsFromFile_int_Name_bool_int)}: TEXT ERROR {ex.Message}");
                self.events.Add(new Conversation.TextEvent(self, 0, "LAST WISH TEXT ERROR", 100));
            }
        }
        else
        {
            orig(self, fileName, saveFile, oneRandomLine, randomSeed);
        }
    }

    private static void SSOralceBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        orig(self, eu);
        if (ModManager.MSC)
        {
            if ((self.oracle.ID == MoreSlugcatsEnums.OracleID.DM || (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.GetStorySession.saveStateNumber == VoidEnums.SlugcatID.Void)) && self.player != null && self.player.room == self.oracle.room)
            {
                List<PhysicalObject>[] physicalObjects = self.oracle.room.physicalObjects;
                for (int num6 = 0; num6 < physicalObjects.Length; num6++)
                {
                    for (int num7 = 0; num7 < physicalObjects[num6].Count; num7++)
                    {
                        PhysicalObject physicalObject = physicalObjects[num6][num7];
                        if (physicalObject is Weapon && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                        {
                            Weapon weapon = physicalObject as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(weapon.firstChunk.pos, self.oracle.firstChunk.pos) < 100f)
                            {
                                weapon.ChangeMode(Weapon.Mode.Free);
                                weapon.SetRandomSpin();
                                weapon.firstChunk.vel *= -0.2f;
                                for (int num8 = 0; num8 < 5; num8++)
                                {
                                    self.oracle.room.AddObject(new Spark(weapon.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
                                }
                                self.oracle.room.AddObject(new Explosion.ExplosionLight(weapon.firstChunk.pos, 150f, 1f, 8, Color.white));
                                self.oracle.room.AddObject(new ShockWave(weapon.firstChunk.pos, 60f, 0.1f, 8, false));
                                self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, weapon.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value * 0.5f);
                            }
                        }
                        bool flag3 = false;
                        bool flag4 = (self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty || self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty || self.action == SSOracleBehavior.Action.General_Idle) && self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior && (self.currSubBehavior as SSOracleBehavior.SSSleepoverBehavior).panicObject == null;
                        if (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.GetStorySession.saveStateNumber == VoidEnums.SlugcatID.Void && self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior)
                        {
                            flag4 = true;
                            flag3 = true;
                        }
                        if (self.inspectPearl == null
                            && (self.conversation == null || flag3)
                            && physicalObject is DataPearl
                            && (physicalObject as DataPearl).grabbedBy.Count == 0
                            && (physicalObject as DataPearl).AbstractPearl != OracleHooks.VoidPearl(self.oracle.room)
                            && (physicalObject as DataPearl).AbstractPearl != OracleHooks.RotPearl(self.oracle.room)
                            && ((physicalObject as DataPearl).AbstractPearl.dataPearlType != DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl
                            || (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM
                            && ((physicalObject as DataPearl).AbstractPearl as PebblesPearl.AbstractPebblesPearl).color >= 0))
                            && !self.readDataPearlOrbits.Contains((physicalObject as DataPearl).AbstractPearl)
                            && flag4 && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark
                            && !self.talkedAboutThisSession.Contains(physicalObject.abstractPhysicalObject.ID))
                        {
                            self.inspectPearl = (physicalObject as DataPearl);
                            if (!(self.inspectPearl is SpearMasterPearl) || !(self.inspectPearl.AbstractPearl as SpearMasterPearl.AbstractSpearMasterPearl).broadcastTagged)
                            {
                                Custom.Log(
                                [
                                    string.Format("---------- INSPECT PEARL TRIGGERED: {0}", self.inspectPearl.AbstractPearl.dataPearlType)
                                ]);
                                if (self.inspectPearl is SpearMasterPearl)
                                {
                                    self.LockShortcuts();
                                    if (self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.firstChunk.pos.y > 600f)
                                    {
                                        self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.Stun(40);
                                        self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.firstChunk.vel = new Vector2(0f, -4f);
                                    }
                                    self.getToWorking = 0.5f;
                                    self.SetNewDestination(new Vector2(600f, 450f));
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                self.inspectPearl = null;
                            }
                        }
                    }
                }
            }
            if (self.oracle.room.world.name == "HR")
            {
                int num9 = 0;
                if (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                {
                    num9 = 2;
                }
                float num10 = Custom.Dist(self.oracle.arm.cornerPositions[0], self.oracle.arm.cornerPositions[2]) * 0.4f;
                if (Custom.Dist(self.baseIdeal, self.oracle.arm.cornerPositions[num9]) >= num10)
                {
                    self.baseIdeal = self.oracle.arm.cornerPositions[num9] + (self.baseIdeal - self.oracle.arm.cornerPositions[num9]).normalized * num10;
                }
            }
            if (self.currSubBehavior.LowGravity >= 0f)
            {
                self.oracle.room.gravity = self.currSubBehavior.LowGravity;
                return;
            }
        }
    }
}
