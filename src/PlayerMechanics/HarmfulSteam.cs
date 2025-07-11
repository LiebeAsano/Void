using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class HarmfulSteam
{
    public static void Hook()
    {
        On.HarmfulSteam.Update += HarmfulSteam_Update;
    }

    private static void HarmfulSteam_Update(On.HarmfulSteam.orig_Update orig, global::HarmfulSteam self, bool eu)
    {
        orig(self, eu);
        for (int i = 0; i < self.steam.particles.Count; i++)
        {
            if (self.steam.particles[i].life > self.dangerRange)
            {
                for (int j = 0; j < self.room.physicalObjects.Length; j++)
                {
                    for (int k = 0; k < self.room.physicalObjects[j].Count; k++)
                    {
                        for (int l = 0; l < self.room.physicalObjects[j][k].bodyChunks.Length; l++)
                        {
                            Vector2 a = self.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.ToVector2();
                            Vector2 b = self.room.physicalObjects[j][k].bodyChunks[l].pos + a * (self.room.physicalObjects[j][k].bodyChunks[l].rad + 30f);
                            if (Vector2.Distance(self.steam.particles[i].pos, b) < 10f && self.room.physicalObjects[j][k] is Player player && player.abstractCreature.rippleLayer == 0 && player.AreVoidViy() && player.playerState.permanentDamageTracking == 0)
                            {
                                player.stun = 0;
                                for (int m = 0; m < self.room.updateList.Count; m++)
                                {
                                    if (self.room.updateList[m] is CreatureSpasmer spasmer && spasmer.crit == self.room.physicalObjects[j][k] as Creature)
                                    {
                                        spasmer.Destroy();
                                        break;
                                    }
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
