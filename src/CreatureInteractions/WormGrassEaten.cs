using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

internal static class WormGrassEatenHook
{
    public static void Hook()
    {
        On.WormGrass.Worm.Attached += Worm_Attached;
    }

    private static void Worm_Attached(On.WormGrass.Worm.orig_Attached orig, WormGrass.Worm self)
    {
        orig(self);

        if (self.attachedChunk?.owner is Player player &&
            player.IsVoid() &&
            self.patch != null &&
            self.patch.trackedCreatures != null)
        {
            var creaturePull = self.patch.trackedCreatures.Find(c => c?.creature == player);
            if (creaturePull != null && creaturePull.bury >= 1f)
            {
                RemoveGrassPatchImmediately(self.patch);
            }
        }
    }

    private static void RemoveGrassPatchImmediately(WormGrass.WormGrassPatch patch)
    {

        if (patch == null || patch.wormGrass == null || patch.wormGrass.room == null)
            return;

        for (int i = patch.worms.Count - 1; i >= 0; i--)
        {
            patch.worms[i]?.Destroy();
        }

        foreach (var tile in patch.tiles)
        {
            if (patch.wormGrass.room.GetTile(tile).wormGrass)
            {
                patch.wormGrass.room.GetTile(tile).wormGrass = false;
                patch.wormGrass.room.MiddleOfTile(tile);
            }
        }

        patch.wormGrass.patches?.Remove(patch);

    }
}
