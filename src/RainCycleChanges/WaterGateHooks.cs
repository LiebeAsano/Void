using System;
using System.Collections.Generic;
using System.IO;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class WaterGateHooks
{
    public static void Hook()
    {
        On.WaterGate.ctor += WaterGate_ctor;
        On.WaterGate.WaterRunning += WaterGate_WaterRunning;
        On.WaterGate.Update += WaterGate_Update;
    }

    private static void WaterGate_ctor(On.WaterGate.orig_ctor orig, WaterGate self, Room room)
    {
        orig(self, room);

        if (!IsPostRain(room))
            return;

        self.waterLeft = 1f;
        ResetPassedState(room);
    }

    private static void WaterGate_WaterRunning(
        On.WaterGate.orig_WaterRunning orig,
        WaterGate self,
        float flow)
    {
        if (IsPostRain(self.room))
            return;

        orig(self, flow);
    }

    private static void WaterGate_Update(On.WaterGate.orig_Update orig, WaterGate self, bool eu)
    {
        bool unlimited = IsPostRain(self.room);

        if (unlimited && self.waterLeft < 1f) self.waterLeft = 1f;

        orig(self, eu);

        if (!unlimited)
            return;

        if (self.mode == RegionGate.Mode.Closed && self.AllDoorsInPosition())
        {
            self.waterLeft = 1f;
            ResetPassedState(self.room);
            self.mode = RegionGate.Mode.MiddleClosed;
        }
    }

    private static bool IsPostRain(Room room)
    {
        if (room?.game?.IsVoidStoryCampaign() != true || room.world?.rainCycle == null) return false;

        return room.world.rainCycle.GetRainCycleExt().PostCycleStarted;
    }

    private static void ResetPassedState(Room room)
    {
        if (room?.world?.regionState?.gatesPassedThrough == null)
            return;

        int index = room.abstractRoom.gateIndex;
        bool[] gates = room.world.regionState.gatesPassedThrough;

        if (index >= 0 && index < gates.Length)
        {
            gates[index] = false;
        }
    }

    public static void RestoreAfterPostRain(World world)
    {
        if (world?.game?.IsVoidStoryCampaign() != true)
            return;

        ResetWaterGateFlags(world);

        for (int i = 0; i < world.activeRooms.Count; i++)
        {
            Room room = world.activeRooms[i];

            if (room?.regionGate is not WaterGate gate) continue;

            gate.waterLeft = 1f;
            ResetPassedState(room);

            if (gate.mode == RegionGate.Mode.Closed && gate.AllDoorsInPosition()) gate.mode = RegionGate.Mode.MiddleClosed;
            
        }
    }

    private static void ResetWaterGateFlags(World world)
    {
        RegionState state = world.regionState;

        if (state?.gatesPassedThrough == null || world.gates == null) return;
       
        string egatesPath = AssetManager.ResolveFilePath(
            "World" +
            Path.DirectorySeparatorChar +
            "Gates" +
            Path.DirectorySeparatorChar +
            "Egates.txt");

        HashSet<string> electricGates = new(File.ReadAllLines(egatesPath), StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < world.gates.Length; i++)
        {
            AbstractRoom room = world.GetAbstractRoom(world.gates[i]);

            if (room == null || electricGates.Contains(room.name)) continue;

            int gateIndex = room.gateIndex;

            if (gateIndex >= 0 && gateIndex < state.gatesPassedThrough.Length)
                state.gatesPassedThrough[gateIndex] = false;
            
        }
    }
}