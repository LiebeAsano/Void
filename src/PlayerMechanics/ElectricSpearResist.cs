using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class ElectricSpearResist
{
    public static void Hook()
    {
        On.MoreSlugcats.ElectricSpear.Electrocute += ElectricSpear_Electrocute;
    }

    private static void ElectricSpear_Electrocute(On.MoreSlugcats.ElectricSpear.orig_Electrocute orig, MoreSlugcats.ElectricSpear self, PhysicalObject otherObject)
    {
        if (otherObject is Player player && (player.IsVoid() || player.IsViy()))
        {
            self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos);
            self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
            return;
        }
        orig(self, otherObject);
    }
}
