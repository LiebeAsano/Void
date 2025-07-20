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
            if (self.abstractCreature.world.name == "SI")
            {
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(self.abstractCreature.ID.RandomSeed);

                Color b = Color.Lerp(new Color(0.5f, 0.25f, 0.1f), new Color(0.6f, 0.5f, 0.4f), UnityEngine.Random.value);
                Color color = Color.Lerp(Color.white, b, Mathf.Pow(UnityEngine.Random.value, 2f) * 0.17f);
                color = Color.Lerp(color, new Color(1f, 0.7f, 0f), (self.abstractCreature.personality.dominance - Mathf.Pow(UnityEngine.Random.value, 2f)) * 0.1f);

                float orangeShift = UnityEngine.Random.Range(0.05f, 0.1f);
                color.r = Mathf.Clamp01(color.r + orangeShift * 3.0f);
                color.g = Mathf.Clamp01(color.g + orangeShift * 0.6f);
                color.b = Mathf.Clamp01(color.b - orangeShift * 0.4f);

                Color secondaryColor = Color.Lerp(
                    Color.Lerp(color, new HSLColor(UnityEngine.Random.value * 0.17f, 1f, 0.5f).rgb, UnityEngine.Random.value * 0.4f),
                    Color.black,
                    0.3f
                );

                float lavenderShift = UnityEngine.Random.Range(0.3f, 0.6f);
                secondaryColor.r = Mathf.Clamp01(color.r + lavenderShift * 1.2f);
                secondaryColor.b = Mathf.Clamp01(color.b + lavenderShift * 2.4f);
                secondaryColor.g = Mathf.Clamp01(color.g - lavenderShift * 0.6f);

                self.iVars.bodyColor = color;
                self.iVars.secondaryColor = secondaryColor;
                UnityEngine.Random.state = state;
            }
            if (self.abstractCreature.world.name == "LF")
            {
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(self.abstractCreature.ID.RandomSeed);

                Color b = Color.Lerp(new Color(0.5f, 0.25f, 0.1f), new Color(0.6f, 0.5f, 0.4f), UnityEngine.Random.value);
                Color color = Color.Lerp(Color.white, b, Mathf.Pow(UnityEngine.Random.value, 2f) * 0.17f);
                color = Color.Lerp(color, new Color(1f, 0.7f, 0f), (self.abstractCreature.personality.dominance - Mathf.Pow(UnityEngine.Random.value, 2f)) * 0.1f);

                Color secondaryColor = Color.Lerp(
                    Color.Lerp(color, new HSLColor(UnityEngine.Random.value * 0.17f, 1f, 0.5f).rgb,
                    UnityEngine.Random.value * 0.4f),
                    Color.black,
                    0.3f
                );

                float greyShift = UnityEngine.Random.Range(0.05f, 0.1f);
                color.r = Mathf.Clamp01(color.r - greyShift * 2.0f);
                color.g = Mathf.Clamp01(color.g - greyShift * 2.0f);
                color.b = Mathf.Clamp01(color.b - greyShift * 2.0f);

                float jadeShift = UnityEngine.Random.Range(0.3f, 0.6f);
                secondaryColor.g = Mathf.Clamp01(color.g + jadeShift * 2.4f);
                secondaryColor.b = Mathf.Clamp01(color.b + jadeShift * 1.2f);
                secondaryColor.r = Mathf.Clamp01(color.r - jadeShift * 0.6f);

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
