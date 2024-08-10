using System;
using VoidTemplate;
using UnityEngine;

public static class PlayerSpawnManager
{
    public static void ApplyHooks()
    {
        On.Player.Update += Player_Update;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.room is Room playerRoom 
            && playerRoom.game.IsStorySession 
            && playerRoom.game.GetStorySession.saveStateNumber == VoidEnums.SlugcatID.TheVoid
            && playerRoom.game.GetStorySession.saveState is SaveState save
            && !save.GetTeleportationDone())
        {
            InitializeTargetRoomID(playerRoom);

            int currentRoomIndex = self.abstractCreature.pos.room;

            if (currentRoomIndex == NewSpawnPoint.room)
            {
                save.SetTeleportationDone(true);
                save.EnlistDreamInShowQueue(SaveManager.Dream.Farm);
                self.abstractCreature.pos = NewSpawnPoint;
                Vector2 newPosition = self.room.MiddleOfTile(NewSpawnPoint.x, NewSpawnPoint.y);
                Array.ForEach(self.bodyChunks, x => x.pos = newPosition);
                self.standing = true;
                self.animation = Player.AnimationIndex.StandUp;
            }
        }
    }

    #region minor helper functions

    private static int targetRoomID = -1;
    static WorldCoordinate NewSpawnPoint
    {
        get
        {
            if (targetRoomID == -1) throw new Exception("Target room ID is not initialized!");
            return new WorldCoordinate(targetRoomID, originalSpawnPoint.x, originalSpawnPoint.y, originalSpawnPoint.abstractNode);
        }
    }

    private static readonly WorldCoordinate originalSpawnPoint = new WorldCoordinate(-1, 27, 13, 0);

    static void InitializeTargetRoomID(Room room)
    {
        if (targetRoomID == -1)
        {
            AbstractRoom targetRoom = room.world.GetAbstractRoom("SB_A14") ?? throw new Exception($"Room 'SB_A14' does not exist.");
            targetRoomID = targetRoom.index;
        }
    }

    #endregion
}