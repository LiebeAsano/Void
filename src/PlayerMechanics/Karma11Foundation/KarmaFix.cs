using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class KarmaFix
{
    public static void Hook()
    {
        //On.Menu.KarmaLadderScreen.GetDataFromGame += KarmaLadderScreen_GetDataFromGame;
    }

    private static void KarmaLadderScreen_GetDataFromGame(On.Menu.KarmaLadderScreen.orig_GetDataFromGame orig, Menu.KarmaLadderScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        if (package.karma.x == 10)
        {
            self.myGamePackage.karma = new IntVector2(10, 10);
        }
    }
}
