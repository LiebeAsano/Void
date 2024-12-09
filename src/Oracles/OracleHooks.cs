using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.IO;
using System.Linq;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.VoidEnums.ConversationID;
using static VoidTemplate.SaveManager;
using UnityEngine;
using System.Collections.Generic;
using RWCustom;
using static VoidTemplate.Oracles.OracleHooks;
using System.Data.SqlTypes;

namespace VoidTemplate.Oracles;

static class OracleHooks
{
	public static void Hook()
	{
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
		On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
        On.SSOracleBehavior.SpecialEvent += SSOracleBehavior_SpecialEvent;
        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        On.SSOracleBehavior.ThrowOutBehavior.Update += ThrowOutBehavior_Update;
        IL.SSOracleBehavior.Update += ILSSOracleBehavior_Update;
	}

    #region immutable
    public static SSOracleBehavior.Action MeetVoid_Init = new("MeetVoid_Init", true);
    public static SSOracleBehavior.Action MeetVoid_Curious = new("MeetVoid_Curious", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new("VoidTalk", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidScan = new("VoidScan", true);
    public static List<ProjectedImage> Void_projectImages = new List<ProjectedImage>();
    #endregion

    public static void EatPearlsInterrupt(this SSOracleBehavior self)
	{
		if (self.oracle.ID == Oracle.OracleID.SL) return;  //only works for FP
		if (self.conversation != null && self.action != SSOracleBehavior.Action.ThrowOut_ThrowOut)
		{
			self.conversation.paused = true;
			self.restartConversationAfterCurrentDialoge = true;
		}
		var savestate = self.oracle.room.game.GetStorySession.saveState;
		var amountOfEatenPearls = savestate.GetPebblesPearlsEaten();
		if (amountOfEatenPearls == 6
		&& !savestate.GetVoidMeetMoon())
		{
            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
			self.getToWorking = 1f;
		}
		else if (amountOfEatenPearls < 12)
		{
            PebbleVoice(self);
            self.dialogBox.Interrupt(self.Translate(
				self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 6
					? OracleConversation.eatInterruptMessages6Step[amountOfEatenPearls]
					: OracleConversation.eatInterruptMessages[amountOfEatenPearls]), 10);
			savestate.SetPebblesPearlsEaten(savestate.GetPebblesPearlsEaten() + 1);
		}

	}

	private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
	{
		orig(self);
		if (OracleConversation.PebbleVoidConversation.Contains(self.id))
		{
    //#warning may be elegible for deletion
          
			self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 30));

			var path = AssetManager.ResolveFilePath($"text/oracle/pebble/{self.id.value.ToLower()}.txt");

            if (self.owner.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/pebble11/{self.id.value.ToLower()}.txt");
			ConversationParser.GetConversationEvents(self, path);
		}
	}

    private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
    {
        if (nextAction == MeetVoid_Init || nextAction == MeetVoid_Curious)
        {
            self.inActionCounter = 0;
            self.action = nextAction;
            if (self.currSubBehavior.ID == VoidTalk)
            {
                self.currSubBehavior.Activate(self.action, nextAction);
                return;
            }

            SSOracleBehavior.SubBehavior subBehavior = null;
            for (int i = 0; i < self.allSubBehaviors.Count; i++)
            {
                if (self.allSubBehaviors[i].ID == VoidTalk)
                {
                    subBehavior = self.allSubBehaviors[i];
                    break;
                }
            }

            if (subBehavior == null)
            {
                subBehavior = new SSOracleMeetVoid_CuriousBehavior(self, self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad);
            }
            self.allSubBehaviors.Add(subBehavior);

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
		if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void
			&& self.oracle.room.game.Players.Exists(x => x.realizedCreature is Player))
		{
            if (self.timeSinceSeenPlayer < 0) self.timeSinceSeenPlayer = 0;
            var saveState = self.oracle.room.game.GetStorySession.saveState;
			var miscData = saveState.miscWorldSaveData;
			var need = miscData.SSaiConversationsHad < OracleConversation.cycleLingers.Length
				? OracleConversation.cycleLingers[miscData.SSaiConversationsHad]
				: -1;
            loginf($"HadConv: {miscData.SSaiConversationsHad}, Cycle: {saveState.cycleNumber}, LastCycle: {saveState.GetLastMeetCycles()}, NeedCycle: {need}");

			switch (miscData.SSaiConversationsHad)
			{
                case 0:
					{
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Pebble);
                        miscData.SSaiConversationsHad++;
                        self.afterGiveMarkAction = MeetVoid_Init;
                        self.NewAction(MeetVoid_Curious);
                        self.SlugcatEnterRoomReaction();
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        break;
					}
				case > 0 when saveState.cycleNumber - saveState.GetLastMeetCycles() <= 0:
					{
                        if (!saveState.GetVoidMeetMoon())
                        {
                            if(self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
						    break;
                        }
                        else
                        {
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                            break;
                        }
                    }
                case 4 when !saveState.GetVoidMeetMoon():
                    {
                        switch (UnityEngine.Random.Range(0, 3))
                        {
                            case 0:
                                PebbleVoice(self);
                                self.dialogBox.Interrupt("Come back as soon as you complete my request.".TranslateString(), 60);
                                break;
                            case 1:
                                PebbleVoice(self);
                                self.dialogBox.Interrupt("I do not see the right records in your mark. Do not worry me about nothing.".TranslateString(), 60);
                                break;
                            case 2:
                                PebbleVoice(self);
                                self.dialogBox.Interrupt("Have you already visited Looks to the Moon?".TranslateString(), 60);
                                self.dialogBox.Interrupt(". . .".TranslateString(), 60);
                                self.dialogBox.Interrupt("Leave me alone.".TranslateString(), 60);
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                            self.NewAction(self.afterGiveMarkAction);
                        self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
						break;
                    }
                case 5:
                    {
                        if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.neuronsLeft < 5)
                        {
                            PebbleVoice(self);
                            self.dialogBox.Interrupt("You should not have done that.".TranslateString(), 60);
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                            self.getToWorking = 1f;
                        }
                        else
                        {
                            if (self.action != MeetVoid_Init)
                            {
                                saveState.SetLastMeetCycles(saveState.cycleNumber);
                                if (self.currSubBehavior.ID != VoidTalk)
                                {
                                    miscData.SSaiConversationsHad++;
                                    self.NewAction(MeetVoid_Init);
                                    if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                        self.NewAction(self.afterGiveMarkAction);
                                    self.SlugcatEnterRoomReaction();
                                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                                }
                            }
                        }
                        break;
                    }
                case 6:
					{
                        if (VoidPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractVoidPearl)
                        {
                            if (self.action != MeetVoid_Init)
                            {
                                saveState.SetLastMeetCycles(saveState.cycleNumber);
                                if (self.currSubBehavior.ID != VoidTalk)
                                {
                                    GrabDataPearlAndDestroyIt(self, abstractVoidPearl.realizedObject as DataPearl);
                                    miscData.SSaiConversationsHad++;
                                    self.NewAction(MeetVoid_Init);
                                    if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                        self.NewAction(self.afterGiveMarkAction);
                                    //self.StartItemConversation(datapearl);
                                    self.SlugcatEnterRoomReaction();
                                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                                }
                            }
                        }
                        else
                        {
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                        }
                        break;
                    }
                case 7:
                    {
                        if (RotPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractRotPearl)
                        {
                            if (self.action != MeetVoid_Init)
                            {
                                saveState.SetLastMeetCycles(saveState.cycleNumber);
                                if (self.currSubBehavior.ID != VoidTalk)
                                {
                                    GrabDataPearlAndDestroyIt(self, abstractRotPearl.realizedObject as DataPearl);
                                    miscData.SSaiConversationsHad++;
                                    self.NewAction(MeetVoid_Init);
                                    if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                        self.NewAction(self.afterGiveMarkAction);
                                    //self.StartItemConversation(datapearl);
                                    self.SlugcatEnterRoomReaction();
                                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                                }
                            }
                        }
                        else
                        {
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                        }
                        break;
                    }
                case 8:
                    {
                        if (self.action != MeetVoid_Init)
                        {
                            saveState.SetLastMeetCycles(saveState.cycleNumber);
                            if (self.currSubBehavior.ID != VoidTalk)
                            {
                                miscData.SSaiConversationsHad++;
                                self.NewAction(MeetVoid_Init);
                                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                    self.NewAction(self.afterGiveMarkAction);
                                self.SlugcatEnterRoomReaction();
                                self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                            }
                        }
                        break;
                    }
                case > 10:
					{
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                            self.NewAction(self.afterGiveMarkAction);
                        self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
						break;
					}
				default:
					{
						if (self.action != MeetVoid_Init)
						{
							saveState.SetLastMeetCycles(saveState.cycleNumber);
							if (self.currSubBehavior.ID != VoidTalk)
							{
								miscData.SSaiConversationsHad++;
								self.NewAction(MeetVoid_Init);
                                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                    self.NewAction(self.afterGiveMarkAction);
                                self.SlugcatEnterRoomReaction();
								self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
							}
						}
                        break;
					}
			}
		}
		else
		{
			orig(self);
        }
    }

#nullable enable
    public static DataPearl.AbstractDataPearl? RotPearl(Room room)
    {
        foreach (Player p in room.PlayersInRoom)
        {
            if (PlayersRotPearl(p) is DataPearl.AbstractDataPearl pearl) return pearl;
        }
        foreach (UpdatableAndDeletable UAD in room.updateList)
        {
            if (UAD is DataPearl pearl
                && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-rot"))
                return pearl.AbstractPearl;
        }
        return null;

        static DataPearl.AbstractDataPearl? PlayersRotPearl(Player p)
        {
            foreach (var grasp in p.grasps)
            {
                if (grasp != null
                    && grasp.grabbed is DataPearl pearl
                    && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-rot"))
                    return pearl.AbstractPearl;
            }
            return null;
        }
    }
    public static DataPearl.AbstractDataPearl? VoidPearl(Room room)
    {
        foreach (Player p in room.PlayersInRoom)
        {
            if (PlayersVoidPearl(p) is DataPearl.AbstractDataPearl pearl) return pearl;
        }
        foreach (UpdatableAndDeletable UAD in room.updateList)
        {
            if (UAD is DataPearl pearl
                && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-void"))
                return pearl.AbstractPearl;
        }
        return null;

        static DataPearl.AbstractDataPearl? PlayersVoidPearl(Player p)
        {
            foreach (var grasp in p.grasps)
            {
                if (grasp != null
                    && grasp.grabbed is DataPearl pearl
                    && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-void"))
                    return pearl.AbstractPearl;
            }
            return null;
        }
    }
#nullable disable

    private static void ThrowOutBehavior_Update(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
    {
        orig(self);
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.owner is SSOracleBehavior self2 && 
               (self.owner.throwOutCounter == 700 || 
                self.owner.throwOutCounter == 980 ||
                self.owner.throwOutCounter == 1530))
            {
                PebbleVoice(self2);
            }
        }
    }
#nullable enable
    private static void SSOracleBehavior_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
    {
        orig(self, eventName);
        switch (eventName)
        {
            case "PebbleVoice":
                {
                    PebbleVoice(self);
                    break;
                }
            case "GrabPearl":
                {
                    DataPearl? pearl = self.oracle.room.updateList.FirstOrDefault(x => x is DataPearl pearl
                    && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-void")) as DataPearl;
                    GrabDataPearlAndDestroyIt(self, pearl);
                    break;
                }
        }
    }
#nullable disable

#nullable enable
    static void GrabDataPearlAndDestroyIt(SSOracleBehavior self, DataPearl? pearl)
    {
        //kill realized pearl, replace with a custom one seamlessly
        if (pearl == null)
        {
            self.conversation.Destroy();
            return;
        }
        pearl.AllGraspsLetGoOfThisObject(true);
        pearl.slatedForDeletetion = true;
        var roomref = self.oracle.room;
        //oh my god DataPearl ctor takes in world but does nothing with it
        var hoveringPearl = new HoveringPearl(pearl.abstractPhysicalObject, roomref.world);
        pearl.abstractPhysicalObject.realizedObject = hoveringPearl;
        hoveringPearl.bodyChunks[0].pos = pearl.bodyChunks[0].pos;
        hoveringPearl.hoverPos = roomref.MiddleOfTile(roomref.Width / 2, roomref.Height / 2);
        hoveringPearl.OnPearlTaken += () =>
        {
            self.dialogBox.Interrupt("Good try, but it is not for you".TranslateString(), 200);
            DestroyPearl(self, hoveringPearl);
        };
        hoveringPearl.OnWaitCompleted += () =>
        {
            DestroyPearl(self, hoveringPearl);
        };
        hoveringPearl.PlaceInRoom(roomref);
        hoveringPearl.AsyncWait(15000);

        static void DestroyPearl(OracleBehavior oracleBehavior, DataPearl pearl)
        {
            pearl.slatedForDeletetion = true;
            pearl.AbstractPearl.Destroy();
            for (int num8 = 0; num8 < 5; num8++)
            {
                oracleBehavior.oracle.room.AddObject(new Spark(pearl.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
            }
            oracleBehavior.oracle.room.AddObject(new Explosion.ExplosionLight(pearl.firstChunk.pos, 150f, 1f, 8, Color.white));
            oracleBehavior.oracle.room.AddObject(new ShockWave(pearl.firstChunk.pos, 60f, 0.1f, 8, false));
            oracleBehavior.oracle.room.PlaySound(SoundID.Snail_Pop, pearl.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value * 0.5f);
        }
    }
#nullable disable
    private static void ILSSOracleBehavior_Update(ILContext il)
	{
		ILCursor c = new ILCursor(il);

        // this.dialogBox.Interrupt(this.Translate("Yes, help yourself. They are not edible." < OR PICK ONE OF CUSTOM INTERRUPT LINES AFTER 1ST MEET>), 10);
        if (c.TryGotoNext(MoveType.After, i => i.MatchLdstr("Yes, help yourself. They are not edible.")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<string, SSOracleBehavior, string>>((str, self) =>
            {
				//SSAIConversationsHad is assigned the moment oracle sees player.
				//so the actual number is ConversationsHad - 1
				//and counting from second time, need another -1 to map meetings 1, 2, 3... to indexes 0, 1, 2...
                var amountOfPreviousMeetings = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1;
                if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void
                && amountOfPreviousMeetings > 0
                && amountOfPreviousMeetings < OracleConversation.pickInterruptMessages.Length)
                {
                    return OracleConversation.pickInterruptMessages[amountOfPreviousMeetings];
                }
                PebbleVoice(self);
                return str;
            });
        }
        else LogExErr("failed to match eating string");
        //else if (!ModManager.MSC
		//|| (this.oracle.ID == Oracle.OracleID.SS
		//	&& this.oracle.room.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Artificer <OR VOID>
		//	&& !flag2))
        //{
        //    (this.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap = 9;
        //}
        if (c.TryGotoNext(MoveType.After,
			i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"),
			i => i.MatchCall(out var method) && method.Name.Contains("op_Inequality"),
			i => i.Match(OpCodes.Brfalse_S),
			i => i.MatchLdloc(10)))
		{
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, SSOracleBehavior, bool>>((re, self)
                => self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void || re);
        }
		else LogExErr("failed to match comparison to artificer");


	}

    public static class OracleConversation
    {
        public static Conversation.ID[] PebbleVoidConversation;
        public static Conversation.ID[] MoonVoidConversation;

        public static int[] cycleLingers = [0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
        public static int[] MooncycleLingers = [0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];

        static OracleConversation()
        {
            PebbleVoidConversation = new Conversation.ID[11];
            for (int i = 0; i < PebbleVoidConversation.Length; i++)
                PebbleVoidConversation[i] = new($"pebblevoidconversation_{i + 1}", true);
            MoonVoidConversation = new Conversation.ID[11];
            for (int i = 0; i < MoonVoidConversation.Length; i++)
                MoonVoidConversation[i] = new($"moonvoidconversation_{i + 1}", true);
        }

        public static string[] pickInterruptMessages =
        [
            "Yes, help yourself. They are not edible.",
            "You seem to like pearls.",
            "Are you listening to me?",
            "You need to stop being distracted by pearls.",
            "Are you trying my patience?",
            "I will tell you about this pearls another time.",
            "Yes... this pearl contains data about my structure.",
            "Strange... This pearl should not be here.",
            "This pearl made by many generations, it contains the stories, technologies and thoughts of long-gone civilization.",
            "You may notice that this pearl is slightly faded, unfortunately, even in them, information is not eternal.",
            "I wish I could just teach you how to read them so you would stop bothering me about little things like that."
        ];

        public static string[] eatInterruptMessages =
        [
            "I am not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature, you should not eat pearls.",
            "You must stop right now.",
            "I am warning you for the last time."
        ];

        public static string[] eatInterruptMessages6Step =
        [
            "I am not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature, you should not eat pearls.",
            "Can you stop dissolving my pearls?",
            "You just ate something more valuable than you can imagine.",
            "These pearls that you have swallowed, do they disappear without a trace or just become a part of you? In any case, I cannot get it back.",
            "Considering that your body weight does not change, this means that all the objects you eat are dissolved by the void fluid.",
            "If to assume that all the water in your body has been displaced by the void fluid,<LINE>its concentration is still insufficient to dissolve objects in such a short period of time.",
            "Watching you, I can assume that your body is invisibly connected to the void sea, but this thought alone raises even more questions.",
            "I would never have thought that such wasteful use of pearls would bring me closer to understanding the nature of the void sea.",
            "Eat as much as you want, from now I will no longer store important information here in your presence."
        ];



    }
    public class SSOracleVoidBehavior : SSOracleBehavior.ConversationBehavior
    {
        public SSOracleVoidBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.PebbleVoidConversation[times - 1])
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
                this.SSOracleVoidCommonConvoEnd();
            }
            else
            {
                owner.LockShortcuts();
            }
        }

        public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            if (newAction == MeetVoid_Init && owner.conversation == null)
            {
                owner.InitateConversation(OracleConversation.PebbleVoidConversation[MeetTimes - 1], this);
                base.NewAction(oldAction, newAction);
            }

        }

        int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;
    }
    private static void PebbleVoice(SSOracleBehavior self)
    {
        SoundID randomTalk = SoundID.SS_AI_Talk_1;
        switch (UnityEngine.Random.Range(0, 5))
        {
            case 0:
                randomTalk = SoundID.SS_AI_Talk_1;
                break;
            case 1:
                randomTalk = SoundID.SS_AI_Talk_2;
                break;
            case 2:
                randomTalk = SoundID.SS_AI_Talk_3;
                break;
            case 3:
                randomTalk = SoundID.SS_AI_Talk_4;
                break;
            case 4:
                randomTalk = SoundID.SS_AI_Talk_5;
                break;
        }
        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
        {
            self.AirVoice(randomTalk);
        }
    }
}

public class SSOracleMeetVoid_CuriousBehavior : SSOracleBehavior.ConversationBehavior
{
    public static SSOracleBehavior.Action MeetVoid_Talking = new SSOracleBehavior.Action("MeetVoid_Talking", true);
    public static SSOracleBehavior.Action MeetVoid_Texting = new SSOracleBehavior.Action("MeetVoid_Texting", true);
    public static SSOracleBehavior.Action MeetVoid_FirstImages = new SSOracleBehavior.Action("MeetVoid_FirstImages", true);
    public static SSOracleBehavior.Action MeetVoid_SecondCurious = new SSOracleBehavior.Action("MeetVoid_SecondCurious", true);
    int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;

    public ChunkSoundEmitter voice
    {
        get
        {
            return this.owner.voice;
        }
        set
        {
            this.owner.voice = value;
        }
    }

    public SSOracleMeetVoid_CuriousBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.PebbleVoidConversation[times - 1])
    {
        this.chatLabel = new OracleChatLabel(owner);
        this.showMediaPos = new Vector2(400f, 300f);
        this.oracle.room.AddObject(this.chatLabel);
        this.chatLabel.Hide();
        if (ModManager.MMF && owner.oracle.room.game.IsStorySession && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked && base.oracle.room.world.rainCycle.timer > base.oracle.room.world.rainCycle.cycleLength / 4)
        {
            base.oracle.room.world.rainCycle.timer = this.oracle.room.world.rainCycle.cycleLength / 4;
            base.oracle.room.world.rainCycle.dayNightCounter = 0;
        }
    }

    public override void Update()
    {
        if (base.player == null)
        {
            return;
        }
        this.owner.LockShortcuts();
        this.owner.getToWorking = 0f;

        if (base.action == MeetVoid_Curious)
        {
            if (inActionCounter < 360)
                this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            else
                this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
            if (inActionCounter > 360)
            {
                this.owner.NewAction(MeetVoid_Talking);
                //UnityEngine.Debug.Log("MeetVoid_Talking");
                return;
            }
        }
        else if (base.action == MeetVoid_Talking)
        {
            this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
            if (!this.CurrentlyCommunicating && this.communicationPause > 0)
            {
                this.communicationPause--;
            }
            if (!this.CurrentlyCommunicating && this.communicationPause < 1)
            {
                if (this.communicationIndex >= 4)
                {
                    this.owner.NewAction(MeetVoid_Texting);
                    //UnityEngine.Debug.Log("MeetVoid_Texting");
                }
                else if (this.owner.allStillCounter > 20)
                {
                    this.NextCommunication();
                    //UnityEngine.Debug.Log("Next Commu");
                }
            }
            if (!this.CurrentlyCommunicating)
            {
                this.owner.nextPos += Custom.RNV();
                return;
            }
        }
        else
        {
            if (base.action == MeetVoid_Texting)
            {
                base.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                if (base.oracle.graphicsModule != null)
                {
                    (base.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 0f;
                }
                if (!this.CurrentlyCommunicating && this.communicationPause > 0)
                {
                    this.communicationPause--;
                }
                if (!this.CurrentlyCommunicating && this.communicationPause < 1)
                {
                    if (this.communicationIndex >= 6)
                    {
                        this.owner.NewAction(MeetVoid_FirstImages);
                        //UnityEngine.Debug.Log("MeetVoid_FirstImages");
                    }
                    else if (this.owner.allStillCounter > 20)
                    {
                        this.NextCommunication();
                        this.communicationPause = 150;
                    }
                }
                return;
            }
            if (base.action == MeetVoid_FirstImages)
            {
                base.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                if (this.communicationPause > 0)
                {
                    this.communicationPause--;
                }

                if (inActionCounter > 150 && this.communicationPause < 1)
                {
                    if (base.action == MeetVoid_FirstImages && this.communicationIndex >= 3)
                    {
                        this.owner.NewAction(MeetVoid_SecondCurious);
                        //UnityEngine.Debug.Log("MeetVoid_SecondCurious");
                    }
                    else
                    {
                        this.NextCommunication();
                    }
                }
                if (this.showImage != null)
                {
                    this.showImage.setPos = new Vector2?(this.showMediaPos);
                }
                if (UnityEngine.Random.value < 0.0333333351f)
                {
                    this.idealShowMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                    this.showMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                    return;
                }
            }
            else if (base.action == MeetVoid_SecondCurious)
            {
                base.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                if (inActionCounter == 80)
                {
                    Custom.Log(new string[]
                    {
                        "extra talk"
                    });
                    this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_5, base.oracle.firstChunk);
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        this.dialogBox.Interrupt(". . .".TranslateString(), 60);
                        this.dialogBox.Interrupt("I can see by your face that you understand me.".TranslateString(), 60);
                    }
                    this.voice.requireActiveUpkeep = true;
                }
                if (inActionCounter > 240)
                {
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap != 10)
                        this.owner.NewAction(SSOracleBehavior.Action.General_GiveMark);
                    if (this.owner.conversation != null)
                    {
                        this.owner.conversation.paused = false;
                    }
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        this.owner.NewAction(this.owner.afterGiveMarkAction);
                    this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                }
                return;
            }
        }

        if (owner.conversation != null && owner.conversation.slatedForDeletion == true)
        {
            this.SSOracleVoidCommonConvoEnd();
        }
    }

    public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
    {
        base.NewAction(oldAction, newAction);
        if (oldAction == MeetVoid_Texting)
        {
            this.chatLabel.Hide();
        }
        if ((oldAction == MeetVoid_FirstImages) && this.showImage != null)
        {
            this.showImage.Destroy();
            this.showImage = null;
        }
        if (newAction == MeetVoid_Curious)
        {
            this.owner.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
            this.owner.invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1f : 1f);
            return;
        }
        if (newAction == MeetVoid_Texting)
        {
            this.communicationPause = 170;
            this.chatLabel.pos = this.showMediaPos;
            this.chatLabel.lastPos = this.showMediaPos;
            return;
        }
        if (newAction == MeetVoid_Init && owner.conversation == null)
        {
            owner.InitateConversation(OracleConversation.PebbleVoidConversation[MeetTimes - 1], this);
        }
    }

    public override void Deactivate()
    {
        this.chatLabel.Hide();
        this.showImage?.Destroy();
        this.voice = null;
        base.Deactivate();
    }

    private void NextCommunication()
    {
        Custom.Log(new string[]
        {
            string.Format("New com att: {0} {1}", base.action, this.communicationIndex)
        });
        //UnityEngine.Debug.Log("NextCommu");
        if (base.action == MeetVoid_Talking)
        {
            switch (this.communicationIndex)
            {
                case 0:
                    this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, base.oracle.firstChunk);
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        this.dialogBox.Interrupt("Did someone send a messenger to me?".TranslateString(), 60);
                    }
                    this.voice.requireActiveUpkeep = true;
                    this.communicationPause = 10;
                    break;
                case 1:
                    this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_2, base.oracle.firstChunk);
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        this.dialogBox.Interrupt("It does not have a mark.".TranslateString(), 30);
                        this.dialogBox.NewMessage("It is just another pest was able to get into my structure.".TranslateString(), 30);
                    }
                    this.voice.requireActiveUpkeep = true;
                    this.communicationPause = 70;
                    break;
                case 2:
                    this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_3, base.oracle.firstChunk);
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        this.dialogBox.Interrupt("You look unnatural.".TranslateString(), 60);
                        this.dialogBox.NewMessage("What happened to you? There are clear signs of external interference here.".TranslateString(), 60);
                    }
                    this.voice.requireActiveUpkeep = true;
                    break;
                case 3:
                    this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, base.oracle.firstChunk);
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        this.dialogBox.Interrupt("A rather strange creature.".TranslateString(), 60);
                    }
                    this.voice.requireActiveUpkeep = true;
                    this.communicationPause = 140;
                    break;
            }
        }
        else if (base.action == MeetVoid_Texting)
        {
            this.chatLabel.NewPhrase(this.communicationIndex);
        }
        else if (base.action == MeetVoid_FirstImages)
        {
            if (this.showImage != null)
            {
                this.showImage.Destroy();
            }

            switch (this.communicationIndex)
            {
                case 0:
                    this.showImage = base.oracle.myScreen.AddImage("aiimg1_void");
                    this.communicationPause = 380;
                    break;
                case 1:
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        this.showImage = base.oracle.myScreen.AddImage("aiimg2_void");
                    else
                        this.showImage = base.oracle.myScreen.AddImage("aiimg3_void");
                    this.communicationPause = 290;
                    break;
                case 2:
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap != 10)
                    {
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, base.oracle.firstChunk);
                        this.showImage = base.oracle.myScreen.AddImage(new List<string>
                        {
                            "void_glyphs_3",
                            "void_glyphs_5"
                        }, 30);
                    }
                    else
                    {
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_3, base.oracle.firstChunk);
                        this.showImage = base.oracle.myScreen.AddImage(new List<string>
                        {
                            "void_glyphs_4",
                            "void_glyphs_5"
                        }, 30);
                        this.dialogBox.Interrupt("Three... four spirals. The genes are twisted into a super-dense structure. This form is almost immune to the external environment...".TranslateString(), 60);
                    }
                    this.communicationPause = 330;
                    break;
            }

            if (this.showImage != null)
            {
                base.oracle.room.PlaySound(SoundID.SS_AI_Image, 0f, 1f, 1f);
                this.showImage.lastPos = this.showMediaPos;
                this.showImage.pos = this.showMediaPos;
                this.showImage.lastAlpha = 0f;
                this.showImage.alpha = 0f;
                this.showImage.setAlpha = new float?(1f);
            }
        }
        this.communicationIndex++;
    }

    public void ShowMediaMovementBehavior()
    {
        if (base.player != null)
        {
            this.owner.lookPoint = base.player.DangerPos;
        }
        Vector2 vector = new Vector2(UnityEngine.Random.value * base.oracle.room.PixelWidth, UnityEngine.Random.value * base.oracle.room.PixelHeight);
        if (this.owner.CommunicatePosScore(vector) + 40f < this.owner.CommunicatePosScore(this.owner.nextPos) && !Custom.DistLess(vector, this.owner.nextPos, 30f))
        {
            this.owner.SetNewDestination(vector);
        }
        this.consistentShowMediaPosCounter += (int)Custom.LerpMap(Vector2.Distance(this.showMediaPos, this.idealShowMediaPos), 0f, 200f, 1f, 10f);
        vector = new Vector2(UnityEngine.Random.value * base.oracle.room.PixelWidth, UnityEngine.Random.value * base.oracle.room.PixelHeight);
        if (this.ShowMediaScore(vector) + 40f < this.ShowMediaScore(this.idealShowMediaPos))
        {
            this.idealShowMediaPos = vector;
            this.consistentShowMediaPosCounter = 0;
        }
        vector = this.idealShowMediaPos + Custom.RNV() * UnityEngine.Random.value * 40f;
        if (this.ShowMediaScore(vector) + 20f < this.ShowMediaScore(this.idealShowMediaPos))
        {
            this.idealShowMediaPos = vector;
            this.consistentShowMediaPosCounter = 0;
        }
        if (this.consistentShowMediaPosCounter > 300)
        {
            this.showMediaPos = Vector2.Lerp(this.showMediaPos, this.idealShowMediaPos, 0.1f);
            this.showMediaPos = Custom.MoveTowards(this.showMediaPos, this.idealShowMediaPos, 10f);
        }
    }

    private float ShowMediaScore(Vector2 tryPos)
    {
        if (base.oracle.room.GetTile(tryPos).Solid || base.player == null)
        {
            return float.MaxValue;
        }
        float num = Mathf.Abs(Vector2.Distance(tryPos, base.player.DangerPos) - 250f);
        num -= Math.Min((float)base.oracle.room.aimap.getTerrainProximity(tryPos), 9f) * 30f;
        num -= Vector2.Distance(tryPos, this.owner.nextPos) * 0.5f;
        for (int i = 0; i < base.oracle.arm.joints.Length; i++)
        {
            num -= Mathf.Min(Vector2.Distance(tryPos, base.oracle.arm.joints[i].pos), 100f) * 10f;
        }
        if (base.oracle.graphicsModule != null)
        {
            for (int j = 0; j < (base.oracle.graphicsModule as OracleGraphics).umbCord.coord.GetLength(0); j += 3)
            {
                num -= Mathf.Min(Vector2.Distance(tryPos, (base.oracle.graphicsModule as OracleGraphics).umbCord.coord[j, 0]), 100f);
            }
        }
        return num;
    }

    public override bool CurrentlyCommunicating
    {
        get
        {
            return base.CurrentlyCommunicating || this.voice != null || (base.action == SSOracleBehavior.Action.MeetWhite_Texting && !this.chatLabel.finishedShowingMessage) || this.showImage != null;
        }
    }

    public ProjectedImage showImage;
    public Vector2 idealShowMediaPos;
    public Vector2 showMediaPos;
    public int consistentShowMediaPosCounter;
    public OracleChatLabel chatLabel;
}
public static class OracleExtensionMethods
{
    public static void AirVoice(this SSOracleBehavior self, SoundID line)
    {
        if (self.voice != null)
        {
            self.voice.currentSoundObject?.Stop();
            self.voice.Destroy();
        }
        self.voice = self.oracle.room.PlaySound(line, self.oracle.firstChunk);
    }
}