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
        private static readonly ConditionalWeakTable<Room, RoomTransportShortcutBlocker> roomShortcutBlock = new();

        public static bool TryGetShortcutBlock(this World world, out WorldShortcutBlock block) => shortcutBlock.TryGetValue(world, out block);

        public static void CreateShortcutBlock(this World world, WorldShortcutBlock block) => shortcutBlock.Add(world, block);

        private static RecentShortcutExit GetRecentExit(this Player player) => recentExit.GetOrCreateValue(player);

        public static RoomTransportShortcutBlocker GetRoomShortcutBlock(this Room room) => roomShortcutBlock.GetValue(room, r => new(r));

        public static void Hook()
        {
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.ShortcutHelper.Update += ShortcutHelper_Update;
            On.Room.BlinkShortCut += Room_BlinkShortCut;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
            On.Room.ShortCutsReady += Room_ShortCutsReady;
            On.Room.Update += Room_Update;
        }

        private static void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);

            var block = self.GetRoomShortcutBlock();
            for (int i = 0; i < block.blockedShorcuts.Count; i++)
            {
                block.blockedShorcuts[i].Update();
            }
        }

        private static void Room_ShortCutsReady(On.Room.orig_ShortCutsReady orig, Room self)
        {
            orig(self);

            var block = self.GetRoomShortcutBlock();
            for (int i = 0; i < self.shortcuts.Length; i++)
            {
                if (self.shortcuts[i].shortCutType == ShortcutData.Type.Normal && block.GetShortcutBlock(self.shortcuts[i].StartTile) == null)
                {
                    block.AddShortcut(self.shortcuts[i].StartTile);
                }
            }
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
                            block.blockedShortcuts.Add(new WorldShortcutBlock.BlockedRoomExit(room, cnct));
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
            orig(self, eu);

            if (self.room == null || self.room.game == null)
                return;

            bool worldHaveBlock = self.room.world.TryGetShortcutBlock(out var worldBlock);
            RoomTransportShortcutBlocker localBlocker = self.room.GetRoomShortcutBlock();

            if (self.room.game.Players != null)
            {
                for (int i = 0; i < self.room.game.Players.Count; i++)
                {
                    AbstractCreature absPlayer = self.room.game.Players[i];
                    if (absPlayer == null || absPlayer.realizedCreature is not Player player || player.room != self.room || !player.IsVoid())
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
                            exit.lastCountedWasRoomShortcut = false;
                            exit.lastCountedShortcutTile = default;
                        }
                    }

                    if (player.enteringShortCut == null)
                        continue;

                    IntVector2 entering = player.enteringShortCut.Value;
                    bool blocked = false;

                    if (localBlocker.ShortcutBlocked(entering))
                    {
                        blocked = true;
                    }
                    else if (worldHaveBlock && TryResolveExitNode(self.room, entering, out int node) && worldBlock.RoomAndNodeBlocked(self.room.abstractRoom, node))
                    {
                        blocked = true;
                    }

                    if (blocked)
                    {
                        player.enteringShortCut = null;
                        player.shortcutDelay = Mathf.Max(player.shortcutDelay, 8);
                    }
                }
            }

            for (int pusher = 0; pusher < self.pushers.Count; pusher++)
            {
                ShortcutHelper.ShortcutPusher currentPusher = self.pushers[pusher];
                ShortcutData data = self.room.shortcutData(currentPusher.shortCutPos);

                bool localBlocked = localBlocker.ShortcutBlocked(currentPusher.shortCutPos);
                bool worldBlocked = worldHaveBlock && worldBlock.RoomAndNodeBlocked(self.room.abstractRoom, data.destNode);

                if (localBlocked || worldBlocked)
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

        private static void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);

            if (self.room != null)
            {
                var roomBlock = self.room.GetRoomShortcutBlock();
                for (int i = 0; i < roomBlock.blockedShorcuts.Count; i++)
                {
                    var shortcut = roomBlock.blockedShorcuts[i];
                    if (shortcut.passCount == shortcut.maxPassCount || shortcut.blockTime > 0)
                    {
                        int index1 = self.room.shortcutsIndex.IndexfOf(shortcut.blockedShrotcut1);
                        int index2 = self.room.shortcutsIndex.IndexfOf(shortcut.blockedShrotcut2);

                        float lerp = Mathf.Sin(shortcut.signalCycle * 2f * Mathf.PI);

                        if (index1 > -1)
                        {
                            FSprite sprite1 = self.entranceSprites[index1, 0];
                            if (sprite1 != null)
                                sprite1.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                            FSprite sprite2 = self.entranceSprites[index1, 1];
                            if (sprite2 != null)
                                sprite2.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
                        }

                        if (index2 > -1)
                        {
                            FSprite sprite1 = self.entranceSprites[index2, 0];
                            if (sprite1 != null)
                                sprite1.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                            FSprite sprite2 = self.entranceSprites[index2, 1];
                            if (sprite2 != null)
                                sprite2.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
                        }
                    }
                }

                if (self.room.world.TryGetShortcutBlock(out var block))
                {
                    for (int i = 0; i < block.blockedShortcuts.Count; i++)
                    {
                        WorldShortcutBlock.BlockedRoomExit shortcut = block.blockedShortcuts[i];
                        if (shortcut.passCount == shortcut.maxPassCount || shortcut.blockTime > 0)
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
                                    sprite1.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                                FSprite sprite2 = self.entranceSprites[index, 1];
                                if (sprite2 != null)
                                    sprite2.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
                            }
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
                    block.blockedShortcuts[i].Update();
                }
            }
        }

        private static void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            Player voidPlayer = self as Player;

            RoomTransportShortcutBlocker.RoomShortcutBlock localHitBlock = null;
            IntVector2 localLockedTile = default;
            IntVector2 localShortcutDir = default;

            WorldShortcutBlock.BlockedRoomExit worldHitBlock = null;
            int exitNode = -1;
            IntVector2 worldLockedTile = default;
            IntVector2 worldShortcutDir = default;

            if (voidPlayer != null && voidPlayer.IsVoid() && newRoom != null)
            {
                localHitBlock = newRoom.GetRoomShortcutBlock().GetShortcutBlock(pos);

                if (localHitBlock != null)
                {
                    localLockedTile = pos;
                    localShortcutDir = newRoom.ShorcutEntranceHoleDirection(localLockedTile);
                }

                if (self.abstractCreature != null &&
                    self.abstractCreature.world != null &&
                    self.abstractCreature.world.TryGetShortcutBlock(out var worldBlock) &&
                    TryResolveExitNode(newRoom, pos, out exitNode))
                {
                    worldHitBlock = worldBlock.GetBlockedShortcut(newRoom.abstractRoom, exitNode);

                    if (worldHitBlock != null)
                    {
                        ShortcutData exitShortcut = newRoom.ShortcutLeadingToNode(exitNode);
                        worldLockedTile = exitShortcut.StartTile;
                        worldShortcutDir = newRoom.ShorcutEntranceHoleDirection(worldLockedTile);
                    }
                }
            }

            orig(self, pos, newRoom, spitOutAllSticks);

            if (voidPlayer == null || !voidPlayer.IsVoid() || newRoom == null)
                return;

            RecentShortcutExit exit = voidPlayer.GetRecentExit();

            if (localHitBlock != null)
            {
                bool sameLocalExitRecently =
                    exit.countDebounce > 0 &&
                    exit.lastCountedRoom == newRoom.abstractRoom &&
                    exit.lastCountedWasRoomShortcut &&
                    SameTile(exit.lastCountedShortcutTile, localLockedTile);

                if (!sameLocalExitRecently)
                {
                    localHitBlock.RegisterPass();

                    if (localHitBlock.passCount > localHitBlock.maxPassCount)
                    {
                        localHitBlock.Block();
                    }

                    exit.lastCountedRoom = newRoom.abstractRoom;
                    exit.lastCountedNode = -1;
                    exit.lastCountedWasRoomShortcut = true;
                    exit.lastCountedShortcutTile = localLockedTile;
                    exit.countDebounce = 10;
                }

                exit.room = newRoom;
                exit.lockedTile = localLockedTile;
                exit.shortcutDir = localShortcutDir;
                exit.timer = 20;

                voidPlayer.enteringShortCut = null;
                voidPlayer.shortcutDelay = Mathf.Max(voidPlayer.shortcutDelay, 8);
                return;
            }

            if (worldHitBlock == null || exitNode < 0)
                return;

            bool sameWorldExitRecently =
                exit.countDebounce > 0 &&
                exit.lastCountedRoom == newRoom.abstractRoom &&
                !exit.lastCountedWasRoomShortcut &&
                exit.lastCountedNode == exitNode;

            if (!sameWorldExitRecently)
            {
                worldHitBlock.RegisterPass();

                if (worldHitBlock.passCount > worldHitBlock.maxPassCount)
                {
                    worldHitBlock.Block();
                }

                exit.lastCountedRoom = newRoom.abstractRoom;
                exit.lastCountedNode = exitNode;
                exit.lastCountedWasRoomShortcut = false;
                exit.lastCountedShortcutTile = default;
                exit.countDebounce = 10;
            }

            exit.room = newRoom;
            exit.lockedTile = worldLockedTile;
            exit.shortcutDir = worldShortcutDir;
            exit.timer = 20;

            voidPlayer.enteringShortCut = null;
            voidPlayer.shortcutDelay = Mathf.Max(voidPlayer.shortcutDelay, 8);
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

            public bool lastCountedWasRoomShortcut;
            public IntVector2 lastCountedShortcutTile;
        }

        public abstract class VoidShortcutBlock
        {
            public const int PassCountResetDelay = 1200;

            public int blockTime;
            public int maxPassCount = 4;
            public bool maxRegister = false;
            public int passCount;
            public int passCountResetTimer;
            public float signalCycle;

            public void Update()
            {
                if (passCount == maxPassCount)
                {
                    signalCycle += 0.011111111f;
                }

                if (blockTime > 0)
                {
                    blockTime--;

                    if (blockTime == 0)
                    {
                        Unlock();
                    }
                }
                else if (passCount > 0 && passCountResetTimer > 0)
                {
                    passCountResetTimer--;

                    if (passCountResetTimer <= 0)
                    {
                        ResetPassCount();
                    }
                }
            }

            public void RegisterPass()
            {
                if (!maxRegister)
                {
                    maxPassCount = Random.Range(3, 8);
                    maxRegister = true;
                }

                passCount++;
                passCountResetTimer = PassCountResetDelay;
            }

            public void ResetPassCount()
            {
                maxRegister = false;
                passCount = 0;
                passCountResetTimer = 0;
                signalCycle = 0f;
            }

            public abstract void Block();
            public abstract void Unlock();
        }

        public class RoomTransportShortcutBlocker(Room room)
        {
            public readonly Room room = room;
            public List<RoomShortcutBlock> blockedShorcuts = [];

            public RoomShortcutBlock GetShortcutBlock(IntVector2 shortcut)
            {
                for (int i = 0; i < blockedShorcuts.Count; i++)
                {
                    if (blockedShorcuts[i].CompareShortcut(shortcut))
                    {
                        return blockedShorcuts[i];
                    }
                }
                return null;
            }

            public bool ShortcutBlocked(IntVector2 shortcut)
            {
                var block = GetShortcutBlock(shortcut);
                if (block != null) return block.blockTime > 0;
                return false;
            }

            public void AddShortcut(IntVector2 shortcut) => blockedShorcuts.Add(new(room, shortcut));

            public class RoomShortcutBlock : VoidShortcutBlock
            {
                public readonly Room room;
                public readonly IntVector2 blockedShrotcut1;
                public readonly IntVector2 blockedShrotcut2;

                public RoomShortcutBlock(Room room, IntVector2 roomShortcut)
                {
                    this.room = room;
                    blockedShrotcut1 = roomShortcut;
                    var sData = room.shortcutData(roomShortcut);
                    blockedShrotcut2 = sData.path[sData.path.Length - 1];
                }

                public bool CompareShortcut(IntVector2 transportShortcut)
                {
                    return blockedShrotcut1 == transportShortcut || blockedShrotcut2 == transportShortcut;
                }

                public override void Block()
                {
                    blockTime = Random.Range(400, 801);
                    ResetPassCount();
                }

                public override void Unlock()
                {
                }
            }
        }

        public class WorldShortcutBlock
        {
            public List<BlockedRoomExit> blockedShortcuts = [];

            public bool RoomAndNodeBlocked(AbstractRoom room, int node)
            {
                if (node <= -1)
                    return false;

                BlockedRoomExit blockedShortcut = GetBlockedShortcut(room, node);
                if (blockedShortcut != null)
                    return blockedShortcut.blockTime > 0;

                return false;
            }

            public BlockedRoomExit GetBlockedShortcut(AbstractRoom room, int node)
            {
                for (int i = 0; i < blockedShortcuts.Count; i++)
                {
                    if (blockedShortcuts[i].CompareRoomAndNode(room, node))
                        return blockedShortcuts[i];
                }

                return null;
            }

            public class BlockedRoomExit : VoidShortcutBlock
            {
                public readonly AbstractRoom room1;
                public readonly AbstractRoom room2;

                public int node1;
                public int node2;

                public BlockedRoomExit(AbstractRoom fromRoom, int toRoom2Node)
                {
                    room1 = fromRoom;
                    room2 = fromRoom.world.GetAbstractRoom(fromRoom.connections[toRoom2Node]);
                    node1 = toRoom2Node;
                    node2 = room2.ExitIndex(fromRoom.index);
                }

                public override void Block()
                {
                    blockTime = Random.Range(400, 801);
                    ResetPassCount();
                }

                public override void Unlock()
                {
                }

                public bool CompareRoomAndNode(AbstractRoom room, int node)
                {
                    return (room1 == room && node1 == node) || (room2 == room && node2 == node);
                }
            }
        }
    }
}