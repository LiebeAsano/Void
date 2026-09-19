using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class PostCycleSleep
{
    public const int FarRooms = 5;
    private const int CheckInterval = 40;

    private sealed class WorldState
    {
        public readonly HashSet<(int room, int index)> flowersBeforeSave = [];
        public readonly Dictionary<int, HashSet<int>> pendingRegrowth = [];
        public readonly Dictionary<int, HashSet<int>> armedRegrowth = [];
        public readonly HashSet<AbstractCreature> corpses = [];
        public readonly Dictionary<int, int> respawns = [];
        public GhostWorldPresence appearingGhost;
        public GhostWorldPresence vanishingGhost;
        public int timer;

        public bool Idle => pendingRegrowth.Count == 0 && corpses.Count == 0 && respawns.Count == 0
            && appearingGhost == null && vanishingGhost == null;
    }

    private static readonly ConditionalWeakTable<World, WorldState> states = new();

    public static void Hook()
    {
        IL.Room.Loaded += Room_LoadedIL;
        On.Room.Loaded += Room_Loaded;
        On.RainWorldGame.Update += RainWorldGame_Update;
    }

    public static void BeforeSave(World world)
    {
        WorldState state = states.GetValue(world, _ => new WorldState());

        state.flowersBeforeSave.Clear();

        foreach (RegionState.ConsumedItem flower in world.game.GetStorySession.saveState.deathPersistentSaveData.consumedFlowers)
        {
            if (world.IsRoomInRegion(flower.originRoom))
                state.flowersBeforeSave.Add((flower.originRoom, flower.placedObjectIndex));
        }
    }

    public static void AfterSave(World world)
    {
        WorldState state = states.GetValue(world, _ => new WorldState());
        SaveState saveState = world.game.GetStorySession.saveState;

        if (world.regionState is RegionState regionState)
        {
            HashSet<(int room, int index)> expired = [.. regionState.consumedItems.Select(item => (item.originRoom, item.placedObjectIndex))];

            regionState.RainCycleTick(saveState.cycleNumber - regionState.lastCycleUpdated, saveState.deathPersistentSaveData.foodReplenishBonus);

            foreach (RegionState.ConsumedItem item in regionState.consumedItems)
                expired.Remove((item.originRoom, item.placedObjectIndex));

            foreach ((int room, int index) in expired)
                AddRegrowth(world, state, room, index);
        }

        foreach (RegionState.ConsumedItem flower in saveState.deathPersistentSaveData.consumedFlowers)
            state.flowersBeforeSave.Remove((flower.originRoom, flower.placedObjectIndex));

        foreach ((int room, int index) in state.flowersBeforeSave)
            AddRegrowth(world, state, room, index);

        state.flowersBeforeSave.Clear();

        CollectCorpses(world, state);
        CollectRespawns(world, state, saveState);
        ReevaluateGhost(world, state);

        Process(world, state);
    }

    private static void AddRegrowth(World world, WorldState state, int roomIndex, int placedObjectIndex)
    {
        if (placedObjectIndex < 0 || !world.IsRoomInRegion(roomIndex))
            return;

        AbstractRoom room = world.GetAbstractRoom(roomIndex);

        if (room == null || room.firstTimeRealized)
            return;

        if (!state.pendingRegrowth.TryGetValue(roomIndex, out HashSet<int> indices))
            state.pendingRegrowth[roomIndex] = indices = [];

        indices.Add(placedObjectIndex);
    }

    private static void CollectCorpses(World world, WorldState state)
    {
        foreach (AbstractRoom room in world.abstractRooms)
        {
            if (room.shelter)
                continue;

            foreach (AbstractCreature creature in room.creatures)
            {
                if (IsCorpse(world, creature))
                    state.corpses.Add(creature);
            }

            foreach (AbstractWorldEntity entity in room.entitiesInDens)
            {
                if (entity is AbstractCreature creature && IsCorpse(world, creature))
                    state.corpses.Add(creature);
            }
        }
    }

    private static bool IsCorpse(World world, AbstractCreature creature)
    {
        return creature.state.dead
            && !creature.slatedForDeletion
            && creature.creatureTemplate.type != CreatureTemplate.Type.Slugcat
            && !world.game.Players.Contains(creature);
    }

    private static void CollectRespawns(World world, WorldState state, SaveState saveState)
    {
        state.respawns.Clear();

        foreach (World.CreatureSpawner spawner in world.spawners)
        {
            int count = saveState.respawnCreatures.Count(id => id == spawner.SpawnerID);

            if (count == 0)
                continue;

            if (spawner is World.SimpleSpawner simple)
            {
                CreatureTemplate template = StaticWorld.GetCreatureTemplate(simple.creatureType);

                if (template != null && !template.quantified && template.saveCreature)
                    state.respawns[spawner.SpawnerID] = Mathf.Min(count, simple.amount);
            }
            else if (spawner is World.Lineage)
                state.respawns[spawner.SpawnerID] = 1;
        }
    }

    private static void ReevaluateGhost(World world, WorldState state)
    {
        GhostWorldPresence existing = world.worldGhost;

        world.worldGhost = null;
        world.SpawnGhost();

        GhostWorldPresence fresh = world.worldGhost;

        if (fresh != null)
            world.migrationInfluences.Remove(fresh);

        world.worldGhost = existing;

        state.appearingGhost = existing == null ? fresh : null;
        state.vanishingGhost = existing != null && fresh == null ? existing : null;
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (self.world == null || !states.TryGetValue(self.world, out WorldState state) || state.Idle || ++state.timer < CheckInterval)
            return;

        state.timer = 0;
        Process(self.world, state);
    }

    private static void Process(World world, WorldState state)
    {
        if (state.Idle)
            return;

        HashSet<int> near = NearRooms(world);

        ArmRegrowth(world, state, near);
        RemoveCorpses(world, state, near);
        SpawnRespawns(world, state, near);
        UpdateGhost(world, state, near);
    }

    private static HashSet<int> NearRooms(World world)
    {
        HashSet<int> near = [];
        Queue<(int room, int depth)> queue = new();

        foreach (AbstractCreature player in world.game.Players)
        {
            if (player?.Room is AbstractRoom room && room.world == world && near.Add(room.index))
                queue.Enqueue((room.index, 0));
        }

        while (queue.Count > 0)
        {
            (int roomIndex, int depth) = queue.Dequeue();

            if (depth + 1 >= FarRooms || world.GetAbstractRoom(roomIndex) is not AbstractRoom room)
                continue;

            foreach (int connection in room.connections)
            {
                if (connection > -1 && near.Add(connection))
                    queue.Enqueue((connection, depth + 1));
            }
        }

        return near;
    }

    private static void ArmRegrowth(World world, WorldState state, HashSet<int> near)
    {
        foreach (int roomIndex in state.pendingRegrowth.Keys.ToList())
        {
            AbstractRoom room = world.GetAbstractRoom(roomIndex);

            if (room == null)
            {
                state.pendingRegrowth.Remove(roomIndex);
                continue;
            }

            if (near.Contains(roomIndex) || room.realizedRoom != null)
                continue;

            HashSet<int> indices = state.pendingRegrowth[roomIndex];
            HashSet<string> origins = [.. indices.Select(index => room.name + ":" + index)];

            for (int i = room.entities.Count - 1; i >= 0; i--)
            {
                if (room.entities[i] is AbstractPhysicalObject obj
                    && obj.realizedObject == null
                    && obj.placedObjectOrigin != null
                    && origins.Contains(obj.placedObjectOrigin)
                    && !ConnectedToPlayer(world, obj))
                    Remove(room, obj);
            }

            if (state.armedRegrowth.TryGetValue(roomIndex, out HashSet<int> armed))
                armed.UnionWith(indices);
            else
                state.armedRegrowth[roomIndex] = indices;

            state.pendingRegrowth.Remove(roomIndex);
        }
    }

    private static void RemoveCorpses(World world, WorldState state, HashSet<int> near)
    {
        foreach (AbstractCreature corpse in state.corpses.ToList())
        {
            AbstractRoom room = corpse.Room;

            if (corpse.slatedForDeletion || !corpse.state.dead || corpse.world != world || room == null || room.shelter)
            {
                state.corpses.Remove(corpse);
                continue;
            }

            if (near.Contains(room.index) || room.realizedRoom != null || corpse.realizedCreature != null
                || ConnectedToPlayer(world, corpse))
                continue;

            Remove(room, corpse);
            world.regionState?.loadedCreatures.Remove(corpse);
            state.corpses.Remove(corpse);
        }
    }

    private static void SpawnRespawns(World world, WorldState state, HashSet<int> near)
    {
        foreach (int spawnerId in state.respawns.Keys.ToList())
        {
            World.CreatureSpawner spawner = world.spawners.FirstOrDefault(s => s.SpawnerID == spawnerId);
            AbstractRoom den = spawner == null ? null : world.GetAbstractRoom(spawner.den);

            if (den != null && (near.Contains(den.index) || den.realizedRoom != null))
                continue;

            if (spawner != null)
                Respawn(world, spawner, den, state.respawns[spawnerId]);

            state.respawns.Remove(spawnerId);
        }
    }

    private static void Respawn(World world, World.CreatureSpawner spawner, AbstractRoom den, int count)
    {
        SaveState saveState = world.game.GetStorySession.saveState;
        int removed = 0;

        for (int i = saveState.respawnCreatures.Count - 1; i >= 0 && removed < count; i--)
        {
            if (saveState.respawnCreatures[i] == spawner.SpawnerID)
            {
                saveState.respawnCreatures.RemoveAt(i);
                removed++;
            }
        }

        if (removed == 0)
            return;

        world.regionState?.loadedCreatures.RemoveAll(creature => creature.ID.spawner == spawner.SpawnerID && creature.state.dead);

        bool validDen = den != null
            && spawner.den.abstractNode >= 0
            && spawner.den.abstractNode < den.nodes.Length
            && (den.nodes[spawner.den.abstractNode].type == AbstractRoomNode.Type.Den
                || den.nodes[spawner.den.abstractNode].type == AbstractRoomNode.Type.GarbageHoles);

        if (spawner is World.SimpleSpawner simple)
        {
            if (!validDen)
                return;

            for (int i = 0; i < removed; i++)
                SpawnInDen(world, den, spawner, simple.creatureType, simple.spawnDataString);
        }
        else if (spawner is World.Lineage lineage)
        {
            CreatureTemplate.Type type = lineage.CurrentType(saveState);

            if (type == null)
            {
                lineage.ChanceToProgress(world);
                saveState.respawnCreatures.Add(lineage.SpawnerID);
            }
            else if (validDen)
                SpawnInDen(world, den, spawner, type, lineage.CurrentSpawnData(saveState));
        }
    }

    private static void SpawnInDen(World world, AbstractRoom den, World.CreatureSpawner spawner, CreatureTemplate.Type type, string spawnData)
    {
        AbstractCreature creature = new(world, StaticWorld.GetCreatureTemplate(type), null, spawner.den, world.game.GetNewID(spawner.SpawnerID))
        {
            spawnData = spawnData,
            nightCreature = spawner.nightCreature
        };

        creature.setCustomFlags();
        den.MoveEntityToDen(creature);
    }

    private static void UpdateGhost(World world, WorldState state, HashSet<int> near)
    {
        if (state.appearingGhost is GhostWorldPresence appearing && IsFar(appearing.ghostRoom, near))
        {
            if (world.worldGhost == null)
            {
                world.worldGhost = appearing;
                world.migrationInfluences.Add(appearing);
            }

            state.appearingGhost = null;
        }

        if (state.vanishingGhost is GhostWorldPresence vanishing && IsFar(vanishing.ghostRoom, near)
            && vanishing.ghostRoom?.realizedRoom == null)
        {
            if (world.worldGhost == vanishing)
            {
                world.worldGhost = null;
                world.migrationInfluences.Remove(vanishing);
            }

            state.vanishingGhost = null;
        }
    }

    private static bool IsFar(AbstractRoom room, HashSet<int> near) => room == null || !near.Contains(room.index);

    private static bool ConnectedToPlayer(World world, AbstractPhysicalObject obj)
    {
        foreach (AbstractPhysicalObject connected in obj.GetAllConnectedObjects())
        {
            if (connected is AbstractCreature creature && world.game.Players.Contains(creature))
                return true;
        }

        return false;
    }

    private static void Remove(AbstractRoom room, AbstractPhysicalObject obj)
    {
        obj.LoseAllStuckObjects();
        room.RemoveEntity(obj);
        obj.Destroy();
    }

    private static HashSet<int> ArmedRegrowth(Room room)
    {
        if (room.world == null || room.abstractRoom.firstTimeRealized || !states.TryGetValue(room.world, out WorldState state))
            return null;

        return state.armedRegrowth.TryGetValue(room.abstractRoom.index, out HashSet<int> armed) ? armed : null;
    }

    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        HashSet<int> armed = ArmedRegrowth(self);

        orig(self);

        if (armed != null && states.TryGetValue(self.world, out WorldState state))
            state.armedRegrowth.Remove(self.abstractRoom.index);
    }

    private static void Room_LoadedIL(ILContext il)
    {
        ILCursor c = new(il);
        int index = -1;

        if (c.TryGotoNext(x => x.MatchLdloc(out index), x => x.MatchCall<Room>(nameof(Room.CheckForWarpedObjects)))
            && c.TryGotoPrev(MoveType.After, x => x.MatchLdfld<AbstractRoom>(nameof(AbstractRoom.firstTimeRealized))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool firstTime, Room room) => firstTime || ArmedRegrowth(room) != null);

            c.GotoNext(MoveType.After, x => x.MatchCall<Room>(nameof(Room.CheckForWarpedObjects)));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, il.Body.Variables[index]);
            c.EmitDelegate((bool skip, Room room, int placedObjectIndex) =>
                skip || ArmedRegrowth(room) is HashSet<int> armed && !armed.Contains(placedObjectIndex));

            if (c.TryGotoNext(x => x.MatchCallvirt<RoomSettings>("get_RandomItemDensity"))
                && c.TryGotoPrev(MoveType.After, x => x.MatchCallvirt<AbstractRoom>("get_shelter")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((bool shelter, Room room) => shelter || ArmedRegrowth(room) != null);
            }
            else Utils.LogExErr("PostCycleSleep: random item spawning not found in Room.Loaded");
        }
        else Utils.LogExErr("PostCycleSleep: placed object spawning not found in Room.Loaded");
    }
}
