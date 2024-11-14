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
    }

    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        var saveState = self.oracle.room.game.GetStorySession.saveState;
        var miscData = saveState.miscWorldSaveData;

        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.State.playerEncounters < 0)
            {
                self.State.playerEncounters = 0;
            }
            switch (self.State.playerEncountersWithMark)
            {
                case > 0 when saveState.cycleNumber - saveState.GetLastMeetCycles() > 0:
                /*case 5 when  :
					{ 
                        self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
                        self.State.playerEncountersWithMark++;
                        break;
                    }*/ 
                default:
                    {
                        saveState.SetLastMeetCycles(saveState.cycleNumber);
                        self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
                        self.State.playerEncountersWithMark++;
                        self.oracle.room.PlaySound(SoundID.SL_AI_Pain_1, self.oracle.firstChunk);
                        if (miscData.SSaiConversationsHad == 1)
                            saveState.SetVoidMeetMoon(true);
                        break;
                    }
            }
            if (self.State.playerEncountersWithMark < OracleConversation.MoonVoidConversation.Length)
                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
            return;
        }
        orig(self);
    }

    private static void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
    {
        orig(self, eu);
        SoundID randomTalk = SoundID.SL_AI_Talk_3;
        switch (UnityEngine.Random.Range(0, 2))
        {
            case 0:
                randomTalk = SoundID.SL_AI_Talk_3;
                break;
            case 1:
                randomTalk = SoundID.SL_AI_Talk_4;
                break;
            case 2:
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
            string path = AssetManager.ResolveFilePath($"text/oracle/moon/{self.id.value.ToLower()}.txt");

            if (self.myBehavior.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/moon11/{self.id.value.ToLower()}.txt");
            ConversationParser.GetConversationEvents(self, path);
        }
    }
}
