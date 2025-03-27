using VoidTemplate.Useful;
using UnityEngine;

namespace VoidTemplate.Misc;

internal static class ShortcutColor
{
    public static void Hook()
    {
        On.Player.ShortCutColor += Player_ShortCutColor;
    }

    private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        var res = orig(self);
        if (self.IsVoid()) return Utils.VoidColor;
        return res;
    }
}
