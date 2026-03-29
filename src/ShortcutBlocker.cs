using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate
{
    public static class ShortcutBlocker
    {
        private static ConditionalWeakTable<World, WorldShortcutBlock> shortcutBlock = new();

        public static bool TryGetShortcutBlock(this World world, out WorldShortcutBlock block) => shortcutBlock.TryGetValue(world, out block);

        public static void CreateShortcutBlock(this World world, WorldShortcutBlock block) => shortcutBlock.Add(world, block);

        public static void Hook()
        {
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.ShortcutHelper.Update += ShortcutHelper_Update;
            On.Room.BlinkShortCut += Room_BlinkShortCut;
        }

        private static void Room_BlinkShortCut(On.Room.orig_BlinkShortCut orig, Room self, int shortcut, int secondary, float blinkFac)
        {
            if (self.world.TryGetShortcutBlock(out var block))
            {
                if (block.RoomAndNodeBlocked(self.abstractRoom, shortcut))
                {
                    shortcut = -1;
                }
                if (block.RoomAndNodeBlocked(self.abstractRoom, secondary))
                {
                    secondary = -1;
                }
            }
            orig(self, shortcut, secondary, blinkFac);
        }

        private static void ShortcutHelper_Update(On.ShortcutHelper.orig_Update orig, ShortcutHelper self, bool eu)
        {
            orig(self, eu);
            if (self.room.world.TryGetShortcutBlock(out var block))
            {
                for (int pusher = 0; pusher < self.pushers.Count; pusher++)
                {
                    if (block.RoomAndNodeBlocked(self.room.abstractRoom, self.room.shortcutData(self.pushers[pusher].shortCutPos).destNode))
                    {
                        for (int creature = 0; creature < self.room.abstractRoom.creatures.Count; creature++)
                        {
                            if (self.room.abstractRoom.creatures[creature].realizedCreature is Creature crit && crit is not Player)
                            {
                                if (crit.enteringShortCut != null && crit.enteringShortCut.Value == self.pushers[pusher].shortCutPos)
                                {
                                    crit.enteringShortCut = null;
                                }
                                for (int chunk = 0; chunk < crit.bodyChunks.Length; chunk++)
                                {
                                    float num3 = 10f + crit.bodyChunks[chunk].rad;

                                    if (crit.bodyChunks[chunk].pos.y > self.pushers[pusher].pushPos.y - num3 && crit.bodyChunks[chunk].pos.y < self.pushers[pusher].pushPos.y + num3 &&
                                        crit.bodyChunks[chunk].pos.x > self.pushers[pusher].pushPos.x - num3 && crit.bodyChunks[chunk].pos.x < self.pushers[pusher].pushPos.x + num3)
                                    {
                                        if (self.pushers[pusher].shortcutDir.x != 0)
                                        {
                                            crit.bodyChunks[chunk].vel.x += self.pushers[pusher].pushPos.x + num3 * self.pushers[pusher].shortcutDir.x - crit.bodyChunks[chunk].pos.x;
                                            crit.bodyChunks[chunk].pos.x += self.pushers[pusher].pushPos.x + num3 * self.pushers[pusher].shortcutDir.x - crit.bodyChunks[chunk].pos.x;
                                        }
                                        else
                                        {
                                            crit.bodyChunks[chunk].vel.y += self.pushers[pusher].pushPos.y + num3 * self.pushers[pusher].shortcutDir.y - crit.bodyChunks[chunk].pos.y;
                                            crit.bodyChunks[chunk].pos.y += self.pushers[pusher].pushPos.y + num3 * self.pushers[pusher].shortcutDir.y - crit.bodyChunks[chunk].pos.y;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);
            if (self.room != null && self.room.world.TryGetShortcutBlock(out var block))
            {
                for (int i = 0; i < block.blockedShortcuts.Count; i++)
                {
                    var shortcut = block.blockedShortcuts[i];
                    if (shortcut.passCount == 2 || shortcut.blockTime > 0)
                    {
                        int index = -1;

                        if (self.room == shortcut.room1.realizedRoom)
                            index = self.entraceSpriteToRoomExitIndex.IndexfOf(shortcut.node1);
                        else if (self.room == shortcut.room2.realizedRoom)
                            index = self.entraceSpriteToRoomExitIndex.IndexfOf(shortcut.node2);

                        if (index > -1)
                        {
                            float lerp = Mathf.Sin(shortcut.signalCycle * 2 * Mathf.PI);

                            FSprite sprite1 = self.entranceSprites[index, 0];
                            if (sprite1 != null)
                                sprite1.color = (shortcut.passCount == 2) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                            FSprite sprite2 = self.entranceSprites[index, 1];
                            if (sprite2 != null)
                                sprite2.color = (shortcut.passCount == 2) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
                        }
                    }
                }
            }
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (!self.GamePaused && self.processActive && self.world.TryGetShortcutBlock(out var block))
            {
                for (int i = 0; i < block.blockedShortcuts.Count; i++)
                {
                    var shortcut = block.blockedShortcuts[i];
                    if (shortcut.passCount == 2)
                    {
                        shortcut.signalCycle += 0.011111111f;
                    }
                    if (shortcut.blockTime > 0)
                    {
                        shortcut.blockTime--;

                        if (shortcut.blockTime == 0)
                        {
                            shortcut.Unlock();
                        }
                    }
                }
            }
        }

        private static void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, RWCustom.IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            if (self is Player player && player.AreVoidViy() && self.inShortcut == true && self.abstractCreature.world.TryGetShortcutBlock(out var block))
            {
                for (int i = 0; i < block.blockedShortcuts.Count; i++)
                {
                    if (block.blockedShortcuts[i].CompareRoomAndNode(newRoom.abstractRoom, newRoom.shortcutData(pos).destNode))
                    {
                        block.blockedShortcuts[i].passCount++;
                        if (block.blockedShortcuts[i].passCount > 2)
                        {
                            block.blockedShortcuts[i].Block();
                        }
                        break;
                    }
                }
            }
            orig(self, pos, newRoom, spitOutAllSticks);
        }

        public class WorldShortcutBlock
        {
            public List<BlockedShortcut> blockedShortcuts = [];

            public bool RoomAndNodeBlocked(AbstractRoom room, int node)
            {
                if (node <= -1) return false;

                for (int i = 0; i < blockedShortcuts.Count; i++)
                {
                    if (blockedShortcuts[i].CompareRoomAndNode(room, node))
                    {
                        return blockedShortcuts[i].blockTime > 0;
                    }
                }
                return false;
            }

            public class BlockedShortcut
            {
                public readonly AbstractRoom room1;

                public readonly AbstractRoom room2;

                public int node1;

                public int node2;

                public int blockTime;

                public int passCount;

                public float signalCycle;

                public BlockedShortcut(AbstractRoom fromRoom, int toRoom2Node)
                {
                    room1 = fromRoom;
                    room2 = fromRoom.world.GetAbstractRoom(fromRoom.connections[toRoom2Node]);
                    node1 = toRoom2Node;
                    node2 = room2.ExitIndex(fromRoom.index);
                }

                public void Block()
                {
                    blockTime = 400;

                    if (room1.realizedRoom is Room rRoom && !rRoom.lockedShortcuts.Contains(rRoom.ShortcutLeadingToNode(node1).StartTile))
                    {
                        rRoom.lockedShortcuts.Add(rRoom.ShortcutLeadingToNode(node1).StartTile);
                    }
                    if (room2.realizedRoom is Room rRoom2 && !rRoom2.lockedShortcuts.Contains(rRoom2.ShortcutLeadingToNode(node2).StartTile))
                    {
                        rRoom2.lockedShortcuts.Add(rRoom2.ShortcutLeadingToNode(node2).StartTile);
                    }
                    passCount = 0;
                    signalCycle = 0;
                }

                public void Unlock()
                {
                    room1.realizedRoom?.lockedShortcuts.Remove(room1.realizedRoom.ShortcutLeadingToNode(node1).StartTile);
                    room2.realizedRoom?.lockedShortcuts.Remove(room2.realizedRoom.ShortcutLeadingToNode(node2).StartTile);
                }

                public bool CompareRoomAndNode(AbstractRoom room, int node)
                {
                    return (room1 == room && node1 == node) || (room2 == room && node2 == node);
                }
            }
        }
    }
}
