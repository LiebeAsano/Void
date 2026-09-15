using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using MoreSlugcats;
using RWCustom;
using SlugBase.SaveData;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

public static class SBGhostSave
{
    private const string wakeRoomKey = "VoidSlugcatEchoWakeRoom";
    private const string wakeTileKey = "VoidSlugcatEchoWakeTile";
    private const string wakeItemsKey = "VoidSlugcatEchoWakeHeldItemsV1";

    private const string followSpawnKey = "VoidSlugcatEchoHeldItemsFollowSpawnV1";

    private static readonly ConditionalWeakTable<RainWorldGame, EncounterState> encounters = new();
    private static readonly ConditionalWeakTable<AbstractCreature, WakePoint> pendingWake = new();
    private static readonly ConditionalWeakTable<RainWorldGame, WakePoint> pendingItems = new();
    private static readonly IntVector2[] directions =
    [
        new(0, -1), new(-1, 0), new(1, 0), new(0, 1)
    ];

    private static bool hooked;

    private sealed class EncounterState(Room room, GhostWorldPresence.GhostID ghostID, Vector2 anchor)
    {
        public readonly Room room = room;
        public readonly GhostWorldPresence.GhostID ghostID = ghostID;
        public readonly HashSet<AbstractCreature> participants = [];
        public Vector2 anchor = anchor;
        public bool hasVoidAnchor;
        public bool completed;
    }

    private sealed class EncounterSnapshot(SBGhostSave.EncounterState encounter, IntVector2 tile, bool placePlayer, string[] items)
    {
        public readonly EncounterState encounter = encounter;
        public readonly IntVector2 tile = tile;
        public readonly bool placePlayer = placePlayer;
        public readonly string[] items = items;
    }

    private sealed class WakePoint(string roomName, int cycle, IntVector2 tile, string[] items)
    {
        public readonly string roomName = roomName;
        public readonly int cycle = cycle;
        public readonly IntVector2 tile = tile;
        public readonly string[] items = items;
        public bool itemsRestored;

        public bool PlacePlayer => tile.x >= 1 && tile.y >= 1;
    }

    public static void Hook()
    {
        if (hooked) return;
        hooked = true;

        On.SaveState.GhostEncounter += SaveState_GhostEncounter;
        On.Ghost.Update += Ghost_Update;
        On.Ghost.FadeOutFinished += Ghost_FadeOutFinished;
        On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
        On.RainWorldGame.Win += RainWorldGame_Win;
        On.RainWorldGame.ctor += RainWorldGame_ctor;
        On.Player.Update += Player_Update;
    }

    private static void SaveState_GhostEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
    {
        orig(self, ghost, rainWorld);

        if (self.saveStateNumber == VoidEnums.SlugcatID.Void)
            self.progression.SaveWorldStateAndProgression(false);
    }

    private static void Ghost_Update(On.Ghost.orig_Update orig, Ghost self, bool eu)
    {
        TrackEncounter(self);
        orig(self, eu);
    }

    private static EncounterState TrackEncounter(Ghost ghost)
    {
        Room room = ghost.room;
        RainWorldGame game = room?.game;
        if (game == null || !game.IsVoidStoryCampaign() || room.world.singleRoomWorld
            || ghost.worldGhost == null || game.manager.upcomingProcess != null)
            return null;

        EncounterState state = GetEncounter(game, room, ghost.worldGhost.ghostID, ghost.pos);
        RecordParticipants(game, state);
        return state;
    }

    private static EncounterState GetEncounter(RainWorldGame game, Room room, GhostWorldPresence.GhostID ghostID, Vector2 anchor)
    {
        if (!encounters.TryGetValue(game, out EncounterState state)
            || state.room != room || state.ghostID != ghostID)
        {
            state = new EncounterState(room, ghostID, anchor);
            encounters.Remove(game);
            encounters.Add(game, state);
        }
        return state;
    }

    private static void RecordParticipants(RainWorldGame game, EncounterState state)
    {
        if (state.completed || game.Players == null) return;

        Player anchorPlayer = null;
        for (int i = 0; i < game.Players.Count; i++)
        {
            AbstractCreature participant = game.Players[i];
            if (participant?.realizedCreature is not Player player
                || player.dead || player.room != state.room || player.inShortcut)
                continue;

            state.participants.Add(participant);
            if (anchorPlayer == null || (player.SlugCatClass == VoidEnums.SlugcatID.Void
                && anchorPlayer.SlugCatClass != VoidEnums.SlugcatID.Void))
                anchorPlayer = player;
        }

        if (anchorPlayer != null && (!state.hasVoidAnchor
            || anchorPlayer.SlugCatClass == VoidEnums.SlugcatID.Void))
        {
            state.anchor = anchorPlayer.mainBodyChunk.pos;
            state.hasVoidAnchor |= anchorPlayer.SlugCatClass == VoidEnums.SlugcatID.Void;
        }
    }

    private static void Ghost_FadeOutFinished(On.Ghost.orig_FadeOutFinished orig, Ghost self)
    {
        TrackEncounter(self);
        orig(self);
    }

    private static EncounterSnapshot CaptureEncounter(RainWorldGame game, GhostWorldPresence.GhostID ghostID)
    {
        if (game == null || ghostID == null || !game.IsVoidStoryCampaign() || game.world == null
            || game.world.singleRoomWorld || game.manager.upcomingProcess != null)
            return null;

        if (!encounters.TryGetValue(game, out EncounterState encounter)
            || encounter.ghostID != ghostID)
        {
            GhostWorldPresence presence = game.world.worldGhost;
            Room room = presence?.ghostID == ghostID ? presence.ghostRoom?.realizedRoom : null;
            if (room == null) return null;
            Vector2 anchor = new(room.TileWidth * 10f, room.TileHeight * 10f);
            for (int i = 0; i < room.updateList.Count; i++)
            {
                if (room.updateList[i] is Ghost ghost && ghost.worldGhost?.ghostID == ghostID)
                {
                    anchor = ghost.pos;
                    break;
                }
            }
            encounter = GetEncounter(game, room, ghostID, anchor);
        }
        if (encounter.completed) return null;
        RecordParticipants(game, encounter);
        if (game.Players == null || encounter.participants.Count == 0) return null;

        IntVector2 origin = encounter.room.GetTilePosition(encounter.anchor);
        IntVector2 tile = origin;
        bool isMS = ModManager.MSC && ghostID == MoreSlugcatsEnums.GhostID.MS;
        bool placePlayer = !isMS && TryFindWakeTile(encounter.room, origin, out tile);

        string[] items = CaptureHeldItems(encounter.room, origin, encounter.participants);
        return new EncounterSnapshot(encounter, tile, placePlayer, items);
    }

    private static void RainWorldGame_GhostShutDown(On.RainWorldGame.orig_GhostShutDown orig, RainWorldGame self, GhostWorldPresence.GhostID ghostID)
    {
        EncounterSnapshot snapshot = CaptureEncounter(self, ghostID);
        orig(self, ghostID);

        if (snapshot != null && self.sawAGhost == ghostID && self.manager.upcomingProcess != null)
            CommitEncounter(self, snapshot);
    }

    private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
    {
        EncounterSnapshot snapshot = null;

        if (ModManager.MSC && !malnourished && !fromWarpPoint && self.IsVoidStoryCampaign()
            && self.world?.worldGhost?.ghostID == MoreSlugcatsEnums.GhostID.MS
            && self.GetStorySession.saveState.sessionEndingFromSpinningTopEncounter
            && self.GetStorySession.saveState.deathPersistentSaveData.karmaCap < 9
            && self.world.worldGhost.ghostRoom?.realizedRoom is Room room && room.BeingViewed)
            snapshot = CaptureEncounter(self, MoreSlugcatsEnums.GhostID.MS);

        orig(self, malnourished, fromWarpPoint);

        if (snapshot != null && (self.manager.upcomingProcess == ProcessManager.ProcessID.SleepScreen
            || self.manager.upcomingProcess == ProcessManager.ProcessID.Dream))
            CommitEncounter(self, snapshot);
    }

    private static void CommitEncounter(RainWorldGame game, EncounterSnapshot snapshot)
    {
        EncounterState encounter = snapshot.encounter;
        if (encounter.completed) return;
        encounter.completed = true;
        Room room = encounter.room;
        IntVector2 tile = snapshot.tile;
        string[] items = snapshot.items;
        SaveState save = game.GetStorySession.saveState;
        var data = save.miscWorldSaveData.GetSlugBaseData();
        data.Set(wakeItemsKey, items);
        save.nextIssuedID = Math.Max(save.nextIssuedID, game.nextIssuedId);

        HashSet<string> savedIDs = GetSavedIDs(items);
        RemoveQueuedCopies(save, savedIDs);
        RemoveOldStomachCopies(save, savedIDs);

        if (snapshot.placePlayer)
        {
            data.Set(wakeRoomKey, room.abstractRoom.name);
            data.Set(wakeTileKey, new int[] { tile.x, tile.y, save.cycleNumber });
            data.Set(followSpawnKey, false);
            UpdateHeldItemTrackers(save, room, tile, items);
            RainWorldGame.ForceSaveNewDenLocation(game, room.abstractRoom.name, false);
        }
        else
        {
            data.Set(followSpawnKey, true);
            save.progression.SaveWorldStateAndProgression(false);
            if (!(ModManager.MSC && encounter.ghostID == MoreSlugcatsEnums.GhostID.MS))
                LogExErr("SBGhostSave: no suitable dry floor in " + room.abstractRoom.name
                    + "; keeping the previous checkpoint and preserving held items there.");
        }
    }

    private static void BindItemsToSpawn(SaveState save)
    {
        var data = save.miscWorldSaveData.GetSlugBaseData();
        if (!data.TryGet(followSpawnKey, out bool followSpawn) || !followSpawn
            || string.IsNullOrEmpty(save.denPosition))
            return;

        IntVector2 tile = TryGetWakePoint(save, out WakePoint previous) && previous.PlacePlayer
            ? previous.tile : new IntVector2(-1, -1);
        data.Set(wakeRoomKey, save.denPosition);
        data.Set(wakeTileKey, new int[] { tile.x, tile.y, save.cycleNumber });
        data.Set(followSpawnKey, false);
        save.progression.SaveWorldStateAndProgression(false);
    }

    private static bool TryGetWakePoint(SaveState save, out WakePoint point)
    {
        point = null;

        if (save == null || save.saveStateNumber != VoidEnums.SlugcatID.Void)
            return false;

        var data = save.miscWorldSaveData.GetSlugBaseData();

        if (!data.TryGet(wakeRoomKey, out string roomName) || string.IsNullOrEmpty(roomName)
            || save.denPosition != roomName
            || !data.TryGet(wakeTileKey, out int[] tile) || tile == null || tile.Length != 3
            || tile[2] != save.cycleNumber
            || ((tile[0] < 1 || tile[1] < 1) && (tile[0] != -1 || tile[1] != -1)))
            return false;

        data.TryGet(wakeItemsKey, out string[] items);
        point = new WakePoint(roomName, tile[2], new IntVector2(tile[0], tile[1]), items ?? []);
        return true;
    }

    private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);

        if (self.overWorld == null || self.world == null || self.world.singleRoomWorld || self.wasAnArtificerDream
            || !self.IsVoidStoryCampaign() || (ModManager.MSC && self.rainWorld.safariMode))
            return;

        BindItemsToSpawn(self.GetStorySession.saveState);
        if (!TryGetWakePoint(self.GetStorySession.saveState, out WakePoint point)) return;

        if (point.items.Length > 0)
        {
            pendingItems.Remove(self);
            pendingItems.Add(self, point);
        }
        if (!point.PlacePlayer) return;

        AbstractRoom room = self.world.GetAbstractRoom(point.roomName);
        if (room == null || room.shelter || self.Players == null)
            return;

        for (int i = 0; i < self.Players.Count; i++)
        {
            AbstractCreature player = self.Players[i];
            if (player == null || player.pos.room != room.index)
                continue;

            player.pos = new WorldCoordinate(room.index, point.tile.x, point.tile.y, -1);
            pendingWake.Remove(player);
            pendingWake.Add(player, point);
        }
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.abstractCreature != null && self.room != null && !self.inShortcut
            && pendingWake.TryGetValue(self.abstractCreature, out WakePoint point))
        {
            pendingWake.Remove(self.abstractCreature);

            Room room = self.room;
            if (!self.dead && room.abstractRoom.name == point.roomName
                && room.game.session is StoryGameSession session
                && session.saveStateNumber == VoidEnums.SlugcatID.Void
                && session.saveState.denPosition == point.roomName
                && session.saveState.cycleNumber == point.cycle
                && TryFindWakeTile(room, point.tile, out IntVector2 tile))
            {
                self.abstractCreature.pos = new WorldCoordinate(room.abstractRoom.index, tile.x, tile.y, -1);
                Vector2 center = room.MiddleOfTile(tile);

                float halfLength = self.bodyChunkConnections[0].distance * 0.5f;
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    BodyChunk chunk = self.bodyChunks[i];
                    chunk.HardSetPosition(center + new Vector2(i == 0 ? -halfLength : halfLength, -10f + chunk.rad));
                    chunk.vel = Vector2.zero;
                }

                self.animation = Player.AnimationIndex.None;
                self.bodyMode = Player.BodyModeIndex.Default;
                self.standing = false;
                self.forceSleepCounter = 0;
                self.sleepCounter = 100;
                self.sleepCurlUp = 1f;
                self.graphicsModule?.Reset();

                int cameraPosition = room.CameraViewingPoint(center);
                if (cameraPosition >= 0 && room.game.cameras != null)
                {
                    for (int i = 0; i < room.game.cameras.Length; i++)
                    {
                        RoomCamera camera = room.game.cameras[i];
                        if (camera != null && camera.followAbstractCreature == self.abstractCreature
                            && (camera.room != room || camera.currentCameraPosition != cameraPosition))
                            camera.MoveCamera(room, cameraPosition);
                    }
                }
            }
        }

        orig(self, eu);
        RestoreItemsAfterWake(self);
    }

    private static void RestoreItemsAfterWake(Player player)
    {
        Room room = player.room;
        RainWorldGame game = room?.game;
        if (game == null || player.dead || player.inShortcut || player.abstractCreature == null
            || !pendingItems.TryGetValue(game, out WakePoint point))
            return;

        if (game.session is not StoryGameSession session
            || session.saveStateNumber != VoidEnums.SlugcatID.Void
            || session.saveState.denPosition != point.roomName
            || session.saveState.cycleNumber != point.cycle)
        {
            pendingItems.Remove(game);
            return;
        }
        if (game.Players == null || !game.Players.Contains(player.abstractCreature)
            || (point.PlacePlayer && room.abstractRoom.name != point.roomName))
            return;

        Vector2 center = (player.bodyChunks[0].pos + player.bodyChunks[1].pos) * 0.5f;
        IntVector2 tile = room.GetTilePosition(center);
        if (tile.x < 0 || tile.y < 0 || tile.x >= room.TileWidth || tile.y >= room.TileHeight)
            return;

        pendingItems.Remove(game);
        RestoreHeldItems(room, tile, point, session.saveState);
    }

    private static string[] CaptureHeldItems(Room room, IntVector2 tile, HashSet<AbstractCreature> participants)
    {
        List<string> items = [];
        HashSet<string> ids = new(StringComparer.Ordinal);
        WorldCoordinate atPos = new(room.abstractRoom.index, tile.x, tile.y, -1);

        for (int p = 0; p < room.game.Players.Count; p++)
        {
            AbstractCreature participant = room.game.Players[p];
            if (participant == null || !participants.Contains(participant)
                || participant.realizedCreature is not Player player
                || player.dead || player.grasps == null)
                continue;

            for (int hand = 0; hand < player.grasps.Length; hand++)
            {
                PhysicalObject held = player.grasps[hand]?.grabbed;
                AbstractPhysicalObject item = held?.abstractPhysicalObject;

                if (held == null || held is Player || held.slatedForDeletetion || item == null
                    || item is AbstractCreature { state: PlayerState })
                    continue;

                string id = item.ID.ToString();
                if (ids.Contains(id)) continue;

                try
                {
                    string text;
                    if (item is AbstractCreature creature)
                    {
                        text = SaveState.AbstractCreatureToStringStoryWorld(creature, atPos);
                    }
                    else
                    {
                        WorldCoordinate oldPos = item.pos;
                        try
                        {
                            item.pos = atPos;
                            text = item.ToString();
                        }
                        finally
                        {
                            item.pos = oldPos;
                        }
                    }

                    if (SavedObjectID(text) != id)
                        throw new InvalidOperationException("Unsupported save string for " + item.type);

                    items.Add(text);
                    ids.Add(id);
                }
                catch (Exception exception)
                {
                    LogExErr("SBGhostSave: could not save held item " + id + ": " + exception);
                }
            }
        }

        return [.. items];
    }

    private static string RebaseSavedPosition(string text, Room room, WorldCoordinate atPos)
    {
        int objectSeparator = text.IndexOf("<oA>", StringComparison.Ordinal);
        int creatureSeparator = text.IndexOf("<cA>", StringComparison.Ordinal);
        bool isObject = objectSeparator >= 0 && (creatureSeparator < 0 || objectSeparator < creatureSeparator);
        string separator = isObject ? "<oA>" : "<cA>";
        int first = isObject ? objectSeparator : creatureSeparator;
        int second = first >= 0 ? text.IndexOf(separator, first + separator.Length, StringComparison.Ordinal) : -1;
        if (second < 0) throw new FormatException("Missing saved object position");

        int start = second + separator.Length;
        int end = text.IndexOf(separator, start, StringComparison.Ordinal);
        if (end < 0) end = text.Length;
        string position = isObject ? atPos.SaveToString()
            : room.abstractRoom.name + "." + atPos.abstractNode.ToString(CultureInfo.InvariantCulture);

        return text.Substring(0, start) + position + text.Substring(end);
    }

    private static string SavedObjectID(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        int objectSeparator = text.IndexOf("<oA>", StringComparison.Ordinal);
        int creatureSeparator = text.IndexOf("<cA>", StringComparison.Ordinal);
        int start = 0;
        int end;
        string layerSeparator;

        if (objectSeparator >= 0 && (creatureSeparator < 0 || objectSeparator < creatureSeparator))
        {
            end = objectSeparator;
            layerSeparator = "<oB>";
        }
        else if (creatureSeparator >= 0)
        {
            start = creatureSeparator + 4;
            end = text.IndexOf("<cA>", start, StringComparison.Ordinal);
            layerSeparator = "<cB>";
        }
        else return null;

        if (end <= start) return null;
        string id = text.Substring(start, end - start);
        int layer = id.IndexOf(layerSeparator, StringComparison.Ordinal);
        if (layer >= 0) id = id.Substring(0, layer);
        return id.Length == 0 ? null : id;
    }

    private static HashSet<string> GetSavedIDs(string[] items)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < items.Length; i++)
        {
            string id = SavedObjectID(items[i]);
            if (id != null) ids.Add(id);
        }
        return ids;
    }

    private static void RemoveQueuedCopies(SaveState save, HashSet<string> ids)
    {
        if (ids.Count == 0) return;

        RemoveQueuedCopies(save.pendingObjects, ids);
        RemoveQueuedCopies(save.spawnedPendingObjects, ids);
        RemoveQueuedCopies(save.pendingFriendCreatures, ids);
        RemoveQueuedCopies(save.spawnedPendingFriendCreatures, ids);
    }

    private static void RemoveQueuedCopies(List<string> items, HashSet<string> ids)
    {
        if (items == null) return;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            string id = SavedObjectID(items[i]);
            if (id != null && ids.Contains(id)) items.RemoveAt(i);
        }
    }

    private static void RemoveOldStomachCopies(SaveState save, HashSet<string> ids)
    {
        if (save.swallowedItems == null || ids.Count == 0) return;

        for (int i = 0; i < save.swallowedItems.Length; i++)
        {
            string id = SavedObjectID(save.swallowedItems[i]);
            if (id != null && ids.Contains(id)) save.swallowedItems[i] = "0";
        }
    }

    private static bool TrackerMatches(PersistentObjectTracker tracker, string id)
    {
        return tracker != null && ((tracker.obj != null && tracker.obj.ID.ToString() == id)
            || SavedObjectID(tracker.objRepresentation) == id);
    }

    private static void UpdateHeldItemTrackers(SaveState save, Room room, IntVector2 tile, string[] items)
    {
        if (save.objectTrackers == null) return;
        WorldCoordinate atPos = new(room.abstractRoom.index, tile.x, tile.y, -1);
        for (int i = 0; i < items.Length; i++)
        {
            string id = SavedObjectID(items[i]);
            if (id == null) continue;
            for (int t = 0; t < save.objectTrackers.Count; t++)
            {
                PersistentObjectTracker tracker = save.objectTrackers[t];
                if (!TrackerMatches(tracker, id)) continue;
                try
                {
                    tracker.ChangeDesiredSpawnLocation(atPos);
                    tracker.lastSeenRoom = room.abstractRoom.name;
                    tracker.lastSeenRegion = room.world.name;
                }
                catch (Exception exception)
                {
                    LogExErr("SBGhostSave: could not update held-item tracker " + id + ": " + exception);
                }
            }
        }
    }

    private static void RestoreHeldItems(Room room, IntVector2 tile, WakePoint point, SaveState save)
    {
        if (point.itemsRestored) return;
        point.itemsRestored = true;
        if (point.items.Length == 0) return;

        HashSet<string> restored = new(StringComparer.Ordinal);
        WorldCoordinate atPos = new(room.abstractRoom.index, tile.x, tile.y, -1);

        for (int i = 0; i < point.items.Length; i++)
        {
            string text = point.items[i];
            string id = SavedObjectID(text);
            if (id == null || restored.Contains(id)) continue;

            AbstractPhysicalObject item = null;
            bool realized = false;
            try
            {
                text = RebaseSavedPosition(text, room, atPos);
                int objectSeparator = text.IndexOf("<oA>", StringComparison.Ordinal);
                int creatureSeparator = text.IndexOf("<cA>", StringComparison.Ordinal);
                item = objectSeparator >= 0 && (creatureSeparator < 0 || objectSeparator < creatureSeparator)
                    ? SaveState.AbstractPhysicalObjectFromString(room.world, text)
                    : SaveState.AbstractCreatureFromString(room.world, text, false, atPos);

                if (item == null || item.ID.ToString() != id || item is AbstractCreature { state: PlayerState })
                    throw new InvalidOperationException("The saved object could not be restored");

                item.pos = atPos;
                if (item is AbstractConsumable consumable) consumable.isFresh = false;

                room.abstractRoom.AddEntity(item);
                item.RealizeInRoom();
                if (item.realizedObject == null)
                    throw new InvalidOperationException("No realized object; check the item mod/parser");
                realized = true;

                RemoveLoadedCopies(room.world, item, save);
                if (!room.abstractRoom.entities.Contains(item)) room.abstractRoom.AddEntity(item);
                PlaceBesidePlayer(item.realizedObject, room, tile, restored.Count);

                if (save.objectTrackers != null)
                {
                    for (int t = 0; t < save.objectTrackers.Count; t++)
                    {
                        PersistentObjectTracker tracker = save.objectTrackers[t];
                        if (!TrackerMatches(tracker, id)) continue;
                        tracker.obj = null;
                        tracker.LinkObjectToTracker(item);
                        tracker.ChangeDesiredSpawnLocation(item.pos);
                        tracker.lastSeenRoom = room.abstractRoom.name;
                        tracker.lastSeenRegion = room.world.name;
                    }
                }
                restored.Add(id);
            }
            catch (Exception exception)
            {
                if (!realized && item != null)
                {
                    try
                    {
                        DetachLoadedCopy(item);
                    }
                    catch (Exception cleanupException)
                    {
                        LogExErr("SBGhostSave: failed to clean up held item " + id + ": " + cleanupException);
                    }
                }
                if (realized) restored.Add(id);
                LogExErr("SBGhostSave: could not restore held item " + id + ": " + exception);
            }
        }

        RemoveQueuedCopies(save, restored);
    }

    private static void RemoveLoadedCopies(World world, AbstractPhysicalObject keep, SaveState save)
    {
        HashSet<AbstractPhysicalObject> copies = [];
        for (int r = 0; r < world.NumberOfRooms; r++)
        {
            AbstractRoom abstractRoom = world.GetAbstractRoom(world.firstRoomIndex + r);
            if (abstractRoom == null) continue;
            CollectLoadedCopies(abstractRoom.entities, keep, copies);
            CollectLoadedCopies(abstractRoom.entitiesInDens, keep, copies);
        }
        if (save.objectTrackers != null)
        {
            for (int i = 0; i < save.objectTrackers.Count; i++)
            {
                AbstractPhysicalObject item = save.objectTrackers[i]?.obj;
                if (item != null && item != keep && item.world == world && item.ID.Equals(keep.ID)
                    && item is not AbstractCreature { state: PlayerState })
                    copies.Add(item);
            }
        }
        foreach (AbstractPhysicalObject copy in copies) DetachLoadedCopy(copy);
    }

    private static void CollectLoadedCopies(List<AbstractWorldEntity> entities, AbstractPhysicalObject keep, HashSet<AbstractPhysicalObject> copies)
    {
        if (entities == null) return;
        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] is AbstractPhysicalObject item && item != keep && item.ID.Equals(keep.ID)
                && item is not AbstractCreature { state: PlayerState })
                copies.Add(item);
        }
    }

    private static void DetachLoadedCopy(AbstractPhysicalObject item)
    {
        PhysicalObject physical = item.realizedObject;
        if (physical != null)
        {
            for (int i = physical.grabbedBy.Count - 1; i >= 0; i--)
                physical.grabbedBy[i].Release();
            physical.RemoveFromRoom();
        }
        for (int i = item.stuckObjects.Count - 1; i >= 0; i--)
            item.stuckObjects[i].Deactivate();
        item.Room?.RemoveEntity(item);
    }

    private static void PlaceBesidePlayer(PhysicalObject item, Room room, IntVector2 tile, int index)
    {
        if (item is Weapon weapon) weapon.ChangeMode(Weapon.Mode.Free);
        Vector2 center = room.MiddleOfTile(tile);
        Vector2 origin = item.firstChunk.pos;
        float side = (index % 2 == 0 ? -1f : 1f) * 16f;

        for (int i = 0; i < item.bodyChunks.Length; i++)
        {
            BodyChunk chunk = item.bodyChunks[i];
            Vector2 offset = chunk.pos - origin;
            Vector2 position = center + new Vector2(side, -9f + chunk.rad) + offset;
            if (room.GetTile(position).Solid || room.PointSubmerged(position))
                position = center + new Vector2(0f, -9f + chunk.rad);
            chunk.HardSetPosition(position);
            chunk.vel = Vector2.zero;
        }
        item.abstractPhysicalObject.pos = room.GetWorldCoordinate(item.firstChunk.pos);
        item.graphicsModule?.Reset();
    }

    private static bool TryFindWakeTile(Room room, IntVector2 origin, out IntVector2 tile)
    {
        tile = default;
        if (room.TileWidth < 3 || room.TileHeight < 3)
            return false;

        origin = new IntVector2(Mathf.Clamp(origin.x, 1, room.TileWidth - 2), Mathf.Clamp(origin.y, 1, room.TileHeight - 2));

        if (IsWakeTile(room, origin))
        {
            tile = origin;
            return true;
        }

        Queue<IntVector2> queue = new();
        bool[,] visited = new bool[room.TileWidth, room.TileHeight];
        queue.Enqueue(origin);
        visited[origin.x, origin.y] = true;

        while (queue.Count > 0)
        {
            IntVector2 current = queue.Dequeue();
            if (IsWakeTile(room, current))
            {
                tile = current;
                return true;
            }

            for (int i = 0; i < directions.Length; i++)
            {
                IntVector2 next = new(current.x + directions[i].x, current.y + directions[i].y);
                if (next.x < 1 || next.x >= room.TileWidth - 1 || next.y < 1 || next.y >= room.TileHeight - 1
                    || visited[next.x, next.y])
                    continue;

                visited[next.x, next.y] = true;
                Room.Tile roomTile = room.GetTile(next);
                if (roomTile.Solid || roomTile.Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                    continue;

                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static bool IsWakeTile(Room room, IntVector2 tile)
    {
        if (tile.x < 1 || tile.x >= room.TileWidth - 1 || tile.y < 1 || tile.y >= room.TileHeight - 1)
            return false;

        for (int x = tile.x - 1; x <= tile.x + 1; x++)
        {
            if (!room.GetTile(x, tile.y - 1).Solid
                || room.GetTile(x, tile.y).Terrain != Room.Tile.TerrainType.Air
                || room.GetTile(x, tile.y + 1).Terrain != Room.Tile.TerrainType.Air)
                return false;
        }

        Vector2 center = room.MiddleOfTile(tile);
        return !room.PointSubmerged(center + new Vector2(0f, -9f))
            && !room.PointSubmerged(center + new Vector2(0f, 19f));
    }
}
