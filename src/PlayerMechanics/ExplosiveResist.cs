using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics;

internal static class ExplosiveResist
{
    public static void Hook()
    {
        On.Creature.Violence += Creature_Violence;
    }

    private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void && type == Creature.DamageType.Explosion)
        {
            int Karma = player.KarmaCap;
            float StunResist = 1f - 0.035f * Karma;
            float DamageResist = 1f - 0.035f * Karma;
            stunBonus *= StunResist;
            damage *= DamageResist;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
}
