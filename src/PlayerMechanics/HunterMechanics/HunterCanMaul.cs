using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.HunterMechanics;

public static class HunterCanMaul
{
    public static void Hook()
    {
        On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
    }

    private static bool SlugcatStats_SlugcatCanMaul(On.SlugcatStats.orig_SlugcatCanMaul orig, SlugcatStats.Name slugcatNum)
    {
        return slugcatNum == SlugcatStats.Name.Red || orig(slugcatNum);
    }
}
