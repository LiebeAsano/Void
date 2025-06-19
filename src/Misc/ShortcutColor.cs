using VoidTemplate.Useful;
using UnityEngine;
using SlugBase.Features;
using RWCustom;

namespace VoidTemplate.Misc;

public static class ShortcutColor
{
    public static void Hook()
    {
        On.Player.ShortCutColor += Player_ShortCutColor;
    }

    private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        var res = orig(self);
        if (self.IsVoid())
        {
            res = Utils.VoidColors[self.playerState.playerNumber];
            if (ModManager.CoopAvailable)
            {
                if (Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM
                    && Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.AUTO
                    || Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.AUTO
                    && self.playerState.playerNumber == 0)
                {
                    res = new(1f, 0.86f, 0f);
                }
            }
            return res;
        }
        return res;
    }
}
