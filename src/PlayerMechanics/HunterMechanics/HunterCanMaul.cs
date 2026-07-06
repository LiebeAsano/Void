using VoidTemplate.OptionInterface;

namespace VoidTemplate.PlayerMechanics.HunterMechanics;

public static class HunterCanMaul
{
    public static void Hook()
    {
        On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
    }

    private static bool SlugcatStats_SlugcatCanMaul(On.SlugcatStats.orig_SlugcatCanMaul orig, SlugcatStats.Name slugcatNum)
    {
        return OptionAccessors.BuffHunter && slugcatNum == SlugcatStats.Name.Red || orig(slugcatNum);
    }
}
