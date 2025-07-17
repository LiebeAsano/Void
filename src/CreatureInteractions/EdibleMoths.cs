using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.CreatureInteractions
{
    public class EdibleMoths
    {
        public static void Hook()
        {
            On.StaticWorld.InitSmallMoth += StaticWorld_InitSmallMoth;
            On.Watcher.BigMoth.GenerateIVars += BigMoth_GenerateIVars;
        }

        private static void BigMoth_GenerateIVars(On.Watcher.BigMoth.orig_GenerateIVars orig, Watcher.BigMoth self)
        {
            orig(self);
            if (self.Small && !Region.IsAncientUrbanRegion(self.abstractCreature.world.name))
            {
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(self.abstractCreature.ID.RandomSeed);
                Color b = Color.Lerp(new Color(0.5f, 0.25f, 0.1f), new Color(0.6f, 0.5f, 0.4f), UnityEngine.Random.value);
                Color color = Color.Lerp(Color.white, b, Mathf.Pow(UnityEngine.Random.value, 2f) * 0.17f);
                color = Color.Lerp(color, new Color(1f, 0.7f, 0f), (self.abstractCreature.personality.dominance - Mathf.Pow(UnityEngine.Random.value, 2f)) * 0.1f);
                Color secondaryColor = Color.Lerp(Color.Lerp(color, new HSLColor(UnityEngine.Random.value * 0.17f, 1f, 0.5f).rgb, UnityEngine.Random.value * 0.4f), Color.black, 0.3f);
                self.iVars.bodyColor = color;
                self.iVars.secondaryColor = secondaryColor;
                UnityEngine.Random.state = state;
            }
        }

        private static void StaticWorld_InitSmallMoth(On.StaticWorld.orig_InitSmallMoth orig, List<CreatureTemplate> tempCreatureTemplates, CreatureTemplate bigMothTemplate, CreatureTemplate batTemplate)
        {
            orig(tempCreatureTemplates, bigMothTemplate, batTemplate);
            bigMothTemplate.meatPoints = 8;
            tempCreatureTemplates[tempCreatureTemplates.Count - 1].meatPoints = 2;
        }

    }
}
