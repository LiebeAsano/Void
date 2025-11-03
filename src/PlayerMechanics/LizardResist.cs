using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Creatures;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

public static class LizardResist
{
    public static void Hook()
    {
        On.Lizard.Bite += Lizard_Bite;
    }

    private static void Lizard_Bite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (chunk.owner is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            float resist;
            if (player.KarmaCap == 10)
                resist = Karma11Update.VoidKarma11 ? 0.3f : 0;
            else
                resist = (player.KarmaCap + 1) * 0.02f;

            if (self.Template.type == CreatureTemplate.Type.RedLizard)
            {
                if (Karma11Update.VoidKarma11)
                {
                    self.lizardParams.biteDamageChance = 0.5f;
                }
                else
                {
                    self.lizardParams.biteDamageChance = 1f - resist;
                }
            }
            else if (self.Template.type == CreatureTemplateType.LWIceLizard || self.Template.type == Watcher.WatcherEnums.CreatureTemplateType.BlizzardLizard)
            {
                self.lizardParams.biteDamageChance = 0.8f - resist;
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
