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
        On.Conversation.LoadEventsFromFile_int_Name_bool_int += Conversation_LoadEventsFromFile_int_Name_bool_int;
        CRSHook();
    }

    public static void CRSHook()
    {
        MethodInfo method = typeof(CustomConvo).GetMethod(nameof(CustomConvo.LoadEventsFromFile), [typeof(Conversation), typeof(string), typeof(Oracle.OracleID), typeof(SlugcatStats.Name), typeof(bool), typeof(int)]);
        new ILHook(method, il =>
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

}
