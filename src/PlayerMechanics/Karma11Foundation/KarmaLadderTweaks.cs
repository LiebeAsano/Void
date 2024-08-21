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
        On.Menu.KarmaLadderScreen.FoodCountDownDone += KarmaLadderScreen_FoodCountDownDone;
    }
    /// <summary>
    /// prevents karma 11 from going up or down
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private static void KarmaLadderScreen_FoodCountDownDone(On.Menu.KarmaLadderScreen.orig_FoodCountDownDone orig, Menu.KarmaLadderScreen self)
    {
        orig(self);
        if(self.karma.x == karma11index) self.karmaLadder.GoToKarma(karma11index, false);
    }

    /// <summary>
    /// Disables the circle around karma 11 for karma ladder and lowers transparency for all the symbols when currently it's karma 11
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="menu"></param>
    /// <param name="owner"></param>
    /// <param name="pos"></param>
    /// <param name="container"></param>
    /// <param name="foregroundContainer"></param>
    /// <param name="displayKarma"></param>
    private static void KarmaSymbol_ctor(On.Menu.KarmaLadder.KarmaSymbol.orig_ctor orig, Menu.KarmaLadder.KarmaSymbol self, Menu.Menu menu, Menu.MenuObject owner, UnityEngine.Vector2 pos, FContainer container, FContainer foregroundContainer, RWCustom.IntVector2 displayKarma)
    {
        orig(self, menu, owner, pos, container, foregroundContainer, displayKarma); 
        if(displayKarma.x == karma11index)
        {
            self.sprites[self.RingSprite].alpha = 0f;
        }
        if(self.ladder.displayKarma.x == karma11index && displayKarma.x != karma11index)
        {
            Array.ForEach(self.sprites, sprite => sprite.alpha = 0f);
        }

    }
}
