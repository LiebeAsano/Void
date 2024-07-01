using System;
using VoidTemplate;
using UnityEngine;
using HarmonyLib;
using TheVoid;

[HarmonyPatch(typeof(Player), nameof(Player.Update))]
public class Patch_Player_Update
{
    private static int targetRoomID = -1;
    private static readonly WorldCoordinate originalSpawnPoint = new WorldCoordinate(-1, 38, 13, 0);
    private static bool karmaTriggerInitialized = false;

    static WorldCoordinate NewSpawnPoint
    {
        get
        {
            if (targetRoomID == -1)
            {
                throw new Exception("[TheVoid] Target room ID is not initialized!");
            }
            return new WorldCoordinate(targetRoomID, originalSpawnPoint.x, originalSpawnPoint.y, originalSpawnPoint.abstractNode);
        }
    }

    static void InitializeTargetRoomID(Room room)
    {
        if (targetRoomID == -1)
        {
            AbstractRoom targetRoom = room.world.GetAbstractRoom("SB_A14");
            if (targetRoom == null)
            {
                throw new Exception($"[TheVoid] Room 'SB_A14' does not exist.");
            }
            targetRoomID = targetRoom.index;
        }
    }

    static void Postfix(Player __instance)
    {
        if (Plugin.isSpawned) return;

        try
        {
            if (__instance == null || __instance.room == null)
            {
                Debug.LogWarning("[TheVoid] Player instance or room is null.");
                return;
            }

            Room playerRoom = __instance.room;
            InitializeTargetRoomID(playerRoom);

            if (!karmaTriggerInitialized)
            {
                KarmaCapCheck.Init(playerRoom, __instance);
                karmaTriggerInitialized = true;
            }

            int currentRoomIndex = __instance.abstractCreature.pos.room;

            if (currentRoomIndex == NewSpawnPoint.room)
            {
                __instance.abstractCreature.pos = NewSpawnPoint;
                Vector2 newPosition = __instance.room.MiddleOfTile(NewSpawnPoint.x, NewSpawnPoint.y);
                __instance.firstChunk.pos = newPosition;
                __instance.mainBodyChunk.pos = newPosition;

                Plugin.isSpawned = true;

                __instance.animation = Player.AnimationIndex.StandUp;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TheVoid] Exception in Patch_Player_Update: {ex}");
        }
    }
}

[HarmonyPatch(typeof(StoryGameSession), nameof(StoryGameSession.AddPlayer))]
class Patch_StoryGameSession_AddPlayer
{
    static void Prefix()
    {
        Plugin.isSpawned = false;
        Debug.Log("[TheVoid] Player added, isSpawned reset.");
    }
}