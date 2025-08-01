using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;
using System;
using System.Reflection;

namespace VoidTemplate.Creatures
{
    internal class IceLizardHooks
    {
        public static void Hook()
        {
            On.LizardGraphics.GenerateIvars += LizardGraphics_GenerateIvars;
            On.LizardTongue.ctor += LizardTongue_ctor;
            On.LizardGraphics.DynamicBodyColor += LizardGraphics_DynamicBodyColor;
            On.LizardGraphics.BodyColor += LizardGraphics_BodyColor;
            HeadColor12Hook();
        }

        private static Color LizardGraphics_BodyColor(On.LizardGraphics.orig_BodyColor orig, LizardGraphics self, float f)
        {
            if (self is IceLizardGraphics)
            {
                return self.DynamicBodyColor(f);
            }
            return orig(self, f);
        }

        private static Color LizardGraphics_DynamicBodyColor(On.LizardGraphics.orig_DynamicBodyColor orig, LizardGraphics self, float f)
        {
            if (self is IceLizardGraphics)
            {
                return Color.Lerp(self.palette.blackColor, self.whiteCamoColor, self.whiteCamoColorAmount);
            }
            return orig(self, f);
        }

        public static void HeadColor12Hook()
        {
            new Hook(typeof(LizardGraphics).GetMethod("get_HeadColor1", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), new Func<Func<LizardGraphics, Color>, LizardGraphics, Color> ((Func<LizardGraphics, Color> orig, LizardGraphics self) =>
            {
                if (self is IceLizardGraphics ice)
                {
                    return Color.Lerp(self.palette.blackColor, self.whiteCamoColor, self.whiteCamoColorAmount);
                }
                return orig(self);
            }));
            new Hook(typeof(LizardGraphics).GetMethod("get_HeadColor2", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), new Func<Func<LizardGraphics, Color>, LizardGraphics, Color>((Func<LizardGraphics, Color> orig, LizardGraphics self) =>
            {
                if (self is IceLizardGraphics ice)
                {
                    return Color.Lerp(self.effectColor, self.whiteCamoColor, self.whiteCamoColorAmount);
                }
                return orig(self);
            }));
        }

        private static void LizardTongue_ctor(On.LizardTongue.orig_ctor orig, LizardTongue self, Lizard lizard)
        {
            orig(self, lizard);
            if (lizard is IceLizard)
            {
                self.range = 540f;
                self.elasticRange = 0.5f;
                self.lashOutSpeed = 37f;
                self.reelInSpeed = 0.0043333336f;
                self.chunkDrag = 0f;
                self.terrainDrag = 0f;
                self.dragElasticity = 0.05f;
                self.emptyElasticity = 0.01f;
                self.involuntaryReleaseChance = 1f;
                self.voluntaryReleaseChance = 1f;
            }
        }

        private static LizardGraphics.IndividualVariations LizardGraphics_GenerateIvars(On.LizardGraphics.orig_GenerateIvars orig, LizardGraphics self)
        {
            var iVars = orig(self);
            if (self is IceLizardGraphics)
            {
                iVars.fatness = Mathf.Min(1f, iVars.fatness);
                iVars.tailFatness = Mathf.Min(1f, iVars.tailFatness);
            }
            return iVars;
        }
    }
}
