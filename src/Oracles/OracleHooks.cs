using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using System.Linq;
using MoreSlugcats;
using UnityEngine;
using static VoidTemplate.SaveManager;
using static VoidTemplate.StaticStuff;

namespace VoidTemplate.Oracles;

static class OracleHooks
{
    public static void Hook()
    {
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += AddEventsByID;
        IL.SSOracleBehavior.Update += SSOracleBehavior_Update;
    }
    private static void logerr(object e) => _Plugin.logger.LogError(e);
    private static void loginf(object e) => _Plugin.logger.LogInfo(e);
    #region Moon look up conversation
    /// <summary>
    /// This thing checks the ID that conversation gets when it is created and looks up file in {anymod}/text/RainWorldLastWishMoonConversations/{ID}.txt
    /// Use >> to split linger time and string
    /// Example: "   5>>I am not sure what this means!   "
    /// </summary>
    #region immutable
    private static Conversation.ID[] modSpecificConversations = [Moon_VoidConversation];
    #endregion
    private static (int, string) ParseLine(string line)
    {
        string[] res = line.Split(new string[] { ">>" }, StringSplitOptions.None);
        if (res.Length != 2) logerr($"the line \"{line}\" was invalid for parsing (splitting with '>>' resulted in non-two array)");
        if (!int.TryParse(res[0], out int value)) logerr($"the line \"{line}\" has invalid int number before '>>'");
        return (value, res[1]);
    }
    private static void AddEventsByID(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);
        if (Array.Exists(modSpecificConversations, x => self.id == x) || Array.Exists(OracleConversation.MoonVoidConversation, x => self.id == x)) //if id is from this mod
        {
            string path = AssetManager.ResolveFilePath("text/RainWorldLastWishMoonConversations/" + self.id + ".txt"); //look it up in our specific folder
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                Array.ForEach(lines, line =>
                {
                    var q = ParseLine(line);
                    self.events.Add(new Conversation.TextEvent(self, 0, StaticStuff.TranslateStringComplex(q.Item2), q.Item1));
                });
            }
            else logerr($"the path '{path}' has no existing file. No events were loaded.");
        }
    }
    #endregion
    public static SSOracleBehavior.Action MeetVoid_Init = new("MeetVoid_Init", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new("VoidTalk", true);




    public static void EatPearlsInterrupt(this SSOracleBehavior self)
    {
        if (self.oracle.ID == Oracle.OracleID.SL) return;  //only works for FP
        if (self.conversation != null)
        {
            self.conversation.paused = true;
            self.restartConversationAfterCurrentDialoge = true;
        }
        var savestate = self.oracle.room.game.GetStorySession.saveState;
        var amountOfEatenPearls = savestate.GetPebblesPearlsEaten();
        if (amountOfEatenPearls == 11
        && self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad < 6)
        {

            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
            self.getToWorking = 1f;


        }
        else
        {
            self.dialogBox.Interrupt(self.Translate(
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 6
                    ? OracleConversation.eatInterruptMessages6Step[savestate.GetPebblesPearlsEaten()]
                    : OracleConversation.eatInterruptMessages[savestate.GetPebblesPearlsEaten()]), 10);
            savestate.SetPebblesPearlsEaten(savestate.GetPebblesPearlsEaten() + 1);
        }

    }

    public static class OracleConversation
    {
        public static Conversation.ID[] VoidConversation;
        public static Conversation.ID[] MoonVoidConversation;

        public static int[] cycleLingers = new[] { 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0 };
        public static int[] MooncycleLingers = new[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        static OracleConversation()
        {
            VoidConversation = new Conversation.ID[11];
            for (int i = 0; i < VoidConversation.Length; i++)
                VoidConversation[i] = new($"VoidConversation_{i + 1}", true);
            MoonVoidConversation = new Conversation.ID[11];
            for (int i = 0; i < MoonVoidConversation.Length; i++)
                MoonVoidConversation[i] = new($"MoonVoidConversation_{i + 1}", true);
        }

        public static string[] pickInterruptMessages = new[]
        {
            "Yes, help yourself. They are not edible.",
            "You seem to like pearls.",
            "Are you listening to me?",
            "You need to stop being distracted by pearls.",
            "Are you trying my patience?",
            "...never mind...",
            "Yes... this pearl contains data about my structure.",
            "Strange.. This pearl shouldn't be here.",
            "There is nothing useful in this pearl, you can eat it.",
            "This pearl contains information about failed experiments.",
            "I should back up the pearls more often..."
        };

        public static string[] eatInterruptMessages = new[]
        {
            "I'm not sure you can stomach them.",
            "You really shouldn't eat pearls.",
            ". . .",
            ". . .",
            "Do you really eat them?",
            "You must stop right now.",
            ". . .",
            "I forbid to do it!",
            "I said no!",
            "Wait, don't eat it, it's very impo...",
            "I'm warning you for the last time!"
        };

        public static string[] eatInterruptMessages6Step = new[]
        {
            "1-6",
            "2-6",
            "3-6",
            "4-6",
            "5-6",
            "6-6",
            "7-6",
            "8-6",
            "9-6",
            "10-6",
            "11-6"
        };



    }

    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        if (!self.State.SpeakingTerms)
        {
            self.dialogBox.NewMessage("...", 10);
            return;
        }
        if (self.oracle.room.game.StoryCharacter == StaticStuff.TheVoid &&
            self.State.playerEncountersWithMark <= 0)
        {
            if (self.State.playerEncounters < 0)
            {
                self.State.playerEncounters = 0;
            }
            if(self.State.playerEncountersWithMark < OracleConversation.MoonVoidConversation.Length) self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
            return;
        }
        orig(self);
    }

    private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
    {
        orig(self);

        if (OracleConversation.VoidConversation.Contains(self.id))
        {
            if (!self.owner.playerEnteredWithMark)
                self.events.Add(new Conversation.TextEvent(self, 0, ".  .  . ", 0));
            else
                self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 30));

            var path = AssetManager.ResolveFilePath($"text/oracle/{self.id.value.ToLower()}.txt");
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    var split = LocalizationTranslator.ConsolidateLineInstructions(line);
                    if (split.Length == 3)
                        self.events.Add(new Conversation.TextEvent(self, int.Parse(split[0]),
                            self.Translate(split[1]), int.Parse(split[2])));
                    else
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(line), 0));
                }
            }
            else
            {
                //DEBUG
                self.events.Add(new Conversation.TextEvent(self, 0,
                    $"text/oracle/{self.id.value.ToLower()}.txt can't find!", 0));
            }
        }
    }



    private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
    {
        if (nextAction == MeetVoid_Init)
        {
            var behaviorID = VoidTalk;

            self.inActionCounter = 0;
            self.action = nextAction;
            if (self.currSubBehavior.ID == behaviorID)
            {
                self.currSubBehavior.Activate(self.action, nextAction);
                return;
            }
            SSOracleBehavior.SubBehavior subBehavior = null;
            for (int i = 0; i < self.allSubBehaviors.Count; i++)
            {
                if (self.allSubBehaviors[i].ID == behaviorID)
                {
                    subBehavior = self.allSubBehaviors[i];
                    break;
                }
            }

            if (subBehavior == null)
            {
                subBehavior = new SSOracleVoidBehavior(self,
                    self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad);
                self.allSubBehaviors.Add(subBehavior);
            }
            subBehavior.Activate(self.action, nextAction);
            self.currSubBehavior.Deactivate();
            self.currSubBehavior = subBehavior;
        }
        else
        {
            orig(self, nextAction);
        }
    }

    private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        bool seePeople = false;
        foreach (var player in self.oracle.room.game.Players)
            if (player.realizedCreature is Player)
                seePeople = true;
        if (seePeople && self.oracle.room.game.session.characterStats.name == StaticStuff.TheVoid)
        {
            var saveState = self.oracle.room.game.GetStorySession.saveState;
            var miscData = saveState.miscWorldSaveData;
            var need = miscData.SSaiConversationsHad >= 10
                ? -1
                : OracleConversation.cycleLingers[miscData.SSaiConversationsHad];
            loginf($"HadConv: {miscData.SSaiConversationsHad}, Cycle: {saveState.cycleNumber}, LastCycle: {saveState.GetLastMeetCycles()}, NeedCycle: {need}");

            

            if (miscData.SSaiConversationsHad >= 10)
            {
                //Maybe changed
                self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
            }
            else if (miscData.SSaiConversationsHad >= 5 && miscData.SLOracleState.playerEncountersWithMark <= 0 ||
                     miscData.SSaiConversationsHad == 3 && saveState.deathPersistentSaveData.karmaCap < 4 ||
                     miscData.SSaiConversationsHad == 7 && saveState.deathPersistentSaveData.karmaCap < 7 ||
                saveState.cycleNumber - saveState.GetLastMeetCycles() < OracleConversation.cycleLingers[miscData.SSaiConversationsHad])
            {
                self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
            }
            else if (self.action != MeetVoid_Init)
            {
                saveState.SetLastMeetCycles(saveState.cycleNumber);
                if (self.timeSinceSeenPlayer < 0)
                {
                    self.timeSinceSeenPlayer = 0;
                }
                if (self.currSubBehavior.ID != VoidTalk)
                {
                    miscData.SSaiConversationsHad++;
                    
                    if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                    {
                        self.NewAction(MeetVoid_Init);
                        self.SlugcatEnterRoomReaction();
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

                    }
                    else
                    {
                        self.NewAction(SSOracleBehavior.Action.General_GiveMark);
                        self.afterGiveMarkAction = MeetVoid_Init;
                        self.SlugcatEnterRoomReaction();
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                    }
                }

            }
        }
        else
        {
            orig(self);
        }
    }


    public class SSOracleVoidBehavior : SSOracleBehavior.ConversationBehavior
    {
        public SSOracleVoidBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.VoidConversation[times - 1])
        {
            if (ModManager.MMF && owner.oracle.room.game.IsStorySession
                               && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked &&
                               oracle.room.world.rainCycle.timer > oracle.room.world.rainCycle.cycleLength / 4)
            {
                oracle.room.world.rainCycle.timer = oracle.room.world.rainCycle.cycleLength / 4;
                oracle.room.world.rainCycle.dayNightCounter = 0;
            }


        }

        public override void Update()
        {
            base.Update();
            if (owner.conversation == null || owner.conversation.slatedForDeletion == true ||
                owner.conversation.events == null)
            {
                owner.UnlockShortcuts();
                owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                owner.getToWorking = 1f;
            }
        }

        public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            if (newAction == MeetVoid_Init && owner.conversation == null)
            {
                owner.InitateConversation(OracleConversation.VoidConversation[MeetTimes - 1], this);
                base.NewAction(oldAction, newAction);
            }

        }

        int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;
    }
    private static void SSOracleBehavior_Update(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILCursor c2 = new ILCursor(il);
        c.GotoNext(MoveType.After,
            i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"),
            i => i.MatchCall(out var method) && method.Name.Contains("op_Inequality"),
            i => i.Match(OpCodes.Brfalse_S),
            i => i.MatchLdloc(10));
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<bool, SSOracleBehavior, bool>>((re, self)
            => self.oracle.room.game.StoryCharacter == StaticStuff.TheVoid || re);

        c2.GotoNext(MoveType.After, i => i.MatchLdstr("Yes, help yourself. They are not edible."));
        c2.Emit(OpCodes.Ldarg_0);
        c2.EmitDelegate<Func<string, SSOracleBehavior, string>>((str, self) =>
        {
            if (self.oracle.room.game.session.characterStats.name == StaticStuff.TheVoid &&
                OracleConversation.pickInterruptMessages.Length >
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1)
            {
                return OracleConversation.pickInterruptMessages[
                    self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1];
            }

            return str;
        });
    }

}
