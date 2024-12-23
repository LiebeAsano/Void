using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.Oracles.OracleHooks;
using UnityEngine;
using MoreSlugcats;
using RWCustom;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;

namespace VoidTemplate.Oracles;

internal static class SLOracle
{
    public static void Hook()
    {
        On.Oracle.ctor += Oracle_ctor;
        On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.TalkToDeadPlayer += SLOracleBehaviorHasMark_TalkToDeadPlayer;
        On.SLOracleBehaviorHasMark.InterruptRain += SLOracleBehaviorHasMark_InterruptRain;
        On.SLOracleBehaviorHasMark.InterruptPlayerHoldNeuron += SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron;
        On.SLOracleBehaviorHasMark.InterruptPlayerLeavingMessage += SLOracleBehaviorHasMark_InterruptPlayerLeavingMessage;
        On.SLOracleBehaviorHasMark.InterruptPlayerAnnoyingMessage += SLOracleBehaviorHasMark_InterruptPlayerAnnoyingMessage;
        On.SLOracleBehaviorHasMark.ResumePausedConversation += SLOracleBehaviorHasMark_ResumePausedConversation;
        On.SLOracleBehaviorHasMark.PlayerReleaseNeuron += SLOracleBehaviorHasMark_PlayerReleaseNeuron;
        On.SLOracleBehaviorHasMark.PlayerAnnoyingWhenNotTalking += SLOracleBehaviorHasMark_PlayerAnnoyingWhenNotTalking;
        On.SLOracleBehaviorHasMark.PlayerPutItemOnGround += SLOracleBehaviorHasMark_PlayerPutItemOnGround;
        On.SLOracleBehaviorHasMark.PlayerInterruptByTakingItem += SLOracleBehaviorHasMark_PlayerInterruptByTakingItem;
        On.SLOracleBehaviorHasMark.PlayerHoldingSSNeuronsGreeting += SLOracleBehaviorHasMark_PlayerHoldingSSNeuronsGreeting;
        On.SLOracleBehaviorHasMark.AlreadyDiscussedItem += SLOracleBehaviorHasMark_AlreadyDiscussedItem;
        On.SLOracleBehaviorHasMark.MoonConversation.PearlIntro += MoonConversation_PearlIntro;
        On.SLOracleBehaviorHasMark.MoonConversation.PebblesPearl += MoonConversation_PebblesPearl;
        On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMark_Update;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        On.SLOracleBehaviorHasMark.SpecialEvent += SLOracleBehaviorHasMark_SpecialEvent;

    }

    private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
    {
        orig(self, abstractPhysicalObject, room);
        if (self.ID == Oracle.OracleID.SL)
        {
            if (room.game.session is StoryGameSession && (room.game.session as StoryGameSession).saveState.saveStateNumber == VoidEnums.SlugcatID.Void
                && (room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap == 10)
                self.oracleBehavior = new SLOracleBehaviorHasMark(self);
        }
    }

    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        var saveState = self.oracle.room.game.GetStorySession.saveState;
        var miscData = saveState.miscWorldSaveData;

        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void && self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes)
        {
            if (self.State.playerEncounters < 0)
            {
                self.State.playerEncounters = 0;
            }
            switch (self.State.playerEncountersWithMark)
            {
                case > 0 when saveState.cycleNumber - saveState.GetEncountersWithMark() <= 0:
                    {
                        switch (UnityEngine.Random.Range(0, 5))
                        {
                            case 0:
                                MoonVoice(self);
                                self.dialogBox.Interrupt("You are here again. Do you want to show me something?".TranslateString(), 60);
                                break;
                            case 1:
                                MoonVoice(self);
                                self.dialogBox.Interrupt("Your return pleases me. What secrets of this world have you revealed this time?".TranslateString(), 60);
                                break;
                            case 2:
                                MoonVoice(self);
                                self.dialogBox.Interrupt("You are back. In your eyes, I see a reflection of our changing world.".TranslateString(), 60);
                                break;
                            case 3:
                                MoonVoice(self);
                                self.dialogBox.Interrupt("<CapPlayerName>, what brought you to me again?".TranslateString(), 60);
                                break;
                            case 4:
                                MoonVoice(self);
                                self.dialogBox.Interrupt("Oh, is that you? Did you come back to learn something new?".TranslateString(), 60);
                                break;
                        }
                        self.State.playerEncountersWithMark--;
                        break;
                    }
                case 4 when self.State.neuronsLeft < 6:
					{
                        MoonVoice(self);
                        self.dialogBox.Interrupt("I am sorry, <CapPlayerName>, but I cannot remember of any new stories I can tell you.".TranslateString(), 60);
                        self.State.playerEncountersWithMark--;
                        break;
                    }
                case 8 when self.State.neuronsLeft < 7:
                    {
                        MoonVoice(self);
                        self.dialogBox.Interrupt("I am embarrassed to ask you, but could you ask Five Pebbles for another neuron, I think he will not mind.".TranslateString(), 60);
                        self.State.playerEncountersWithMark--;
                        break;
                    }
                default:
                    {
                        saveState.SetEncountersWithMark(saveState.cycleNumber);
                        self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
                        if (miscData.SSaiConversationsHad == 4)
                            saveState.SetVoidMeetMoon(true);
                        break;
                    }
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_TalkToDeadPlayer(On.SLOracleBehaviorHasMark.orig_TalkToDeadPlayer orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (!self.deadTalk && self.oracle.room.ViewedByAnyCamera(self.oracle.firstChunk.pos, 0f))
            {
                if (self.State.neuronsLeft > 3)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("..."), 60);
                    self.dialogBox.NewMessage(self.Translate("<CapPlayerName>, are you okay?"), 60);
                    self.dialogBox.NewMessage(self.Translate("..."), 120);
                    self.dialogBox.NewMessage(self.Translate("Oh..."), 60);
                }
                else
                {
                    self.dialogBox.Interrupt(self.Translate("..."), 60);
                }
                self.deadTalk = true;
            }
        }
        else
        {
            orig(self);
        }
    }

    private static void SLOracleBehaviorHasMark_InterruptRain(On.SLOracleBehaviorHasMark.orig_InterruptRain orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            switch (self.State.neuronsLeft)
            {
                case 2:
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("...rain..."), 5);
                    self.dialogBox.NewMessage(self.Translate("run"), 10);
                    break;
                case 3:
                    MoonVoice(self);
                    self.dialogBox.Interrupt("...", 5);
                    self.dialogBox.NewMessage(self.Translate("...rain... coming... Go!"), 10);
                    return;
                case 4:
                    MoonVoice(self);
                    self.dialogBox.Interrupt("...", 5);
                    self.dialogBox.NewMessage(self.Translate("Rain... You better go. I will be fine."), 10);
                    return;
                default:
                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                    {
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("The rain is coming. If you stay, you will drown. Now, leave me alone."), 5);
                        if (ModManager.MSC && self.CheckSlugpupsInRoom())
                        {
                            MoonVoice(self);
                            self.dialogBox.NewMessage(self.Translate("Take your offspring with you."), 10);
                            return;
                        }
                        if (ModManager.MMF && self.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                        {
                            MoonVoice(self);
                            self.dialogBox.NewMessage(self.Translate("Take your pet with you."), 10);
                            return;
                        }
                    }
                    else
                    {
                        MoonVoice(self);
                        self.dialogBox.Interrupt("...", 5);
                        self.dialogBox.NewMessage(self.Translate("I think the rain is approaching."), 20);
                        self.dialogBox.NewMessage(self.Translate("You better go, <PlayerName>! I will be fine.<LINE>It's not pleasant, but I have been through it before."), 0);
                        if (ModManager.MSC && self.CheckSlugpupsInRoom())
                        {
                            MoonVoice(self);
                            self.dialogBox.NewMessage(self.Translate("Keep your family safe!"), 10);
                            return;
                        }
                        if (ModManager.MMF && self.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                        {
                            MoonVoice(self);
                            self.dialogBox.NewMessage(self.Translate("Keep your friend safe!"), 10);
                            return;
                        }
                    }
                    break;
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron(On.SLOracleBehaviorHasMark.orig_InterruptPlayerHoldNeuron orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (ModManager.MSC && self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && self.oracle.room.game.IsMoonActive())
            {
                float value = UnityEngine.Random.value;
                MoonVoice(self);
                if (value <= 0.25f)
                {
                    self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("No, please, release it!") : self.Translate("NO! ... no. Let it go, please."), 10);
                }
                else if (value <= 0.5f)
                {
                    self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("Wait, that's not food!") : self.Translate("not... edible, please."), 10);
                }
                else if (value <= 0.75f)
                {
                    self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("What are you doing? Stop!") : self.Translate("stop, don... don't!"), 10);
                }
                else
                {
                    self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("Please, don't touch those!") : self.Translate("LET GO! p please"), 10);
                }
                if (!self.DamagedMode)
                {
                    MoonVoice(self);
                    value = UnityEngine.Random.value;
                    if (value <= 0.33f)
                    {
                        self.dialogBox.NewMessage(self.Translate("Despite some power to my facility being restored, those are still crucial to my survival!"), 10);
                    }
                    else if (value <= 0.67f)
                    {
                        self.dialogBox.NewMessage(self.Translate("Those are the only memories I have left. I will cease functioning without them."), 10);
                    }
                    else
                    {
                        self.dialogBox.NewMessage(self.Translate("Your intention was to help me, was it not? Then please don't play with those!"), 10);
                    }
                }
                self.State.InfluenceLike(-0.1f);
            }
            else if (self.State.totalInterruptions >= 5 || self.State.hasToldPlayerNotToEatNeurons)
            {
                MoonVoice(self);
                self.NoLongerOnSpeakingTerms();
                self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("NO! I will...not speak to you...") : self.Translate("Release that, and leave. I will not speak to you any more."), 10);
            }
            else
            {
                MoonVoice(self);
                self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("NO! ... no. Let it go, please.") : self.Translate("No, please, release it!"), 10);
                self.dialogBox.NewMessage(self.DamagedMode ? self.Translate("...please...") : self.Translate("If you eat it or leave with it, I will die. I beg you."), 10);
                self.State.InfluenceLike(-0.2f);
            }
            self.State.hasToldPlayerNotToEatNeurons = true;
            SLOrcacleState state = self.State;
            int num = state.annoyances;
            state.annoyances = num + 1;
            SLOrcacleState state2 = self.State;
            num = state2.totalInterruptions;
            state2.totalInterruptions = num + 1;
            self.State.increaseLikeOnSave = false;
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_InterruptPlayerLeavingMessage(On.SLOracleBehaviorHasMark.orig_InterruptPlayerLeavingMessage orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.State.totalInterruptions >= 5 && (!ModManager.MMF || self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes))
            {
                self.NoLongerOnSpeakingTerms();
                if (self.State.totalInterruptions == 5)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("...don't... come back.") : self.Translate("Please don't come back."), 10);
                }
            }
            else if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
            {
                switch (self.State.leaves)
                {
                    case 0:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("...leaving now? Don't... return.") : self.Translate("Oh, leaving. Please don't come back."), 10);
                        break;
                    case 1:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("...yes... leave.") : self.Translate("Leaving again."), 10);
                        break;
                    case 2:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("This... time... don't... come back.") : self.Translate("You're leaving yet again. This time, stay away."), 10);
                        break;
                    case 3:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("Again? ... just, go.") : self.Translate("Yes, there you go. This is ridiculous."), 10);
                        break;
                    case 4:
                        MoonVoice(self);
                        self.NoLongerOnSpeakingTerms();
                        self.dialogBox.Interrupt(self.DamagedMode ? "..." : self.Translate("I don't know what to say. Never come back, creature!"), 10);
                        break;
                }
            }
            else
            {
                switch (self.State.leaves)
                {
                    case 0:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate("Oh... You are leaving."), 10);
                        if (!self.DamagedMode)
                        {
                            self.currentConversation.ForceAddMessage(self.Translate("Good bye, I suppose..."), 10);
                        }
                        break;
                    case 1:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("again... leaving...") : self.Translate("There you go again."), 10);
                        break;
                    case 2:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.DamagedMode ? "..." : self.Translate("Yet again leaving."), 10);
                        break;
                    default:
                        if (!self.DamagedMode)
                        {
                            switch (UnityEngine.Random.Range(0, 5))
                            {
                                case 0:
                                    MoonVoice(self);
                                    self.currentConversation.Interrupt(self.Translate("Yes, there you go."), 10);
                                    break;
                                case 1:
                                    MoonVoice(self);
                                    self.currentConversation.Interrupt(self.Translate("Again."), 10);
                                    break;
                                case 2:
                                    MoonVoice(self);
                                    self.currentConversation.Interrupt(self.Translate("*sigh*"), 10);
                                    break;
                                case 3:
                                    MoonVoice(self);
                                    self.currentConversation.Interrupt(self.Translate("..."), 10);
                                    break;
                                case 4:
                                    MoonVoice(self);
                                    self.currentConversation.Interrupt(self.Translate("This again."), 10);
                                    break;
                                default:
                                    MoonVoice(self);
                                    self.currentConversation.Interrupt(self.Translate("<CapPlayerName>... Never mind."), 10);
                                    break;
                            }
                        }
                        break;
                }
            }
            if (!ModManager.MMF)
            {
                self.State.InfluenceLike(-0.05f);
            }
            SLOrcacleState state = self.State;
            int num = state.leaves;
            state.leaves = num + 1;
            SLOrcacleState state2 = self.State;
            num = state2.totalInterruptions;
            state2.totalInterruptions = num + 1;
            self.State.increaseLikeOnSave = false;
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_InterruptPlayerAnnoyingMessage(On.SLOracleBehaviorHasMark.orig_InterruptPlayerAnnoyingMessage orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (!ModManager.MMF && self.State.totalInterruptions >= 5)
            {
                self.NoLongerOnSpeakingTerms();
                if (self.State.totalInterruptions == 5)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("I will...not speak to you...") : self.Translate("I will not speak to you any more."), 10);
                }
            }
            else if (self.State.annoyances == 0)
            {
                MoonVoice(self);
                self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("...please... be still...") : self.Translate("Please. Be still for a moment."), 10);
            }
            else if (self.State.annoyances == 1)
            {
                MoonVoice(self);
                self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("...stop...") : self.Translate("Please stop it!"), 10);
            }
            else if (self.State.neuronsLeft > 3 && !self.DamagedMode)
            {
                switch (UnityEngine.Random.Range(0, 6))
                {
                    case 0:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate("<CapPlayerName>! Stay still and listen."), 10);
                        break;
                    case 1:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate(ModManager.MMF ? "Calm down!" : "I won't talk to you if you continue like this."), 10);
                        break;
                    case 2:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate("Why should I tolerate this?"), 10);
                        break;
                    case 3:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate("STOP!"), 10);
                        break;
                    case 4:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate("This again."), 10);
                        break;
                    default:
                        MoonVoice(self);
                        self.currentConversation.Interrupt(self.Translate("Leave me alone!"), 10);
                        break;
                }
            }
            if (!ModManager.MMF)
            {
                self.State.InfluenceLike(-0.2f);
            }
            SLOrcacleState state = self.State;
            int num = state.annoyances;
            state.annoyances = num + 1;
            SLOrcacleState state2 = self.State;
            num = state2.totalInterruptions;
            state2.totalInterruptions = num + 1;
            self.State.increaseLikeOnSave = false;
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_ResumePausedConversation(On.SLOracleBehaviorHasMark.orig_ResumePausedConversation orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.Annoyance)
            {
                if (self.State.annoyances < 3)
                {
                    MoonVoice(self);
                    self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("Thank... you.") : self.Translate("Thank you."), 5);
                }
            }
            else if (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.Leave)
            {
                if (self.State.leaves == 1)
                {
                    MoonVoice(self);
                    self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("You... are back.") : self.Translate("And you are back."), 10);
                }
                else if (self.State.leaves == 2)
                {
                    MoonVoice(self);
                    self.currentConversation.Interrupt(self.DamagedMode ? self.Translate("And...back.") : self.Translate("Back again."), 10);
                }
                else if (!self.DamagedMode)
                {
                    MoonVoice(self);
                    self.currentConversation.Interrupt(self.Translate("Here again."), 10);
                }
            }
            else if (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.GrabNeuron)
            {
                self.PlayerReleaseNeuron();
            }
            if (self.State.totalInterruptions == 1)
            {
                MoonVoice(self);
                self.currentConversation.ForceAddMessage(self.DamagedMode ? self.Translate("I...said...") : self.Translate("As I was saying..."), 10);
                return;
            }
            if (self.State.totalInterruptions == 2)
            {
                MoonVoice(self);
                self.currentConversation.ForceAddMessage(self.DamagedMode ? self.Translate("Tried to say... to you...") : self.Translate("As I tried to say to you..."), 10);
                return;
            }
            if (self.State.totalInterruptions == 3)
            {
                MoonVoice(self);
                self.currentConversation.ForceAddMessage(self.DamagedMode ? self.Translate("Stay! ... Still...") : self.Translate("Little creature, why don't you stay calm and listen?"), 10);
                self.currentConversation.ForceAddMessage(self.DamagedMode ? self.Translate("And...listen!") : self.Translate("As I tried to say to you..."), 10);
                return;
            }
            if (self.State.totalInterruptions != 4)
            {
                if (self.State.totalInterruptions == 5)
                {
                    if (self.DamagedMode)
                    {
                        MoonVoice(self);
                        self.currentConversation.ForceAddMessage(self.Translate("I am... too tired."), 10);
                        self.currentConversation.ForceAddMessage(self.Translate("Stop doing... this, or I... will not speak... to you again."), 10);
                        return;
                    }
                    MoonVoice(self);
                    self.currentConversation.ForceAddMessage(self.Translate("If you behave like this, why should I talk to you?"), 10);
                    MoonVoice(self);
                    self.currentConversation.ForceAddMessage(self.Translate("You come here, but you can't be respectful enough to listen to me.<LINE>Will you listen this time?"), 0);
                    MoonVoice(self);
                    self.currentConversation.ForceAddMessage(self.Translate("Look at me. The only thing I have to offer is my words.<LINE>If you come here, I must assume you want me to speak? So then would you PLEASE listen?<LINE>If not, you are welcome to leave me alone."), 0);
                    MoonVoice(self);
                    self.currentConversation.ForceAddMessage(self.Translate("Now if you'll let me, I will try to say this again."), 0);
                }
                return;
            }
            if (self.DamagedMode)
            {
                MoonVoice(self);
                self.currentConversation.ForceAddMessage(self.Translate("And...now you expect me to... talk again?"), 10);
                return;
            }
            MoonVoice(self);
            self.currentConversation.ForceAddMessage(self.Translate("And now you expect me to continue speaking?"), 10);
            if (self.State.neuronsLeft < 5)
            {
                MoonVoice(self);
                self.currentConversation.ForceAddMessage(self.Translate("First you hurt me, then you come back to annoy me.<LINE>I wish I knew what was going on in that little head of yours."), 0);
            }
            MoonVoice(self);
            self.currentConversation.ForceAddMessage(self.Translate("Let us try again - not that it has worked well before. I was saying..."), 10);
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_PlayerReleaseNeuron(On.SLOracleBehaviorHasMark.orig_PlayerReleaseNeuron orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (!ModManager.MSC || !(self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet) || !self.oracle.room.game.IsMoonActive())
            {
                if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("...don't... do that.") : self.Translate("Never do that again. Or just kill me quickly. Whichever way."), 5);
                }
                else if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("Thank... you.") : self.Translate("Thank you. I must ask you... Don't do that again."), 5);
                }
                else
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("Please... don't do... that.") : self.Translate("I must ask you... Don't do that again."), 5);
                }
                MoonVoice(self);
                self.dialogBox.NewMessage(self.DamagedMode ? self.Translate("I... won't speak to you... if you do that.") : self.Translate("<CapPlayerName>, if you do that, I will not speak to you any more."), 10);
                return;
            }
            float value = UnityEngine.Random.value;
            if (value <= 0.33f)
            {
                MoonVoice(self);
                self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("Thank you. I must ask you... Don't do that again.") : self.Translate("...don't... do that."), 10);
                return;
            }
            if (value <= 0.67f)
            {
                MoonVoice(self);
                self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("Please, don't scare me like that. I don't appreciate it.") : self.Translate("Leave me... alone."), 10);
                return;
            }
            MoonVoice(self);
            self.dialogBox.Interrupt((!self.DamagedMode) ? self.Translate("Those aren't toys, <PlayerName>. I cannot trust anyone with them.") : self.Translate("...don't... trust you."), 10);
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_PlayerAnnoyingWhenNotTalking(On.SLOracleBehaviorHasMark.orig_PlayerAnnoyingWhenNotTalking orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (!ModManager.MMF && self.State.annoyances >= 5)
            {
                self.NoLongerOnSpeakingTerms();
                if (self.State.annoyances == 5)
                {
                    if (self.State.neuronsLeft > 3)
                    {
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("I will not speak to you any more."), 10);
                    }
                    else if (self.State.neuronsLeft > 1)
                    {
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("I will...not speak to you..."), 10);
                    }
                }
            }
            else if (self.State.annoyances == 0)
            {
                MoonVoice(self);
                self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("...stop...") : self.Translate("<CapPlayerName>... Please settle down."), 10);
            }
            else if (self.State.annoyances == 1)
            {
                MoonVoice(self);
                self.dialogBox.Interrupt(self.DamagedMode ? self.Translate("no...") : self.Translate("Please stop it!"), 10);
            }
            else if (self.State.neuronsLeft > 3 && !self.DamagedMode)
            {
                switch (UnityEngine.Random.Range(0, 6))
                {
                    case 0:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Why are you doing this?"), 10);
                        break;
                    case 1:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Please!"), 10);
                        break;
                    case 2:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Why should I tolerate this?"), 10);
                        break;
                    case 3:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("STOP!"), 10);
                        break;
                    case 4:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("This again."), 10);
                        break;
                    default:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Leave me alone!"), 10);
                        break;
                }
            }
            if (!ModManager.MMF)
            {
                self.State.InfluenceLike(-0.2f);
            }
            SLOrcacleState state = self.State;
            int annoyances = state.annoyances;
            state.annoyances = annoyances + 1;
            self.State.increaseLikeOnSave = false;
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_PlayerPutItemOnGround(On.SLOracleBehaviorHasMark.orig_PlayerPutItemOnGround orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (ModManager.MSC && self.RejectDiscussItem())
            {
                return;
            }
            switch (self.State.totalItemsBrought)
            {
                case 0:
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("What is that?"), 10);
                    return;
                case 1:
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("Another gift?"), 10);
                    if (self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes)
                    {
                        self.dialogBox.NewMessage(self.Translate("I will take a look."), 10);
                        return;
                    }
                    break;
                case 2:
                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                    {
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Oh, what is that, <PlayerName>?"), 10);
                        return;
                    }
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("What is that, <PlayerName>?"), 10);
                    return;
                case 3:
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("Yet another gift?"), 10);
                    return;
                default:
                    switch (UnityEngine.Random.Range(0, 11))
                    {
                        case 0:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Something new you want me to look at, <PlayerName>?"), 10);
                            return;
                        case 1:
                            MoonVoice(self);
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                self.dialogBox.Interrupt(self.Translate("Another gift for me?"), 10);
                            }
                            self.dialogBox.NewMessage(self.Translate("I will take a look."), 10);
                            return;
                        case 2:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Oh, what is that, <PlayerName>?"), 10);
                            return;
                        case 3:
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                MoonVoice(self);
                                self.dialogBox.Interrupt(self.Translate("Yet another gift? You're quite curious, <PlayerName>!"), 10);
                                return;
                            }
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Yet another thing?"), 10);
                            self.dialogBox.NewMessage(self.Translate("Your curiosity seems boundless, <PlayerName>."), 10);
                            return;
                        case 4:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Another thing you want me to look at?"), 10);
                            return;
                        case 5:
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                MoonVoice(self);
                                self.dialogBox.Interrupt(self.Translate("Oh... I will look at it."), 10);
                                return;
                            }
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Something new you want me to look at,<LINE>I suppose, <PlayerName>?"), 10);
                            return;
                        case 6:
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                MoonVoice(self);
                                self.dialogBox.Interrupt(self.Translate("Oh... Of course I will take a look"), 10);
                                return;
                            }
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Oh... I will take a look"), 10);
                            return;
                        case 7:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("You want me to take a look at that?"), 10);
                            return;
                        case 8:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Oh... Should I look at that?"), 10);
                            return;
                        case 9:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("A gift for me, <PlayerName>?"), 10);
                            return;
                        default:
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("A new gift for me, <PlayerName>?"), 10);
                            break;
                    }
                    break;
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_PlayerInterruptByTakingItem(On.SLOracleBehaviorHasMark.orig_PlayerInterruptByTakingItem orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.throwAwayObjects)
            {
                if (UnityEngine.Random.value < 0.25f)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("Stop it! Go away!"), 30);
                }
                else
                {
                    self.dialogBox.Interrupt(self.Translate("..."), 10);
                }
                SLOrcacleState state = self.State;
                int totalInterruptions = state.totalInterruptions;
                state.totalInterruptions = totalInterruptions + 1;
                return;
            }
            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("Yes, take it and leave me alone."), 10);
                }
                else
                {
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("And now you're taking it, apparently."), 10);
                }
            }
            else
            {
                switch (UnityEngine.Random.Range(0, 4))
                {
                    case 0:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Oh... Never mind, I suppose."), 10);
                        break;
                    case 1:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Oh, you want it back?"), 10);
                        }
                        else
                        {
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("And now you're taking it back."), 10);
                        }
                        break;
                    case 2:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Want it back, <PlayerName>?"), 10);
                        break;
                    default:
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Oh..."), 10);
                        self.dialogBox.NewMessage(self.Translate("Yes, you're welcome to have it back."), 10);
                        break;
                }
            }
            if (self.currentConversation != null)
            {
                self.currentConversation.Destroy();
                self.currentConversation = null;
                SLOrcacleState state2 = self.State;
                int totalInterruptions = state2.totalInterruptions;
                state2.totalInterruptions = totalInterruptions + 1;
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_PlayerHoldingSSNeuronsGreeting(On.SLOracleBehaviorHasMark.orig_PlayerHoldingSSNeuronsGreeting orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            switch (self.State.neuronsLeft)
            {
                case 0:
                    break;
                case 1:
                    MoonVoice(self);
                    self.dialogBox.Interrupt("...", 40);
                    return;
                case 2:
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("...oh... to... save me?"), 20);
                    return;
                case 3:
                    MoonVoice(self);
                    self.dialogBox.Interrupt(self.Translate("You... brought that... for me?"), 20);
                    return;
                default:
                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                    {
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("You are bringing a neuron. Is it to taunt me?"), 30);
                            return;
                        }
                        self.dialogBox.Interrupt(self.Translate("A neuron."), 30);
                        return;
                    }
                    else
                    {
                        bool flag = self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes;
                        int num = UnityEngine.Random.Range(0, 2);
                        if (num == 0)
                        {
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("That... That is for me?"), 10);
                            return;
                        }
                        if (num == 1)
                        {
                            MoonVoice(self);
                            self.dialogBox.Interrupt(self.Translate("Hello" + (flag ? "!" : ".")), 10);
                            self.dialogBox.NewMessage(self.Translate("That... Oh, thank you."), 10);
                            return;
                        }
                        MoonVoice(self);
                        self.dialogBox.Interrupt(self.Translate("Ah... <PlayerName>, a neuron from Pebbles?"), 30);
                    }
                    break;
            }
        }
        else
            orig(self);
    }
    private static void SLOracleBehaviorHasMark_AlreadyDiscussedItem(On.SLOracleBehaviorHasMark.orig_AlreadyDiscussedItem orig, SLOracleBehaviorHasMark self, bool pearl)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            string text;
            if (pearl)
            {
                int num = UnityEngine.Random.Range(0, 3);
                if (num != 0)
                {
                    if (num != 1)
                    {
                        text = self.Translate("This one again, <PlayerName>?");
                    }
                    else
                    {
                        text = self.Translate("This one I've already read to you, <PlayerName>.");
                    }
                }
                else
                {
                    text = self.Translate("Oh, I have already read this one to you, <PlayerName>.");
                }
            }
            else
            {
                int num = UnityEngine.Random.Range(0, 3);
                if (num != 0)
                {
                    if (num != 1)
                    {
                        text = self.Translate("<CapPlayerName>, this one again?");
                    }
                    else
                    {
                        text = self.Translate("I've told you about this one, <PlayerName>.");
                    }
                }
                else
                {
                    text = self.Translate("I think we have already talked about this one, <PlayerName>.");
                }
            }
            MoonVoice(self);
            if (self.currentConversation != null)
            {
                self.currentConversation.Interrupt(text, 10);
                return;
            }
            self.dialogBox.Interrupt(text, 10);
        }
        else
            orig(self, pearl);
    }

    private static void MoonConversation_PearlIntro(On.SLOracleBehaviorHasMark.MoonConversation.orig_PearlIntro orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self.myBehavior.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.myBehavior is SLOracleBehaviorHasMark self2)
            {
                if (self.myBehavior.isRepeatedDiscussion)
                {
                    self.events.Add(new Conversation.TextEvent(self, 0, self.myBehavior.AlreadyDiscussedItemString(true), 10));
                    return;
                }
                if (self.myBehavior.oracle.ID != Oracle.OracleID.SS)
                {
                    switch (self.State.totalPearlsBrought + self.State.miscPearlCounter)
                    {
                        case 0:
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah, you would like me to read this?"), 10));
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It's a bit dusty, but I will do my best. Hold on..."), 10));
                            return;
                        case 1:
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Another pearl! You want me to read this one too? Just a moment..."), 10));
                            return;
                        case 2:
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("And yet another one! I will read it to you."), 10));
                            return;
                        case 3:
                            if (ModManager.MSC && self.myBehavior.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Another? Let us see... to be honest, I'm as curious to see it as you are."), 10));
                                return;
                            }
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Another? You're no better than the scavengers!"), 10));
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let us see... to be honest, I'm as curious to see it as you are."), 10));
                                return;
                            }
                            break;
                        default:
                            switch (UnityEngine.Random.Range(0, 5))
                            {
                                case 0:
                                    break;
                                case 1:
                                    if (ModManager.MSC && self.myBehavior.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                                    {
                                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh? What have you found this time? Let's see what it says..."), 10));
                                        return;
                                    }
                                    MoonVoice(self2);
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("The scavengers must be jealous of you, finding all these"), 10));
                                    return;
                                case 2:
                                    MoonVoice(self2);
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Here we go again, little archeologist. Let's read your pearl."), 10));
                                    return;
                                case 3:
                                    MoonVoice(self2);
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("... You're getting quite good at this you know. A little archeologist beast.<LINE>Now, let's see what it says."), 10));
                                    return;
                                default:
                                    MoonVoice(self2);
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("And yet another one! I will read it to you."), 10));
                                    return;
                            }
                            break;
                    }
                }
                else
                {
                    switch (self.State.totalPearlsBrought + self.State.miscPearlCounter)
                    {
                        case 0:
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah, you have found me something to read?"), 10));
                            return;
                        case 1:
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Have you found something else for me to read?"), 10));
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let us take a look."), 10));
                            return;
                        case 2:
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I am surprised you have found so many of these."), 10));
                            return;
                        case 3:
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Where do you find all of these?"), 10));
                            MoonVoice(self2);
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I wonder, just how much time has passed since some of these were written."), 10));
                            return;
                        default:
                            switch (UnityEngine.Random.Range(0, 5))
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let us see what you have found."), 10));
                                    return;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah. Have you found something new?"), 10));
                                    return;
                                case 2:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("What is this?"), 10));
                                    return;
                                case 3:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Is that something new? Allow me to see."), 10));
                                    return;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let us see if there is anything important written on this."), 10));
                                    break;
                            }
                            break;
                    }
                }
            }
        }
        else
            orig (self);

    }

    private static void MoonConversation_PebblesPearl(On.SLOracleBehaviorHasMark.MoonConversation.orig_PebblesPearl orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self.myBehavior.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.myBehavior is SLOracleBehaviorHasMark self2)
            {
                switch (UnityEngine.Random.Range(0, 5))
                {
                    case 0:
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You would like me to read this?"), 10));
                        MoonVoice(self2);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It's still warm... this was in use recently."), 10));
                        break;
                    case 1:
                        MoonVoice(self2);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("A pearl... This one is crystal clear - it was used just recently."), 10));
                        break;
                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Would you like me to read this pearl?"), 10));
                        MoonVoice(self2);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Strange... it seems to have been used not too long ago."), 10));
                        break;
                    case 3:
                        MoonVoice(self2);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl has been written to just now!"), 10));
                        break;
                    default:
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let's see... A pearl..."), 10));
                        MoonVoice(self2);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("And this one is fresh! It was not long ago this data was written to it!"), 10));
                        break;
                }
                self.LoadEventsFromFile((ModManager.MSC && self.myBehavior.oracle.ID == MoreSlugcatsEnums.OracleID.DM) ? 168 : 40, true, (self.myBehavior is SLOracleBehaviorHasMark && (self.myBehavior as SLOracleBehaviorHasMark).holdingObject != null) ? (self.myBehavior as SLOracleBehaviorHasMark).holdingObject.abstractPhysicalObject.ID.RandomSeed : UnityEngine.Random.Range(0, 100000));
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
    {
        orig(self, eu);
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            /*if (self.player == null && self.hasNoticedPlayer)
            {
                self.TalkToDeadPlayer();
            }*/
            int randomTime = UnityEngine.Random.Range(200, 401);
            if (self.holdingObject != null && self.describeItemCounter % randomTime == 0 && !VoidEnums.ConversationID.PearlConversations.Contains(self.currentConversation.id))
            {
                MoonVoice(self);
            }
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
        switch (eventName)
        {
            case "MoonVoice":
                {
                    MoonVoice(self);
                    break;
                }
            case "HoverPearl":
                {
                    if (self.holdingObject is DataPearl pearl)
                    {
                        pearl.slatedForDeletetion = true;
                        var roomref = self.oracle.room;
                        //oh my god DataPearl ctor takes in world but does nothing with it
                        var hoveringPearl = new HoveringPearl(pearl.abstractPhysicalObject, roomref.world);
                        self.holdingObject = hoveringPearl;
                        pearl.abstractPhysicalObject.realizedObject = hoveringPearl;
                        hoveringPearl.bodyChunks[0].pos = pearl.bodyChunks[0].pos;
                        hoveringPearl.hoverPos = self.oracle.firstChunk.pos + new Vector2(-40f, 5f);
                        hoveringPearl.OnPearlTaken += () =>
                        {
                            self.dialogBox.Interrupt("aw...", 200);
                        };
                        hoveringPearl.PlaceInRoom(roomref);
                    }
                    else logerr("attempting event HoverPearl while moon is not holding pearl");
                    break;
                }
            /*case "AsyncHover":
                {
                    if (self.holdingObject is DataPearl pearl)
                    {
                        var roomref = self.oracle.room;
                        var hoveringPearl = new HoveringPearl(pearl.abstractPhysicalObject, roomref.world);
                        hoveringPearl.AnyncHover(16000);
                    }
                    break;
                }*/
        }
    }
    private static void MoonVoice(SLOracleBehaviorHasMark self)
    {
        SoundID randomTalk = SoundID.SL_AI_Talk_1;
        switch (UnityEngine.Random.Range(0, 5))
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
    }
}
