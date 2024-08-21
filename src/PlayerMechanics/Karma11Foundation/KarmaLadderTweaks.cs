using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class KarmaLadderTweaks
{
    const int karma11index = 10;
    public static void Hook()
    {
        On.Menu.KarmaLadder.KarmaSymbol.ctor += KarmaSymbol_ctor;
    }

    private static void KarmaSymbol_ctor(On.Menu.KarmaLadder.KarmaSymbol.orig_ctor orig, Menu.KarmaLadder.KarmaSymbol self, Menu.Menu menu, Menu.MenuObject owner, UnityEngine.Vector2 pos, FContainer container, FContainer foregroundContainer, RWCustom.IntVector2 displayKarma)
    {
        orig(self, menu, owner, pos, container, foregroundContainer, displayKarma); 
        if(displayKarma.x == karma11index)
        {
            self.sprites[self.RingSprite].alpha = 0f;
        }
    }
}
