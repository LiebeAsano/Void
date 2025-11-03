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
    internal class LWIceLizardHooks
    {
        public static void Hook()
        {
            On.LizardGraphics.GenerateIvars += LizardGraphics_GenerateIvars;
            On.LizardGraphics.DynamicBodyColor += LizardGraphics_DynamicBodyColor;
            On.LizardGraphics.BodyColor += LizardGraphics_BodyColor;
            HeadColor12Hook();
        }

        private static Color LizardGraphics_BodyColor(On.LizardGraphics.orig_BodyColor orig, LizardGraphics self, float f)
        {
            if (self is LWIceLizardGraphics)
            {
                return self.DynamicBodyColor(f);
            }
            return orig(self, f);
        }

        private static Color LizardGraphics_DynamicBodyColor(On.LizardGraphics.orig_DynamicBodyColor orig, LizardGraphics self, float f)
        {
            if (self is LWIceLizardGraphics)
            {
                return Color.Lerp(self.palette.blackColor, self.whiteCamoColor, self.whiteCamoColorAmount);
            }
            return orig(self, f);
        }

        public static void HeadColor12Hook()
        {
            new Hook(typeof(LizardGraphics).GetMethod("get_HeadColor1", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), new Func<Func<LizardGraphics, Color>, LizardGraphics, Color> ((Func<LizardGraphics, Color> orig, LizardGraphics self) =>
            {
                if (self is LWIceLizardGraphics ice)
                {
                    return Color.Lerp(self.palette.blackColor, self.whiteCamoColor, self.whiteCamoColorAmount);
                }
                return orig(self);
            }));
            new Hook(typeof(LizardGraphics).GetMethod("get_HeadColor2", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), new Func<Func<LizardGraphics, Color>, LizardGraphics, Color>((Func<LizardGraphics, Color> orig, LizardGraphics self) =>
            {
                if (self is LWIceLizardGraphics ice)
                {
                    return Color.Lerp(self.effectColor, self.whiteCamoColor, self.whiteCamoColorAmount);
                }
                return orig(self);
            }));
        }

        private static LizardGraphics.IndividualVariations LizardGraphics_GenerateIvars(On.LizardGraphics.orig_GenerateIvars orig, LizardGraphics self)
        {
            var iVars = orig(self);
            if (self is LWIceLizardGraphics)
            {
                iVars.fatness = Mathf.Min(1f, iVars.fatness);
                iVars.tailFatness = Mathf.Min(1f, iVars.tailFatness);
            }
            return iVars;
        }
    }
}
