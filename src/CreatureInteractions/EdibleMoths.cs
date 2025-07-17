using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.CreatureInteractions
{
    public class EdibleMoths
    {
        public static void Hook()
        {
            On.StaticWorld.InitSmallMoth += StaticWorld_InitSmallMoth;
        }

        private static void StaticWorld_InitSmallMoth(On.StaticWorld.orig_InitSmallMoth orig, List<CreatureTemplate> tempCreatureTemplates, CreatureTemplate bigMothTemplate, CreatureTemplate batTemplate)
        {
            orig(tempCreatureTemplates, bigMothTemplate, batTemplate);
            bigMothTemplate.meatPoints = 8;
            tempCreatureTemplates[tempCreatureTemplates.Count - 1].meatPoints = 2;
        }

    }
}
