using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics;

internal static class LizardResist
{
    public static void Hook()
    {
        On.Lizard.Bite += Lizard_Bite;
    }

    private static void Lizard_Bite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (chunk.owner is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            float resist = player.KarmaCap * 0.05f;
            if (self.Template.type == CreatureTemplate.Type.RedLizard)
            {
                if (player.KarmaCap == 10)
                {
                    self.lizardParams.biteDamageChance = 0.5f;
                }
                else
                {
                    self.lizardParams.biteDamageChance = 1f;
                }
            }
            else
            {
                self.lizardParams.biteDamageChance = self.lizardParams.biteDamageChance - resist;
                if (self.lizardParams.biteDamageChance < 0)
                {
                    self.lizardParams.biteDamageChance = 0f;
                }
            }
        }
        orig(self, chunk);
    }
}
