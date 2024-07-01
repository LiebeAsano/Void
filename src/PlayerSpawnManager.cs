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
                Debug.Log("[TheVoid] KarmaCapTrigger initialized.");
            }

            int currentRoomIndex = __instance.abstractCreature.pos.room;

            // Логгирование состояния игрока
            //LogPlayerState(__instance);

            // Логгирование ввода игрока
            //LogPlayerInput(__instance);

            // Если игрок в целевой комнате
            if (currentRoomIndex == NewSpawnPoint.room)
            {
                // Обновляем позицию игрока
                __instance.abstractCreature.pos = NewSpawnPoint;
                Vector2 newPosition = __instance.room.MiddleOfTile(NewSpawnPoint.x, NewSpawnPoint.y);
                __instance.firstChunk.pos = newPosition;
                __instance.mainBodyChunk.pos = newPosition;

                Plugin.isSpawned = true;

                // Автоматически активируем анимацию стояния
                __instance.animation = Player.AnimationIndex.StandUp;
                //Debug.Log("[TheVoid] Player automatically set to StandUp animation.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TheVoid] Exception in Patch_Player_Update: {ex}");
        }
    }

    // Метод для логгирования состояния игрока
    static void LogPlayerState(Player player)
    {
        Debug.Log($"[TheVoid] Player State - Room: {player.abstractCreature.pos.room}, X: {player.abstractCreature.pos.x}, Y: {player.abstractCreature.pos.y}");
        Debug.Log($"[TheVoid] Player Position - X: {player.firstChunk.pos.x}, Y: {player.firstChunk.pos.y}");
        Debug.Log($"[TheVoid] Player Animation: {player.animation}");
    }

    // Метод для логгирования ввода игрока
    static void LogPlayerInput(Player player)
    {
        // Проверка на наличие и доступность свойства input.
        if (player.input != null && player.input.Length > 0)
        {
            foreach (var input in player.input)
            {
                Debug.Log($"[TheVoid] Player Input Package: {input.ToString()}");
            }
        }
        else
        {
            Debug.LogWarning("[TheVoid] Player input is null or empty.");
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