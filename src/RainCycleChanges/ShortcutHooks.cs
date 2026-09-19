using RWCustom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoidTemplate.RainCycleChanges
{
    public class ShortcutHooks
    {
        private sealed class RoomState
        {
            public readonly HashSet<IntVector2> rainLockedShortcuts = [];
        }

        private sealed class WorldState
        {
            public readonly HashSet<int> floodShelters = [];
        }

        private static readonly ConditionalWeakTable<Room, RoomState> roomStates = new();
        private static readonly ConditionalWeakTable<World, WorldState> worldStates = new();

        public static void Hook()
        {
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.Room.Update += Room_Update;
        }

        private static bool HasFloodWeather(Room room)
        {
            if (room?.roomSettings == null)
                return false;

            RoomRain.DangerType danger = room.roomSettings.DangerType;

            return danger == RoomRain.DangerType.Flood
                || danger == RoomRain.DangerType.FloodAndRain;
        }

        private static bool IsWorkingShelter(AbstractRoom room)
        {
            return room != null
                && room.shelter
                && !room.world.brokenShelters[room.shelterIndex];
        }

        private static AbstractRoom GetLeadingRoom(Room room, ShortcutData shortcut)
        {
            if (shortcut.destNode < 0 || shortcut.destNode >= room.abstractRoom.connections.Length)
                return null;

            return room.world.GetAbstractRoom(room.abstractRoom.connections[shortcut.destNode]);
        }

        private static void RegisterFloodShelters(Room room, WorldState state)
        {
            if (room.abstractRoom.shelter)
            {
                for (int i = 0; i < room.abstractRoom.connections.Length; i++)
                {
                    AbstractRoom connectedRoom = room.world.GetAbstractRoom(room.abstractRoom.connections[i]);

                    if (connectedRoom?.realizedRoom != null && HasFloodWeather(connectedRoom.realizedRoom))
                    {
                        state.floodShelters.Add(room.abstractRoom.index);
                        return;
                    }
                }

                return;
            }

            if (!HasFloodWeather(room))
                return;

            for (int i = 0; i < room.abstractRoom.connections.Length; i++)
            {
                AbstractRoom connectedRoom = room.world.GetAbstractRoom(room.abstractRoom.connections[i]);

                if (IsWorkingShelter(connectedRoom))
                    state.floodShelters.Add(connectedRoom.index);
            }
        }

        private static void SetRainLock(Room room, RoomState state, IntVector2 tile, bool locked)
        {
            if (locked)
            {
                if (!room.lockedShortcuts.Contains(tile))
                {
                    room.lockedShortcuts.Add(tile);
                    state.rainLockedShortcuts.Add(tile);
                }

                return;
            }

            if (state.rainLockedShortcuts.Remove(tile))
                room.lockedShortcuts.Remove(tile);
        }

        private static void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);

            if (!self.ReadyForPlayer || self.world?.rainCycle == null)
                return;

            RoomState roomState = roomStates.GetValue(self, _ => new RoomState());
            WorldState worldState = worldStates.GetValue(self.world, _ => new WorldState());

            RegisterFloodShelters(self, worldState);

            bool timeToLock = self.world.rainCycle.GetRainCycleExt().TimeToLockShelters;

            for (int i = 0; i < self.shortcuts.Length; i++)
            {
                ShortcutData shortcut = self.shortcuts[i];

                if (shortcut.shortCutType != ShortcutData.Type.RoomExit)
                    continue;

                bool shouldLock = false;

                if (self.abstractRoom.shelter)
                {
                    shouldLock = timeToLock
                        && worldState.floodShelters.Contains(self.abstractRoom.index);
                }
                else if (timeToLock && HasFloodWeather(self))
                {
                    AbstractRoom leadingRoom = GetLeadingRoom(self, shortcut);
                    shouldLock = IsWorkingShelter(leadingRoom);
                }

                SetRainLock(self, roomState, shortcut.StartTile, shouldLock);
            }
        }

        private static void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);

            if (self.waitingForRoomToGenerateShortcuts)
                return;

            if (self.room.abstractRoom.shelter)
            {
                if (!roomStates.TryGetValue(self.room, out RoomState state) || state.rainLockedShortcuts.Count == 0)
                    return;

                for (int i = 0; i < self.room.shortcuts.Length; i++)
                {
                    if (state.rainLockedShortcuts.Contains(self.room.shortcuts[i].StartTile)
                        && self.entranceSprites[i, 0] is FSprite sprite)
                        sprite.color = Color.black;
                }

                return;
            }

            if (!self.room.world.rainCycle.GetRainCycleExt().TimeToLockShelters || !HasFloodWeather(self.room))
                return;

            for (int i = 0; i < self.entranceSprites.GetLength(0); i++)
            {
                if (self.entranceSprites[i, 0] is FSprite sprite
                    && (sprite.element.name == "ShortcutShelter"
                    || sprite.element.name == "ShortcutAShelter"))
                    sprite.color = Color.black;
            }
        }
    }
}