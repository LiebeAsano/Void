using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class ExplosiveResist
{
    public static void Hook()
    {
        On.Creature.Violence += Creature_Violence;
    }

    private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player player && player.IsVoid() && type == Creature.DamageType.Explosion)
        {
            int Karma = player.KarmaCap;
            if (Karma == 10)
                if (Karma11Update.VoidKarma11)
                    Karma = 10;
                else
                    Karma = 0;
            stunBonus *= 1f - 0.066f * (Karma + 1);
            damage *= 1f - 0.066f * (Karma + 1);
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
}
