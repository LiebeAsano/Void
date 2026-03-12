using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.RainCycleChanges
{
    public class ShortcutHooks
    {
        public static void Hook()
        {
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.Room.Update += Room_Update;
        }

        private static void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);
            if (self.ReadyForPlayer && self.world.rainCycle.GetRainCycleExt().TimeToLockShelters)
            {
                for (int i = 0; i < self.shortcuts.Length; i++)
                {
                    if (self.shortcuts[i].shortCutType == ShortcutData.Type.RoomExit && !self.lockedShortcuts.Contains(self.shortcuts[i].StartTile))
                    {
                        AbstractRoom leadingRoom = self.world.GetAbstractRoom(self.abstractRoom.connections[self.shortcuts[i].destNode]);
                        if (leadingRoom != null && leadingRoom.shelter && !leadingRoom.world.brokenShelters[leadingRoom.shelterIndex])
                        {
                            self.lockedShortcuts.Add(self.shortcuts[i].StartTile);
                        }
                    }
                }
            }
        }

        private static void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);
            if (!self.waitingForRoomToGenerateShortcuts && self.room.world.rainCycle.GetRainCycleExt().TimeToLockShelters)
            {
                for (int i = 0; i < self.entranceSprites.GetLength(0); i++)
                {
                    if (self.entranceSprites[i, 0] is FSprite sprite && (sprite.element.name == "ShortcutShelter" || sprite.element.name == "ShortcutAShelter"))
                    {
                        sprite.color = Color.black;
                    }
                }
            }
        }
    }
}
