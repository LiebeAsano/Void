using RWCustom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;

namespace VoidTemplate
{
    public static class ShortcutBlocker
    {
        private static readonly ConditionalWeakTable<World, WorldShortcutBlock> shortcutBlock = new();
        private static readonly ConditionalWeakTable<Player, RecentShortcutExit> recentExit = new();

        public static bool TryGetShortcutBlock(this World world, out WorldShortcutBlock block) => shortcutBlock.TryGetValue(world, out block);

        public static void CreateShortcutBlock(this World world, WorldShortcutBlock block) => shortcutBlock.Add(world, block);

        private static RecentShortcutExit GetRecentExit(this Player player) => recentExit.GetOrCreateValue(player);

        public static void Hook()
        {
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.ShortcutHelper.Update += ShortcutHelper_Update;
            On.Room.BlinkShortCut += Room_BlinkShortCut;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
        }

        private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
        {
            orig(self);

            if (!OptionAccessors.ShortcutBlocker)
            {
                if (!self.world.TryGetShortcutBlock(out var block))
                {
                    block = new WorldShortcutBlock();
                    self.world.CreateShortcutBlock(block);
                }

                for (int rm = 0; rm < self.world.abstractRooms.Length; rm++)
                {
                    AbstractRoom room = self.world.abstractRooms[rm];

                    if (room == self.world.offScreenDen)
                        continue;

                    for (int cnct = 0; cnct < room.connections.Length; cnct++)
                    {
                        if (block.GetBlockedShortcut(room, cnct) == null && room.connections[cnct] > -1)
                        {
                            block.blockedShortcuts.Add(new WorldShortcutBlock.BlockedShortcut(room, cnct));
                        }
                    }
                }
            }
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
            List<IntVector2> temporarilyUnlocked = null;

            if (self.room != null && self.room.game != null && self.room.game.Players != null)
            {
                for (int i = 0; i < self.room.game.Players.Count; i++)
                {
                    AbstractCreature absPlayer = self.room.game.Players[i];
                    if (absPlayer == null || absPlayer.realizedCreature == null)
                        continue;

                    if (absPlayer.realizedCreature is not Player player || player.room != self.room || !player.IsVoid())
                        continue;

                    RecentShortcutExit exit = player.GetRecentExit();
                    if (exit.timer > 0 && exit.room == self.room)
                    {
                        if (self.room.lockedShortcuts.Contains(exit.lockedTile))
                        {
                            temporarilyUnlocked ??= [];

                            temporarilyUnlocked.Add(exit.lockedTile);
                            self.room.lockedShortcuts.Remove(exit.lockedTile);
                        }

                        player.enteringShortCut = null;
                    }
                }
            }

            orig(self, eu);

            if (self.room != null && self.room.game != null && self.room.game.Players != null)
            {
                for (int i = 0; i < self.room.game.Players.Count; i++)
                {
                    AbstractCreature absPlayer = self.room.game.Players[i];
                    if (absPlayer == null || absPlayer.realizedCreature == null)
                        continue;

                    if (absPlayer.realizedCreature is not Player player || player.room != self.room || !player.IsVoid())
                        continue;

                    RecentShortcutExit exit = player.GetRecentExit();
                    if (exit.timer > 0 && exit.room == self.room)
                    {
                        player.enteringShortCut = null;

                        Vector2 dir = exit.shortcutDir.ToVector2();
                        Vector2 target = self.room.MiddleOfTile(exit.lockedTile + exit.shortcutDir);

                        for (int c = 0; c < player.bodyChunks.Length; c++)
                        {
                            BodyChunk chunk = player.bodyChunks[c];
                            Vector2 wantedPos = target + dir * (10f + chunk.rad);

                            chunk.pos = Vector2.Lerp(chunk.pos, wantedPos, 0.5f);
                            chunk.vel = Vector2.Lerp(chunk.vel, Vector2.zero, 0.85f);
                        }
                    }
                }
            }

            if (temporarilyUnlocked != null)
            {
                for (int i = 0; i < temporarilyUnlocked.Count; i++)
                {
                    if (!self.room.lockedShortcuts.Contains(temporarilyUnlocked[i]))
                    {
                        self.room.lockedShortcuts.Add(temporarilyUnlocked[i]);
                    }
                }
            }

            if (self.room != null && self.room.game != null && self.room.game.Players != null)
            {
                for (int i = 0; i < self.room.game.Players.Count; i++)
                {
                    AbstractCreature absPlayer = self.room.game.Players[i];
                    if (absPlayer == null || absPlayer.realizedCreature == null)
                        continue;

                    if (absPlayer.realizedCreature is not Player player || !player.IsVoid())
                        continue;

                    RecentShortcutExit exit = player.GetRecentExit();

                    if (exit.timer > 0)
                    {
                        exit.timer--;
                        if (exit.timer <= 0)
                        {
                            exit.room = null;
                        }
                    }

                    if (exit.countDebounce > 0)
                    {
                        exit.countDebounce--;
                        if (exit.countDebounce <= 0)
                        {
                            exit.lastCountedRoom = null;
                            exit.lastCountedNode = -1;
                        }
                    }
                }
            }

            if (self.room.world.TryGetShortcutBlock(out var block))
            {
                for (int pusher = 0; pusher < self.pushers.Count; pusher++)
                {
                    ShortcutHelper.ShortcutPusher currentPusher = self.pushers[pusher];
                    ShortcutData data = self.room.shortcutData(currentPusher.shortCutPos);

                    if (block.RoomAndNodeBlocked(self.room.abstractRoom, data.destNode))
                    {
                        for (int creature = 0; creature < self.room.abstractRoom.creatures.Count; creature++)
                        {
                            Creature crit = self.room.abstractRoom.creatures[creature].realizedCreature;
                            if (crit != null && crit is not Player)
                            {
                                if (crit.enteringShortCut != null && crit.enteringShortCut.Value == currentPusher.shortCutPos)
                                {
                                    crit.enteringShortCut = null;
                                }

                                for (int chunk = 0; chunk < crit.bodyChunks.Length; chunk++)
                                {
                                    float num3 = 10f + crit.bodyChunks[chunk].rad;

                                    if (crit.bodyChunks[chunk].pos.y > currentPusher.pushPos.y - num3 &&
                                        crit.bodyChunks[chunk].pos.y < currentPusher.pushPos.y + num3 &&
                                        crit.bodyChunks[chunk].pos.x > currentPusher.pushPos.x - num3 &&
                                        crit.bodyChunks[chunk].pos.x < currentPusher.pushPos.x + num3)
                                    {
                                        if (currentPusher.shortcutDir.x != 0)
                                        {
                                            float push = currentPusher.pushPos.x + num3 * currentPusher.shortcutDir.x - crit.bodyChunks[chunk].pos.x;
                                            crit.bodyChunks[chunk].vel.x += push;
                                            crit.bodyChunks[chunk].pos.x += push;
                                        }
                                        else
                                        {
                                            float push = currentPusher.pushPos.y + num3 * currentPusher.shortcutDir.y - crit.bodyChunks[chunk].pos.y;
                                            crit.bodyChunks[chunk].vel.y += push;
                                            crit.bodyChunks[chunk].pos.y += push;
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
                    WorldShortcutBlock.BlockedShortcut shortcut = block.blockedShortcuts[i];
                    if (shortcut.passCount == 4 || shortcut.blockTime > 0)
                    {
                        int index = -1;

                        if (self.room == shortcut.room1.realizedRoom)
                            index = self.entraceSpriteToRoomExitIndex.IndexfOf(shortcut.node1);
                        else if (self.room == shortcut.room2.realizedRoom)
                            index = self.entraceSpriteToRoomExitIndex.IndexfOf(shortcut.node2);

                        if (index > -1)
                        {
                            float lerp = Mathf.Sin(shortcut.signalCycle * 2f * Mathf.PI);

                            FSprite sprite1 = self.entranceSprites[index, 0];
                            if (sprite1 != null)
                                sprite1.color = (shortcut.passCount == 4) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                            FSprite sprite2 = self.entranceSprites[index, 1];
                            if (sprite2 != null)
                                sprite2.color = (shortcut.passCount == 4) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
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
                    WorldShortcutBlock.BlockedShortcut shortcut = block.blockedShortcuts[i];

                    if (shortcut.passCount == 4)
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
                    else if (shortcut.passCount > 0 && shortcut.passCountResetTimer > 0)
                    {
                        shortcut.passCountResetTimer--;

                        if (shortcut.passCountResetTimer <= 0)
                        {
                            shortcut.ResetPassCount();
                        }
                    }
                }
            }
        }

        private static void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            Player voidPlayer = self as Player;
            WorldShortcutBlock.BlockedShortcut hitBlock = null;
            int exitNode = -1;
            IntVector2 lockedTile = default;
            IntVector2 shortcutDir = default;

            if (voidPlayer != null &&
                voidPlayer.IsVoid() &&
                self.abstractCreature != null &&
                self.abstractCreature.world != null &&
                self.abstractCreature.world.TryGetShortcutBlock(out var block) &&
                TryResolveExitNode(newRoom, pos, out exitNode))
            {
                hitBlock = block.GetBlockedShortcut(newRoom.abstractRoom, exitNode);

                if (hitBlock != null)
                {
                    ShortcutData exitShortcut = newRoom.ShortcutLeadingToNode(exitNode);
                    lockedTile = exitShortcut.StartTile;
                    shortcutDir = newRoom.ShorcutEntranceHoleDirection(lockedTile);
                }
            }

            orig(self, pos, newRoom, spitOutAllSticks);

            if (voidPlayer == null || !voidPlayer.IsVoid() || hitBlock == null || exitNode < 0)
                return;

            RecentShortcutExit exit = voidPlayer.GetRecentExit();

            bool sameExitRecently =
                exit.countDebounce > 0 &&
                exit.lastCountedRoom == newRoom.abstractRoom &&
                exit.lastCountedNode == exitNode;

            if (!sameExitRecently)
            {
                hitBlock.RegisterPass();

                if (hitBlock.passCount > 4)
                {
                    hitBlock.Block();
                }

                exit.lastCountedRoom = newRoom.abstractRoom;
                exit.lastCountedNode = exitNode;
                exit.countDebounce = 10;
            }

            exit.room = newRoom;
            exit.lockedTile = lockedTile;
            exit.shortcutDir = shortcutDir;
            exit.timer = 20;

            voidPlayer.enteringShortCut = null;

            for (int i = 0; i < voidPlayer.bodyChunks.Length; i++)
            {
                voidPlayer.bodyChunks[i].vel = Vector2.zero;
            }
        }

        private static bool TryResolveExitNode(Room room, IntVector2 pos, out int node)
        {
            node = -1;

            if (room == null)
                return false;

            if (room.shortcuts != null)
            {
                for (int i = 0; i < room.shortcuts.Length; i++)
                {
                    ShortcutData shortcut = room.shortcuts[i];
                    if ((shortcut.shortCutType == ShortcutData.Type.Normal || shortcut.shortCutType == ShortcutData.Type.RoomExit) &&
                        SameTile(shortcut.StartTile, pos) &&
                        shortcut.destNode >= 0)
                    {
                        node = shortcut.destNode;
                        return true;
                    }
                }
            }

            ShortcutData fallback = room.shortcutData(pos);
            if (fallback.destNode >= 0)
            {
                node = fallback.destNode;
                return true;
            }

            return false;
        }

        private static bool SameTile(IntVector2 a, IntVector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        private sealed class RecentShortcutExit
        {
            public Room room;
            public IntVector2 lockedTile;
            public IntVector2 shortcutDir;
            public int timer;

            public AbstractRoom lastCountedRoom;
            public int lastCountedNode = -1;
            public int countDebounce;
        }

        public class WorldShortcutBlock
        {
            public List<BlockedShortcut> blockedShortcuts = [];

            public bool RoomAndNodeBlocked(AbstractRoom room, int node)
            {
                if (node <= -1)
                    return false;

                BlockedShortcut blockedShortcut = GetBlockedShortcut(room, node);
                if (blockedShortcut != null)
                    return blockedShortcut.blockTime > 0;

                return false;
            }

            public BlockedShortcut GetBlockedShortcut(AbstractRoom room, int node)
            {
                for (int i = 0; i < blockedShortcuts.Count; i++)
                {
                    if (blockedShortcuts[i].CompareRoomAndNode(room, node))
                        return blockedShortcuts[i];
                }

                return null;
            }

            public class BlockedShortcut
            {
                public readonly AbstractRoom room1;
                public readonly AbstractRoom room2;

                public int node1;
                public int node2;

                public int blockTime;
                public int passCount;
                public int passCountResetTimer;
                public float signalCycle;

                public const int PassCountResetDelay = 1200;

                public BlockedShortcut(AbstractRoom fromRoom, int toRoom2Node)
                {
                    room1 = fromRoom;
                    room2 = fromRoom.world.GetAbstractRoom(fromRoom.connections[toRoom2Node]);
                    node1 = toRoom2Node;
                    node2 = room2.ExitIndex(fromRoom.index);
                }

                public BlockedShortcut(AbstractRoom room1, AbstractRoom room2)
                {
                    this.room1 = room1;
                    this.room2 = room2;
                    node1 = room1.ExitIndex(room2.index);
                    node2 = room2.ExitIndex(room1.index);
                }

                public void RegisterPass()
                {
                    passCount++;
                    passCountResetTimer = PassCountResetDelay;
                }

                public void ResetPassCount()
                {
                    passCount = 0;
                    passCountResetTimer = 0;
                    signalCycle = 0f;
                }

                public void Block()
                {
                    blockTime = Random.Range(400, 801);

                    if (room1.realizedRoom is Room rRoom)
                    {
                        IntVector2 tile1 = rRoom.ShortcutLeadingToNode(node1).StartTile;
                        if (!rRoom.lockedShortcuts.Contains(tile1))
                        {
                            rRoom.lockedShortcuts.Add(tile1);
                        }
                    }

                    if (room2.realizedRoom is Room rRoom2)
                    {
                        IntVector2 tile2 = rRoom2.ShortcutLeadingToNode(node2).StartTile;
                        if (!rRoom2.lockedShortcuts.Contains(tile2))
                        {
                            rRoom2.lockedShortcuts.Add(tile2);
                        }
                    }

                    passCount = 0;
                    passCountResetTimer = 0;
                    signalCycle = 0f;
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

                public bool ComareRooms(AbstractRoom r1, AbstractRoom r2)
                {
                    return (room1 == r1 && room2 == r2) || (room2 == r1 && room1 == r2);
                }
            }
        }
    }
}