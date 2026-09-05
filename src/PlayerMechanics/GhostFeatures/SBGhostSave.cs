using System;
using System.Collections.Generic;
using MoreSlugcats;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

public static class SBGhostSave
{
    public static void Hook()
    {
        On.SaveState.GhostEncounter += SaveState_GhostEncounter;
        On.Ghost.FadeOutFinished += Ghost_FadeOutFinished;
    }

    private static void SaveState_GhostEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
    {
        orig(self, ghost, rainWorld);
        if (self.saveStateNumber == VoidEnums.SlugcatID.Void)
            self.progression.SaveWorldStateAndProgression(false);
    }

    private static void Ghost_FadeOutFinished(On.Ghost.orig_FadeOutFinished orig, Ghost self)
    {
        RainWorldGame game = self.room?.game;

        if (game == null || !game.IsVoidStoryCampaign())
        {
            orig(self);
            return;
        }

        if (ModManager.MSC && self.worldGhost.ghostID == MoreSlugcatsEnums.GhostID.MS)
        {
            orig(self);
            return;
        }

        World world = self.room.world;

        AbstractRoom nearestShelter = FindNearestShelter(world, self.room.abstractRoom);

        orig(self);

        RainWorldGame.ForceSaveNewDenLocation(game, nearestShelter?.name, false);
    }

    private static AbstractRoom FindNearestShelter(World world, AbstractRoom startRoom)
    {
        Queue<AbstractRoom> queue = [];
        HashSet<int> visited = [];

        queue.Enqueue(startRoom);
        visited.Add(startRoom.index);

        while (queue.Count > 0)
        {
            AbstractRoom room = queue.Dequeue();

            if (IsValidShelter(room))
                return room;

            for (int i = 0; i < room.connections.Length; i++)
            {
                int connection = room.connections[i];

                if (connection < 0 || !visited.Add(connection))
                    continue;

                AbstractRoom nextRoom = world.GetAbstractRoom(connection);

                if (nextRoom != null) queue.Enqueue(nextRoom);
            }
        }

        return null;
    }

    private static bool IsValidShelter(AbstractRoom room)
    {
        if (!room.shelter)
            return false;

        return !room.world.brokenShelters[room.shelterIndex];
    }
}