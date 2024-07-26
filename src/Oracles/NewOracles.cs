using System;
using UnityEngine;
using CoralBrain;
using RWCustom;
using MoreSlugcats;
using static DataPearl.AbstractDataPearl;
using System.Collections.Generic;
using System.Linq;

namespace VoidTemplate.Oracles;

internal class NewOracles
{
    public static void Hook()
    {
        //it was assumed that room spawns specific oracles. Nope. It spawns generic oracles, which check room in constructor
        On.Oracle.ctor += Oracle_ctor;
    }

    private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
    {
        orig(self, abstractPhysicalObject, room);
        if(room.game.StoryCharacter == StaticStuff.TheVoid)
        {
            switch(room.world.name)
            {
                case "SL":
                    {
                        self.ID = new("VoidSLOracle");
                        
                        break;
                    }
                case "SS":
                    {
                        self.ID = new("VoidSSOracle");
                        self.oracleBehavior = new SSVoidOracleBehavior(self);
                        break;
                    }
            }
        }

       
    }
}
public class SSVoidOracleBehavior : OracleBehavior
{
    private float unconciousTick;
    private SLOracleBehaviorHasMark.MoonConversation pearlConversation;
    private List<EntityID> talkedAboutThisSession;
    private SSOracleBehavior.PebblesConversation conversation;
    private int timeSinceSeenPlayer;
    private Vector2 lastPos;
    private Vector2 nextPos;
    private Vector2 lastPosHandle;
    private Vector2 nextPosHandle;
    private float pathProgression;
    private Vector2 currentGetTo;

    private RainWorld RainWorld => oracle.room.game.rainWorld;

    public SSVoidOracleBehavior(Oracle oracle) : base(oracle)
    {

    }
    #region subclasses
    static class
    #endregion

    #region minor helping functions
    private float BasePosScore(Vector2 tryPos)
    {
        if (this.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate || this.player == null)
        {
            return Vector2.Distance(tryPos, this.oracle.room.MiddleOfTile(24, 5));
        }
        if (this.movementBehavior == SSOracleBehavior.MovementBehavior.ShowMedia)
        {
            return -Vector2.Distance(this.player.DangerPos, tryPos);
        }
        return Mathf.Abs(Vector2.Distance(this.nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(this.player.DangerPos, tryPos), 40f, 300f, 800f, 0f);
    }
    private void LockShortcuts()
    {
        if (oracle.room.lockedShortcuts.Count != 0) return;
        Array.ForEach(oracle.room.shortcutsIndex, oracle.room.lockedShortcuts.Add);
    }
    private void UnlockShortcuts() => oracle.room.lockedShortcuts.Clear();

    public override void UnconciousUpdate()
    {
        oracle.room.gravity = 1;
        oracle.setGravity(.9f);
        Array.ForEach(oracle.room.game.cameras, cam =>
        {
            if (cam.room == oracle.room 
            && !cam.AboutToSwitchRoom) 
                cam.ChangeBothPalettes(10, 26, 0.51f + Mathf.Sin(unconciousTick * 0.25707963f) * 0.35f);
        });

    }
    public void TurnOffSSMusic(bool abruptEnd)
    {
        oracle.room.updateList.ForEach(UAD =>
        {
            if (UAD is SSMusicTrigger) UAD.Destroy();
        });
        if (abruptEnd 
            && oracle.room.game.manager.musicPlayer is Music.MusicPlayer player 
            && player.song is Music.SSSong song)
        {
            song.FadeOut(2f);
        }
    }
    public Vector2 storedPearlOrbitLocation(int index)
    {
        float gridSize = 5f;
        float row = (float)index % gridSize;
        float column = Mathf.Floor((float)index / gridSize);
        float yalign = row * 0.5f;
        return new Vector2(615f, 100f) + new Vector2(row * 26f, (column + yalign) * 18f);
    }

    public void StartItemConversation(DataPearl item)
    {
        SLOrcacleState sloracleState = oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState;
        isRepeatedDiscussion = false;
        if (item.AbstractPearl.dataPearlType == DataPearlType.Misc || item.AbstractPearl.dataPearlType.Index == -1)
        {
            pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(Conversation.ID.Moon_Pearl_Misc, this, SLOracleBehaviorHasMark.MiscItemType.NA);
        }
        else if (item.AbstractPearl.dataPearlType == DataPearlType.Misc2)
        {
            pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(Conversation.ID.Moon_Pearl_Misc2, this, SLOracleBehaviorHasMark.MiscItemType.NA);
        }
        else if (item.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.BroadcastMisc)
        {
            pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(MoreSlugcatsEnums.ConversationID.Moon_Pearl_BroadcastMisc, this, SLOracleBehaviorHasMark.MiscItemType.NA);
        }
        else if (oracle.ID == MoreSlugcatsEnums.OracleID.DM && item.AbstractPearl.dataPearlType == DataPearlType.PebblesPearl && (item.AbstractPearl as PebblesPearl.AbstractPebblesPearl).color >= 0)
        {
            pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(Conversation.ID.Moon_Pebbles_Pearl, this, SLOracleBehaviorHasMark.MiscItemType.NA);
        }
        else
        {
            if (pearlConversation != null)
            {
                pearlConversation.Interrupt("...", 0);
                pearlConversation.Destroy();
                pearlConversation = null;
            }
            Conversation.ID id = Conversation.DataPearlToConversation(item.AbstractPearl.dataPearlType);
            if (!sloracleState.significantPearls.Contains(item.AbstractPearl.dataPearlType))
            {
                sloracleState.significantPearls.Add(item.AbstractPearl.dataPearlType);
            }
            if (ModManager.MSC && oracle.ID == MoreSlugcatsEnums.OracleID.DM)
            {
                isRepeatedDiscussion = rainWorld.progression.miscProgressionData.GetDMPearlDeciphered(item.AbstractPearl.dataPearlType);
                rainWorld.progression.miscProgressionData.SetDMPearlDeciphered(item.AbstractPearl.dataPearlType, false);
            }
            else
            {
                isRepeatedDiscussion = rainWorld.progression.miscProgressionData.GetPebblesPearlDeciphered(item.AbstractPearl.dataPearlType);
                rainWorld.progression.miscProgressionData.SetPebblesPearlDeciphered(item.AbstractPearl.dataPearlType, false);
            }
            pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(id, this, SLOracleBehaviorHasMark.MiscItemType.NA);
            sloracleState.totalPearlsBrought++;
        }
        if (!isRepeatedDiscussion)
        {
            sloracleState.totalItemsBrought++;
            sloracleState.AddItemToAlreadyTalkedAbout(item.abstractPhysicalObject.ID);
        }
        talkedAboutThisSession.Add(item.abstractPhysicalObject.ID);
    }
    public new void SpecialEvent(string eventName)
    {
        Custom.Log(new string[] { "SPECEVENT :", eventName });
        if (eventName == "karma")
        {
            if (conversation != null)
            {
                conversation.paused = true;
            }
            afterGiveMarkAction = action;
            NewAction(SSOracleBehavior.Action.General_GiveMark);
        }
    }
    public void SlugcatEnterRoomReaction()
    {
        getToWorking = 0f;
        oracle.room.PlaySound(SoundID.SS_AI_Exit_Work_Mode, 0f, 1f, 1f);
        if (oracle.graphicsModule is OracleGraphics graphics)
        {
            graphics.halo.ChangeAllRadi();
            graphics.halo.connectionsFireChance = 1f;
        }
        TurnOffSSMusic(true);
    }
    private void SetNewDestination(Vector2 dst)
    {
        this.lastPos = this.currentGetTo;
        this.nextPos = dst;
        this.lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
        this.nextPosHandle = -this.GetToDir * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
        this.pathProgression = 0f;
    }
    public void SeePlayer()
    {
        if (this.timeSinceSeenPlayer < 0)
        {
            this.timeSinceSeenPlayer = 0;
        }
        this.SlugcatEnterRoomReaction();
    }

}

    
    #endregion

public class SLVoidOracleMarkBehavior : OracleBehavior
{
    public SLVoidOracleMarkBehavior(Oracle oracle) : base(oracle)
    {
    }
}
