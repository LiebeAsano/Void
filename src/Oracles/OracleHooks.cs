using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using VoidTemplate.Useful;
using static UnityEngine.Mesh;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.VoidEnums.ConversationID;

namespace VoidTemplate.Oracles;

static class OracleHooks
{
	public static void Hook()
	{
        On.Oracle.ctor += Oracle_ctor;
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
		On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
		On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
		On.SSOracleBehavior.Update += OnSSOracleBehavior_Update;
		IL.SSOracleBehavior.Update += ILSSOracleBehavior_Update;
	}

    #region immutable
    private static readonly Conversation.ID[] modSpecificConversations = [Moon_VoidConversation];
    public static SSOracleBehavior.Action MeetVoid_Init = new("MeetVoid_Init", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new("VoidTalk", true);
	#endregion

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
		if (amountOfEatenPearls == 6
		&& self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad < 6)
		{

			self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
			self.getToWorking = 1f;


		}
		else if (amountOfEatenPearls < 12)
		{
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
			if (!self.owner.playerEnteredWithMark)
				self.events.Add(new Conversation.TextEvent(self, 0, ". . .", 0));
			else
				self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 30));

			var path = AssetManager.ResolveFilePath($"text/oracle/pebble/{self.id.value.ToLower()}.txt");

            if (self.owner.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/pebble11/{self.id.value.ToLower()}.txt");

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
				self.events.Add(new Conversation.TextEvent(self, 0,
					$"text/oracle/{self.id.value.ToLower()}.txt can't find!", 0));
			}
		}
	}

    private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
	{
		if (nextAction == MeetVoid_Init)
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
                self.LockShortcuts();
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
		if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void
			&& self.oracle.room.game.Players.Exists(x => x.realizedCreature is Player))
		{
			var saveState = self.oracle.room.game.GetStorySession.saveState;
			var miscData = saveState.miscWorldSaveData;
			var need = miscData.SSaiConversationsHad < OracleConversation.cycleLingers.Length
				? OracleConversation.cycleLingers[miscData.SSaiConversationsHad]
				: -1;
            LogExInf($"HadConv: {miscData.SSaiConversationsHad}, Cycle: {saveState.cycleNumber}, LastCycle: {saveState.GetLastMeetCycles()}, NeedCycle: {need}");

			switch (miscData.SSaiConversationsHad)
			{
                case 0:
					{
						miscData.SSaiConversationsHad++;
                        self.afterGiveMarkAction = MeetVoid_Init;
                        self.NewAction(SSOracleBehavior.Action.General_GiveMark);
                        self.SlugcatEnterRoomReaction(); 
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Pebble);
                        break;
					}
				case > 0 when saveState.cycleNumber - saveState.GetLastMeetCycles() > 0:
                case 2 when saveState.deathPersistentSaveData.karmaCap < 4:
				case 5 when miscData.SLOracleState.playerEncountersWithMark - saveState.GetEncountersWithMark() <= 0:
					{
						self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
						break;
					}
				case > 10:
					{
						self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
						break;
					}
				default:
					{
						if (self.action != MeetVoid_Init)
						{
							saveState.SetLastMeetCycles(saveState.cycleNumber);
							if (self.timeSinceSeenPlayer < 0) self.timeSinceSeenPlayer = 0;
							if (self.currSubBehavior.ID != VoidTalk)
							{
								miscData.SSaiConversationsHad++;
								self.NewAction(MeetVoid_Init);
                                self.SlugcatEnterRoomReaction();
								self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
							}
						}
                        if (miscData.SSaiConversationsHad == 4)
                            saveState.SetEncountersWithMark(miscData.SLOracleState.playerEncountersWithMark);
                        if (miscData.SSaiConversationsHad == 5)
							saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Rot);
                        break;
					}
			}
		}
		else
		{
			orig(self);
		}
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

    private static void OnSSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
		orig(self, eu);
		if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void)
		{
            var saveState = self.oracle.room.game.GetStorySession.saveState;
            var miscData = saveState.miscWorldSaveData;

            if (self.conversation == null || self.conversation.slatedForDeletion || self.conversation.events == null)
            {
				if (miscData.SSaiConversationsHad < 5)
				{
                    self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                }
				else
				{
                    self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                }
                self.UnlockShortcuts();
			}
			else
			{
                self.LockShortcuts();
            }

		}

	}
    private static void ILSSOracleBehavior_Update(ILContext il)
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
			=> self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void || re);

		c2.GotoNext(MoveType.After, i => i.MatchLdstr("Yes, help yourself. They are not edible."));
		c2.Emit(OpCodes.Ldarg_0);
		c2.EmitDelegate<Func<string, SSOracleBehavior, string>>((str, self) =>
		{
			if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void &&
				OracleConversation.pickInterruptMessages.Length >
				self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1)
			{
				return OracleConversation.pickInterruptMessages[
					self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1];
			}

			return str;
		});
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
            "Strange... This pearl shouldn't be here.",
            "This pearl made by many generations, it contains the stories, technologies and thoughts of long-gone civilizations.",
            "You may notice that this pearl is slightly faded, unfortunately, even in them, information is not eternal.",
            "I wish I could just teach you how to read them..."
        ];

        public static string[] eatInterruptMessages =
        [
            "I'm not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature. You shouldn't eat pearls.",
            "You must stop right now.",
            "I'm warning you for the last time."
        ];

        public static string[] eatInterruptMessages6Step =
        [
            "I'm not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature. You shouldn't eat pearls.",
            "Can you stop dissolving my pearls?",
            "You just ate something more valuable than you can imagine.",
            "This pearl that you have swallowed, do they disappear without a trace or just become a part of you? In any case, I can't get it back.",
            "Considering that your body weight does not change, this means that all the objects you eat are simply dissolved by the void fluid.",
            "Although if to assume that all the water in your body has been displaced by the void fluid, its concentration is still insufficient to dissolve objects so fast.",
            "Watching you, I can assume that your body is invisibly connected to the void sea, but this thought alone raises even more questions.",
            "I would never have thought that such wasteful use of pearls could bring me closer to understanding the nature of the void sea.",
            "Eat as much as you want, from now on I will no longer store important information here in your presence."
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
                owner.UnlockShortcuts();
                owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                owner.getToWorking = 1f;
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
}
