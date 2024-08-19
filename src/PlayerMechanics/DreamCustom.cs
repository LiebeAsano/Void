using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace VoidTemplate.PlayerMechanics;

public static class DreamCustom
{
    public static void Hook()
    {
        //On.DreamsState.EndOfCycleProgress += CustomDream;
    }

    private static void CustomDream(On.DreamsState.orig_EndOfCycleProgress orig, DreamsState self, SaveState saveState, string currentRegion, string denPosition)
    {

    }
}
