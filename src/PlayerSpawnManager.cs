using System;
using VoidTemplate;
using UnityEngine;
using TheVoid;
public static class PlayerSpawnManager
{
    public static bool isSpawned = false;
    public static void ApplyHooks()
    {
        On.Player.Update += Player_Update;
        On.StoryGameSession.AddPlayer += static (orig, self, abstractPlayer) =>
        {
            orig(self, abstractPlayer);
            isSpawned = false;
            Plugin.logger.LogMessage("Player added, isSpawned reset");
        };
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (!isSpawned && self.room is Room playerRoom)
        {
            InitializeTargetRoomID(playerRoom);

            int currentRoomIndex = self.abstractCreature.pos.room;

            if (currentRoomIndex == NewSpawnPoint.room)
            {
                self.abstractCreature.pos = NewSpawnPoint;
                Vector2 newPosition = self.room.MiddleOfTile(NewSpawnPoint.x, NewSpawnPoint.y);
                self.firstChunk.pos = newPosition;
                self.mainBodyChunk.pos = newPosition;

                isSpawned = true;

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
    private static readonly WorldCoordinate originalSpawnPoint = new WorldCoordinate(-1, 38, 13, 0);
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