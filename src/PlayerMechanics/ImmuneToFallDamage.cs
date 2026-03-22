using RWCustom;
using System;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class ImmuneToFallDamage
{
    public static void Hook()
    {
        On.Player.TerrainImpact += Player_TerrainImpact;
    }

    private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        if (self.AreVoidViy() && !HasTempleGuard(self.room))
        {
            int old = self.immuneToFallDamage;
            self.immuneToFallDamage = Math.Max(old, 1);

            orig(self, chunk, direction, speed, firstContact);

            self.immuneToFallDamage = old;
            return;
        }

        orig(self, chunk, direction, speed, firstContact);
    }

    private static bool HasTempleGuard(Room room)
    {
        if (room?.abstractRoom?.creatures == null)
            return false;

        var creatures = room.abstractRoom.creatures;
        for (int i = 0; i < creatures.Count; i++)
        {
            var creature = creatures[i];
            if (creature?.creatureTemplate?.type == CreatureTemplate.Type.TempleGuard)
                return true;
        }

        return false;
    }
}
