using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class ElectricSpearResist
{
    public static void Hook()
    {
        On.MoreSlugcats.ElectricSpear.Electrocute += ElectricSpear_Electrocute;
    }

    private static void ElectricSpear_Electrocute(On.MoreSlugcats.ElectricSpear.orig_Electrocute orig, MoreSlugcats.ElectricSpear self, PhysicalObject otherObject)
    {
        if (otherObject is not Player player || !player.AreVoidViy())
        {
            orig(self, otherObject);
            return;
        }

        var pos = self.firstChunk.pos;
        self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, pos);
        self.room.AddObject(new Explosion.ExplosionLight(pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
    }
}
