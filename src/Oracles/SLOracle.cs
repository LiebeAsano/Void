using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.Oracles.OracleHooks;

namespace VoidTemplate.Oracles;

internal static class SLOracle
{
    public static void Hook()
    {
        On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMark_Update;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        On.SLOracleBehaviorHasMark.SpecialEvent += SLOracleBehaviorHasMark_SpecialEvent;
    }

    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        var saveState = self.oracle.room.game.GetStorySession.saveState;
        var miscData = saveState.miscWorldSaveData;

        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void && self.State.likesPlayer >= 0)
        {
            if (self.State.playerEncounters < 0)
            {
                self.State.playerEncounters = 0;
            }
            switch (self.State.playerEncountersWithMark)
            {
                case > 0 when saveState.cycleNumber - saveState.GetEncountersWithMark() <= 0:
                    {
                        SoundID randomTalk = SoundID.SL_AI_Talk_1;
                        switch (UnityEngine.Random.Range(0, 4))
                        {
                            case 0:
                                self.dialogBox.Interrupt("You are here again. Do you want to show me something?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_1;
                                break;
                            case 1:
                                self.dialogBox.Interrupt("Your return pleases me. What secrets of this world have you revealed this time?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_2;
                                break;
                            case 2:
                                self.dialogBox.Interrupt("You are back. In your eyes, I see a reflection of our changing world.".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_3;
                                break;
                            case 3:
                                self.dialogBox.Interrupt("Little creature, what brought you to me again?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_4;
                                break;
                            case 4:
                                self.dialogBox.Interrupt("Oh, is that you? Did you come back to learn something new?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_5;
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.AirVoice(randomTalk);
                        }
                        break;
                    }
                case 4 when self.State.neuronsLeft < 6:
					{
                        self.dialogBox.Interrupt("I am sorry, friend, but I cannot remember of any new stories I can tell you.".TranslateString(), 50);
                        SoundID randomTalk = SoundID.SL_AI_Talk_1;
                        switch (UnityEngine.Random.Range(0, 4))
                        {
                            case 0:
                                randomTalk = SoundID.SL_AI_Talk_1;
                                break;
                            case 1:
                                randomTalk = SoundID.SL_AI_Talk_2;
                                break;
                            case 2:
                                randomTalk = SoundID.SL_AI_Talk_3;
                                break;
                            case 3:
                                randomTalk = SoundID.SL_AI_Talk_4;
                                break;
                            case 4:
                                randomTalk = SoundID.SL_AI_Talk_5;
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.AirVoice(randomTalk);
                        }
                        break;
                    }
                case 8 when self.State.neuronsLeft < 7:
                    {
                        self.dialogBox.Interrupt("I am embarrassed to ask you, but could you ask Five Pebbles for another neuron, I think he will not mind.".TranslateString(), 50);
                        SoundID randomTalk = SoundID.SL_AI_Talk_1;
                        switch (UnityEngine.Random.Range(0, 4))
                        {
                            case 0:
                                randomTalk = SoundID.SL_AI_Talk_1;
                                break;
                            case 1:
                                randomTalk = SoundID.SL_AI_Talk_2;
                                break;
                            case 2:
                                randomTalk = SoundID.SL_AI_Talk_3;
                                break;
                            case 3:
                                randomTalk = SoundID.SL_AI_Talk_4;
                                break;
                            case 4:
                                randomTalk = SoundID.SL_AI_Talk_5;
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.AirVoice(randomTalk);
                        }
                        break;
                    }
                default:
                    {
                        saveState.SetEncountersWithMark(saveState.cycleNumber);
                        self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
                        self.State.playerEncountersWithMark++;
                        if (miscData.SSaiConversationsHad == 1)
                            saveState.SetVoidMeetMoon(true);
                        break;
                    }
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
    {
        orig(self, eu);
        SoundID randomTalk = SoundID.SL_AI_Talk_1;
        switch (UnityEngine.Random.Range(0, 4))
        {
            case 0:
                randomTalk = SoundID.SL_AI_Talk_1;
                break;
            case 1:
                randomTalk = SoundID.SL_AI_Talk_2;
                break;
            case 2:
                randomTalk = SoundID.SL_AI_Talk_3;
                break;
            case 3:
                randomTalk = SoundID.SL_AI_Talk_4;
                break;
            case 4:
                randomTalk = SoundID.SL_AI_Talk_5;
                break;
        }
        if (self.currentConversation != null && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
        {
            self.AirVoice(randomTalk);
        }
    }

    private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);

        if (OracleConversation.MoonVoidConversation.Contains(self.id))
        {
            string path = AssetManager.ResolveFilePath($"text/oracle/firstmoon/{self.id.value.ToLower()}.txt");
            if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0 
                && self.myBehavior.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/moon11/{self.id.value.ToLower()}.txt");
            else if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0)
                path = AssetManager.ResolveFilePath($"text/oracle/moon/{self.id.value.ToLower()}.txt");
            else if (self.myBehavior.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/firstmoon11/{self.id.value.ToLower()}.txt");
            ConversationParser.GetConversationEvents(self, path);
        }
    }

    private static void SLOracleBehaviorHasMark_SpecialEvent(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
    {
        orig(self, eventName);
        if (eventName == "MoonVoice")
        {
            SoundID randomTalk = SoundID.SL_AI_Talk_1;
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0:
                    randomTalk = SoundID.SL_AI_Talk_1;
                    break;
                case 1:
                    randomTalk = SoundID.SL_AI_Talk_2;
                    break;
                case 2:
                    randomTalk = SoundID.SL_AI_Talk_3;
                    break;
                case 3:
                    randomTalk = SoundID.SL_AI_Talk_4;
                    break;
                case 4:
                    randomTalk = SoundID.SL_AI_Talk_5;
                    break;
            }
            if (self.currentConversation != null && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
            {
                self.AirVoice(randomTalk);
            }
        }
    }

}
