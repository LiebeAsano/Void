using RWCustom;
using System;
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

        public static RoomTransportShortcutBlocker GetRoomShortcutBlock(this Room room)
        {
            return roomShortcutBlock.GetValue(room, r => new RoomTransportShortcutBlocker(r));
        }

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

            RoomTransportShortcutBlocker block = self.GetRoomShortcutBlock();
            for (int i = 0; i < block.blockedShorcuts.Count; i++)
            {
                block.blockedShorcuts[i].Update();
            }
        }

        private static void Room_ShortCutsReady(On.Room.orig_ShortCutsReady orig, Room self)
        {
            orig(self);

            RoomTransportShortcutBlocker block = self.GetRoomShortcutBlock();
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

            if (OptionAccessors.ShortcutBlocker || self.world == null)
                return;

            if (self.abstractRooms == null || self.abstractRooms.Count == 0)
                return;

            if (!self.world.TryGetShortcutBlock(out var block))
            {
                block = new WorldShortcutBlock();
                self.world.CreateShortcutBlock(block);
            }

            for (int rm = 0; rm < self.abstractRooms.Count; rm++)
            {
                AbstractRoom room = self.abstractRooms[rm];

                if (room == null || room == self.world.offScreenDen)
                    continue;

                if (room.connections == null)
                    continue;

                for (int cnct = 0; cnct < room.connections.Length; cnct++)
                {
                    if (room.connections[cnct] <= -1)
                        continue;

                    if (room.world == null)
                        continue;

                    AbstractRoom targetRoom = self.world.GetAbstractRoom(room.connections[cnct]);
                    if (targetRoom == null)
                        continue;

                    if (block.GetBlockedShortcut(room, cnct) == null)
                    {
                        block.blockedShortcuts.Add(new WorldShortcutBlock.BlockedRoomExit(room, cnct));

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
            if (self.room == null || self.room.game == null || self.room.game.Players == null)
            {
                orig(self, eu);
                return;
            }

            List<RemovedVoidPlayer> removedVoidPlayers = [];

            for (int i = self.room.game.Players.Count - 1; i >= 0; i--)
            {
                AbstractCreature absPlayer = self.room.game.Players[i];

                if (absPlayer == null || absPlayer.realizedCreature == null)
                    continue;

                if (absPlayer.realizedCreature is not Player player || player.room != self.room || !player.IsVoid())
                    continue;

                removedVoidPlayers.Add(new RemovedVoidPlayer(player, absPlayer, i));
                self.room.game.Players.RemoveAt(i);
            }

            orig(self, eu);

            for (int i = removedVoidPlayers.Count - 1; i >= 0; i--)
            {
                RemovedVoidPlayer entry = removedVoidPlayers[i];
                int insertIndex = Mathf.Clamp(entry.index, 0, self.room.game.Players.Count);

                if (!self.room.game.Players.Contains(entry.abstractCreature))
                    self.room.game.Players.Insert(insertIndex, entry.abstractCreature);
            }

            RoomTransportShortcutBlocker localBlocker = self.room.GetRoomShortcutBlock();
            WorldShortcutBlock worldBlock = null;
            bool worldHaveBlock = self.room.world != null && self.room.world.TryGetShortcutBlock(out worldBlock);

            for (int i = 0; i < removedVoidPlayers.Count; i++)
            {
                Player player = removedVoidPlayers[i].player;

                if (player == null || player.room != self.room || !player.IsVoid())
                    continue;

                UpdateVoidRecentExit(player);

                UpdateVoidPlayerAgainstVanillaLocksExceptVoidLocks(self, player, localBlocker, worldHaveBlock, worldBlock);

                HandleVoidBlockedShortcutAttempt(self.room, player, localBlocker, worldHaveBlock, worldBlock);
            }

            for (int i = 0; i < self.pushers.Count; i++)
            {
                self.pushers[i].swell = Custom.LerpAndTick(
                    self.pushers[i].swell,
                    self.pushers[i].swellUp ? 1f : 0f,
                    0.02f,
                    0.033333335f
                );

                self.pushers[i].swellUp = false;
            }

            PushNonPlayersAwayFromVoidBlockedShortcuts(self, localBlocker, worldHaveBlock, worldBlock);
        }

        private static void UpdateVoidPlayerAgainstVanillaLocksExceptVoidLocks(ShortcutHelper self, Player player, RoomTransportShortcutBlocker localBlocker, bool worldHaveBlock, WorldShortcutBlock worldBlock)
        {
            if (self.room == null || player == null || !player.Consious || ShortcutHelper.CanBePulledIntoShortcut(player))
                return;

            IntVector2 inputDir = new(player.input[0].x, player.input[0].y);

            bool challengeExitClosed = false;

            if (ModManager.ChallengeModule &&
                self.room.world.game.IsArenaSession &&
                self.room.world.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == DLCSharedEnums.GameTypeID.Challenge &&
                !self.room.world.game.GetArenaGameSession.exitManager.ExitsOpen())
            {
                challengeExitClosed = true;
            }

            for (int j = 0; j < self.pushers.Count; j++)
            {
                ShortcutHelper.ShortcutPusher pusher = self.pushers[j];

                bool voidBlocked = IsVoidBlockedShortcutForPusher(self.room, localBlocker, worldHaveBlock, worldBlock, pusher);
                bool vanillaBlocked = false;

                if (challengeExitClosed)
                {
                    vanillaBlocked = true;

                    if (!self.room.shortcutData(pusher.shortCutPos).ToNode)
                        vanillaBlocked = false;
                }

                if (!vanillaBlocked && !voidBlocked && self.room.lockedShortcuts.Contains(pusher.shortCutPos))
                    vanillaBlocked = true;

                if (voidBlocked &&
                    player.enteringShortCut != null &&
                    player.enteringShortCut.Value == pusher.shortCutPos)
                {
                    player.enteringShortCut = null;
                    player.shortcutDelay = Mathf.Max(player.shortcutDelay, 8);
                }

                if (vanillaBlocked &&
                    player.enteringShortCut != null &&
                    player.enteringShortCut.Value == pusher.shortCutPos)
                {
                    player.enteringShortCut = null;
                }

                if ((pusher.wrongHole || vanillaBlocked) &&
                    (!pusher.floor || player.GoThroughFloors) &&
                    (pusher.shortcutDir.y <= 0 ||
                     (!(player.animation == Player.AnimationIndex.BellySlide) &&
                      !(player.animation == Player.AnimationIndex.DownOnFours))))
                {
                    bool pushingAgainstShortcut =
                        (inputDir.x != 0 && inputDir.x == -pusher.shortcutDir.x) ||
                        (inputDir.y != 0 && inputDir.y == -pusher.shortcutDir.y);

                    bool canSoftPush =
                        player.input[0].jmp ||
                        player.jumpBoost > 0f ||
                        pusher.validNeighbors.Count > 0;

                    if (player.enteringShortCut != null &&
                        player.enteringShortCut.Value == pusher.shortCutPos)
                    {
                        player.enteringShortCut = null;
                    }

                    for (int k = 0; k < player.bodyChunks.Length; k++)
                    {
                        BodyChunk chunk = player.bodyChunks[k];

                        if (pushingAgainstShortcut &&
                            player.input[0].jmp &&
                            !player.input[1].jmp &&
                            Custom.DistLess(pusher.pushPos, chunk.pos, 30f + chunk.rad))
                        {
                            chunk.vel = Vector2.Lerp(
                                chunk.vel,
                                pusher.shortcutDir.ToVector2() * 6f +
                                new Vector2(0f, pusher.shortcutDir.x != 0 ? 6f : 0f),
                                0.5f
                            );
                        }
                        else if (canSoftPush)
                        {
                            float pushRadius =
                                20f +
                                chunk.rad +
                                Custom.LerpMap(pusher.swell, 0.5f, 1f, -5f, 10f, 3f) -
                                ((inputDir.y != 0 && inputDir.y == -pusher.shortcutDir.y) ? 5f : 0f);

                            pusher.swellUp =
                                Custom.DistLess(
                                    pusher.pushPos,
                                    chunk.pos,
                                    Mathf.Max(20f + chunk.rad, pushRadius - 1f)
                                ) &&
                                pushingAgainstShortcut;

                            if (Custom.DistLess(pusher.pushPos, chunk.pos, pushRadius))
                            {
                                float pushFac = Mathf.InverseLerp(
                                    pushRadius - (pushingAgainstShortcut ? 2.5f : 5f),
                                    pushRadius - 20f,
                                    Vector2.Distance(pusher.pushPos, chunk.pos)
                                );

                                if (pusher.validNeighbors.Count > 0 && pushingAgainstShortcut)
                                {
                                    pusher.PushPlayerTowardsValidNeighbor(player, pushFac);
                                }
                                else
                                {
                                    chunk.vel *= Mathf.Lerp(1f, 0.5f, pushFac);
                                    chunk.vel.y += player.gravity * self.room.gravity * pushFac;

                                    Vector2 push =
                                        Vector3.Slerp(
                                            Custom.DirVec(pusher.pushPos, chunk.pos),
                                            pusher.shortcutDir.ToVector2(),
                                            0.9f
                                        ) *
                                        ((pushingAgainstShortcut ? 3f : 0.9f) * pushFac);

                                    chunk.vel += push;
                                    chunk.pos += push;

                                    if (pushingAgainstShortcut && pusher.shortcutDir.x != 0)
                                    {
                                        chunk.vel.y = Mathf.Lerp(
                                            chunk.vel.y,
                                            Mathf.Clamp(chunk.vel.y, -2f, 20f),
                                            0.75f
                                        );
                                    }
                                }
                            }
                        }

                        if (player.rollDirection != 0 &&
                            pusher.shortcutDir.x == -player.rollDirection &&
                            Custom.DistLess(pusher.pushPos, chunk.pos, 30f + chunk.rad))
                        {
                            player.rollDirection = 0;
                            player.animation = Player.AnimationIndex.None;
                            player.rollCounter = 0;
                        }

                        float hardPushRadius = 10f + chunk.rad;

                        if (vanillaBlocked)
                            hardPushRadius *= Mathf.InverseLerp(0f, 500f, player.timeSinceSpawned);

                        if (chunk.pos.y > pusher.pushPos.y - hardPushRadius &&
                            chunk.pos.y < pusher.pushPos.y + hardPushRadius &&
                            chunk.pos.x > pusher.pushPos.x - hardPushRadius &&
                            chunk.pos.x < pusher.pushPos.x + hardPushRadius)
                        {
                            if (pusher.shortcutDir.x != 0)
                            {
                                float push =
                                    pusher.pushPos.x +
                                    hardPushRadius * pusher.shortcutDir.x -
                                    chunk.pos.x;

                                chunk.vel.x += push;
                                chunk.pos.x += push;
                            }
                            else
                            {
                                float push =
                                    pusher.pushPos.y +
                                    hardPushRadius * pusher.shortcutDir.y -
                                    chunk.pos.y;

                                chunk.vel.y += push;
                                chunk.pos.y += push;
                            }
                        }
                    }
                }
            }
        }

        private static void HandleVoidBlockedShortcutAttempt(Room room, Player player, RoomTransportShortcutBlocker localBlocker, bool worldHaveBlock, WorldShortcutBlock worldBlock)
        {
            if (room == null || player == null)
                return;

            RecentShortcutExit exit = player.GetRecentExit();
            bool blockedAttempt = false;

            if (player.enteringShortCut != null)
            {
                IntVector2 entering = player.enteringShortCut.Value;
                blockedAttempt = IsVoidBlockedShortcutTile(room, entering, localBlocker, worldHaveBlock, worldBlock);
            }

            if (!blockedAttempt &&
                exit.timer > 0 &&
                exit.room == room &&
                player.enteringShortCut != null &&
                SameTile(player.enteringShortCut.Value, exit.lockedTile) &&
                IsVoidBlockedShortcutTile(room, exit.lockedTile, localBlocker, worldHaveBlock, worldBlock))
            {
                blockedAttempt = true;
            }

            if (blockedAttempt)
            {
                player.enteringShortCut = null;
                player.shortcutDelay = Mathf.Max(player.shortcutDelay, 8);
            }
        }

        private static bool IsVoidBlockedShortcutTile(
            Room room,
            IntVector2 shortcutTile,
            RoomTransportShortcutBlocker localBlocker,
            bool worldHaveBlock,
            WorldShortcutBlock worldBlock)
        {
            if (room == null)
                return false;

            if (localBlocker != null && localBlocker.ShortcutBlocked(shortcutTile))
                return true;

            if (worldHaveBlock &&
                worldBlock != null &&
                TryResolveExitNode(room, shortcutTile, out int node) &&
                worldBlock.RoomAndNodeBlocked(room.abstractRoom, node))
            {
                return true;
            }

            return false;
        }

        private static bool IsVoidBlockedShortcutForPusher(
            Room room,
            RoomTransportShortcutBlocker localBlocker,
            bool worldHaveBlock,
            WorldShortcutBlock worldBlock,
            ShortcutHelper.ShortcutPusher pusher)
        {
            if (room == null || pusher == null)
                return false;

            return IsVoidBlockedShortcutTile(room, pusher.shortCutPos, localBlocker, worldHaveBlock, worldBlock);
        }

        private static void UpdateVoidRecentExit(Player player)
        {
            RecentShortcutExit exit = player.GetRecentExit();

            if (exit.timer > 0)
            {
                exit.timer--;

                if (exit.timer <= 0)
                    exit.room = null;
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
        }

        private static void PushNonPlayersAwayFromVoidBlockedShortcuts(ShortcutHelper self, RoomTransportShortcutBlocker localBlocker, bool worldHaveBlock, WorldShortcutBlock worldBlock)
        {
            if (self.room == null || self.room.abstractRoom == null)
                return;

            for (int pusherIndex = 0; pusherIndex < self.pushers.Count; pusherIndex++)
            {
                ShortcutHelper.ShortcutPusher pusher = self.pushers[pusherIndex];

                if (!IsVoidBlockedShortcutForPusher(self.room, localBlocker, worldHaveBlock, worldBlock, pusher))
                    continue;

                for (int creatureIndex = 0; creatureIndex < self.room.abstractRoom.creatures.Count; creatureIndex++)
                {
                    Creature crit = self.room.abstractRoom.creatures[creatureIndex].realizedCreature;

                    if (crit == null || crit is Player)
                        continue;

                    if (crit.enteringShortCut != null && crit.enteringShortCut.Value == pusher.shortCutPos)
                        crit.enteringShortCut = null;

                    for (int chunkIndex = 0; chunkIndex < crit.bodyChunks.Length; chunkIndex++)
                    {
                        BodyChunk chunk = crit.bodyChunks[chunkIndex];
                        float pushRadius = 10f + chunk.rad;

                        if (chunk.pos.y > pusher.pushPos.y - pushRadius &&
                            chunk.pos.y < pusher.pushPos.y + pushRadius &&
                            chunk.pos.x > pusher.pushPos.x - pushRadius &&
                            chunk.pos.x < pusher.pushPos.x + pushRadius)
                        {
                            if (pusher.shortcutDir.x != 0)
                            {
                                float push = pusher.pushPos.x + pushRadius * pusher.shortcutDir.x - chunk.pos.x;
                                chunk.vel.x += push;
                                chunk.pos.x += push;
                            }
                            else
                            {
                                float push = pusher.pushPos.y + pushRadius * pusher.shortcutDir.y - chunk.pos.y;
                                chunk.vel.y += push;
                                chunk.pos.y += push;
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
                RoomTransportShortcutBlocker roomBlock = self.room.GetRoomShortcutBlock();
                for (int i = 0; i < roomBlock.blockedShorcuts.Count; i++)
                {
                    RoomTransportShortcutBlocker.RoomShortcutBlock shortcut = roomBlock.blockedShorcuts[i];
                    if (shortcut.passCount == shortcut.maxPassCount || shortcut.blockTime > 0)
                    {
                        int index1 = self.room.shortcutsIndex.IndexfOf(shortcut.blockedShrotcut1);
                        int index2 = self.room.shortcutsIndex.IndexfOf(shortcut.blockedShrotcut2);

                        float lerp = Mathf.Sin(shortcut.signalCycle * 2f * Mathf.PI);

                        if (index1 > -1)
                        {
                            FSprite sprite1 = self.entranceSprites[index1, 0];
                            sprite1?.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                            FSprite sprite2 = self.entranceSprites[index1, 1];
                            sprite2?.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
                        }

                        if (index2 > -1)
                        {
                            FSprite sprite1 = self.entranceSprites[index2, 0];
                            sprite1?.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                            FSprite sprite2 = self.entranceSprites[index2, 1];
                            sprite2?.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
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
                                sprite1?.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite1.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;

                                FSprite sprite2 = self.entranceSprites[index, 1];
                                sprite2?.color = (shortcut.passCount == shortcut.maxPassCount) ? Color.Lerp(sprite2.color, DrawSprites.voidColor, lerp) : DrawSprites.voidColor;
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

                if (localHitBlock.blockTime <= 0 && !sameLocalExitRecently)
                {
                    localHitBlock.RegisterPass();

                    if (localHitBlock.passCount > localHitBlock.maxPassCount)
                        localHitBlock.Block();

                    exit.lastCountedRoom = newRoom.abstractRoom;
                    exit.lastCountedNode = -1;
                    exit.lastCountedWasRoomShortcut = true;
                    exit.lastCountedShortcutTile = localLockedTile;
                    exit.countDebounce = 10;
                }

                if (localHitBlock.blockTime <= 0)
                    return;

                exit.room = newRoom;
                exit.lockedTile = localLockedTile;
                exit.shortcutDir = localShortcutDir;
                exit.timer = 20;

                voidPlayer.enteringShortCut = null;
                voidPlayer.shortcutDelay = Mathf.Max(voidPlayer.shortcutDelay, 8);

                for (int i = 0; i < voidPlayer.bodyChunks.Length; i++)
                    voidPlayer.bodyChunks[i].vel *= 0f;

                return;
            }

            if (worldHitBlock == null || exitNode < 0)
                return;

            bool sameWorldExitRecently =
                exit.countDebounce > 0 &&
                exit.lastCountedRoom == newRoom.abstractRoom &&
                !exit.lastCountedWasRoomShortcut &&
                exit.lastCountedNode == exitNode;

            if (worldHitBlock.blockTime <= 0 && !sameWorldExitRecently)
            {
                worldHitBlock.RegisterPass();

                if (worldHitBlock.passCount > worldHitBlock.maxPassCount)
                    worldHitBlock.Block();

                exit.lastCountedRoom = newRoom.abstractRoom;
                exit.lastCountedNode = exitNode;
                exit.lastCountedWasRoomShortcut = false;
                exit.lastCountedShortcutTile = default;
                exit.countDebounce = 10;
            }

            if (worldHitBlock.blockTime <= 0)
                return;

            exit.room = newRoom;
            exit.lockedTile = worldLockedTile;
            exit.shortcutDir = worldShortcutDir;
            exit.timer = 20;

            voidPlayer.enteringShortCut = null;
            voidPlayer.shortcutDelay = Mathf.Max(voidPlayer.shortcutDelay, 8);

            for (int i = 0; i < voidPlayer.bodyChunks.Length; i++)
                voidPlayer.bodyChunks[i].vel *= 0f;
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

        private readonly struct RemovedVoidPlayer(Player player, AbstractCreature abstractCreature, int index)
        {
            public readonly Player player = player;
            public readonly AbstractCreature abstractCreature = abstractCreature;
            public readonly int index = index;
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
                    signalCycle += 0.033333333f;
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
                    maxPassCount = UnityEngine.Random.Range(3, 8);
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
            public readonly List<RoomShortcutBlock> blockedShorcuts = [];

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
                RoomShortcutBlock block = GetShortcutBlock(shortcut);
                return block != null && block.blockTime > 0;
            }

            public void AddShortcut(IntVector2 shortcut)
            {
                blockedShorcuts.Add(new RoomShortcutBlock(room, shortcut));
            }

            public class RoomShortcutBlock : VoidShortcutBlock
            {
                public readonly Room room;
                public readonly IntVector2 blockedShrotcut1;
                public readonly IntVector2 blockedShrotcut2;

                public RoomShortcutBlock(Room room, IntVector2 roomShortcut)
                {
                    this.room = room;
                    blockedShrotcut1 = roomShortcut;

                    ShortcutData sData = room.shortcutData(roomShortcut);
                    blockedShrotcut2 = sData.path[sData.path.Length - 1];
                }

                public bool CompareShortcut(IntVector2 transportShortcut)
                {
                    return blockedShrotcut1 == transportShortcut || blockedShrotcut2 == transportShortcut;
                }

                public override void Block()
                {
                    blockTime = UnityEngine.Random.Range(400, 801);

                    if (!room.lockedShortcuts.Contains(blockedShrotcut1))
                        room.lockedShortcuts.Add(blockedShrotcut1);

                    if (!room.lockedShortcuts.Contains(blockedShrotcut2))
                        room.lockedShortcuts.Add(blockedShrotcut2);

                    ResetPassCount();
                }

                public override void Unlock()
                {
                    room.lockedShortcuts.Remove(blockedShrotcut1);
                    room.lockedShortcuts.Remove(blockedShrotcut2);
                }
            }
        }

        public class WorldShortcutBlock
        {
            public readonly List<BlockedRoomExit> blockedShortcuts = [];

            public bool RoomAndNodeBlocked(AbstractRoom room, int node)
            {
                if (node <= -1)
                    return false;

                BlockedRoomExit blockedShortcut = GetBlockedShortcut(room, node);
                return blockedShortcut != null && blockedShortcut.blockTime > 0;
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

                public readonly int node1;
                public readonly int node2;

                public BlockedRoomExit(AbstractRoom fromRoom, int toRoom2Node)
                {
                    room1 = fromRoom;

                    if (fromRoom == null)
                    {
                        room2 = fromRoom;
                        node1 = -1;
                        node2 = -1;
                        return;
                    }

                    if (toRoom2Node < 0 ||
                        toRoom2Node >= fromRoom.connections.Length ||
                        fromRoom.connections[toRoom2Node] <= -1)
                    {
                        room2 = fromRoom;
                        node1 = toRoom2Node;
                        node2 = -1;
                        return;
                    }

                    int room2Index = fromRoom.connections[toRoom2Node];

                    if (fromRoom.world == null)
                    {
                        room2 = fromRoom;
                        node1 = toRoom2Node;
                        node2 = -1;
                        return;
                    }

                    room2 = fromRoom.world.GetAbstractRoom(room2Index);

                    if (room2 == null)
                    {
                        room2 = fromRoom;
                        node1 = toRoom2Node;
                        node2 = -1;
                        return;
                    }

                    node1 = toRoom2Node;
                    node2 = room2.ExitIndex(fromRoom.index);
                }

                public override void Block()
                {
                    blockTime = UnityEngine.Random.Range(400, 801);

                    if (room1?.realizedRoom != null && node1 >= 0)
                    {
                        ShortcutData shortcut1 = room1.realizedRoom.ShortcutLeadingToNode(node1);
                        if (shortcut1.destNode >= 0)
                        {
                            IntVector2 tile1 = shortcut1.StartTile;
                            if (!room1.realizedRoom.lockedShortcuts.Contains(tile1))
                            {
                                room1.realizedRoom.lockedShortcuts.Add(tile1);
                            }
                        }
                    }

                    if (room2?.realizedRoom != null && node2 >= 0 && room2 != room1)
                    {
                        ShortcutData shortcut2 = room2.realizedRoom.ShortcutLeadingToNode(node2);
                        if (shortcut2.destNode >= 0)
                        {
                            IntVector2 tile2 = shortcut2.StartTile;
                            if (!room2.realizedRoom.lockedShortcuts.Contains(tile2))
                            {
                                room2.realizedRoom.lockedShortcuts.Add(tile2);
                            }
                        }
                    }

                    ResetPassCount();
                }

                public override void Unlock()
                {

                    if (room1?.realizedRoom != null && node1 >= 0)
                    {
                        ShortcutData shortcut1 = room1.realizedRoom.ShortcutLeadingToNode(node1);
                        if (shortcut1.destNode >= 0)
                        {
                            room1.realizedRoom.lockedShortcuts.Remove(shortcut1.StartTile);
                        }
                    }

                    if (room2?.realizedRoom != null && node2 >= 0 && room2 != room1)
                    {
                        ShortcutData shortcut2 = room2.realizedRoom.ShortcutLeadingToNode(node2);
                        if (shortcut2.destNode >= 0)
                        {
                            room2.realizedRoom.lockedShortcuts.Remove(shortcut2.StartTile);
                        }
                    }
                }

                public bool CompareRoomAndNode(AbstractRoom room, int node)
                {
                    if (room == null)
                        return false;

                    return (room1 == room && node1 == node) || (room2 == room && node2 == node);
                }
            }
        }
    }
}
