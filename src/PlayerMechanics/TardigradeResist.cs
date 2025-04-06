using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;
using Watcher;

namespace VoidTemplate.PlayerMechanics;

internal class TardigradeResist
{
    public static void Hook()
    {
        On.Watcher.Tardigrade.BitByPlayer += Tardigrade_BitByPlayer;
    }

    private static void Tardigrade_BitByPlayer(On.Watcher.Tardigrade.orig_BitByPlayer orig, Watcher.Tardigrade self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && (player.IsVoid() || player.IsViy()))
        {
            Creature grabber = grasp.grabber;
            Vector3 vector = Custom.RGB2HSL((self.BitesLeft == 3) ? self.iVars.secondaryColor : self.iVars.bodyColor);
            (self.State as Tardigrade.TardigradeState).bites--;
            self.room.PlaySound((self.BitesLeft == 0) ? SoundID.Slugcat_Eat_Slime_Mold : SoundID.Slugcat_Bite_Slime_Mold, self.firstChunk);
            self.firstChunk.MoveFromOutsideMyUpdate(eu, grabber.mainBodyChunk.pos);
            if (self.BitesLeft <= 1 && !self.dead)
            {
                self.Die();
            }
            if (self.BitesLeft < 1)
            {
                (grabber as Player).ObjectEaten(self);
                grasp.Release();
                self.Destroy();
            }
        }
        else
        {
            orig(self, grasp, eu);
        }
    }
}
