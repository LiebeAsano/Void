using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Menu;

namespace VoidTemplate.MenuTinkery;

internal static class DisablePassage
{
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.AddPassageButton += RemoveButtonForVoid;
        //IL.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame1;
    }

    private static void SleepAndDeathScreen_GetDataFromGame1(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        if(package.characterStats.name == StaticStuff.TheVoid)
        {
            self.pages[0].subObjects.Remove(self.endgameTokens);
            self.endgameTokens = null;
        }
    }

    private static void logerr(object e) => _Plugin.logger.LogError(e);
    /// <summary>
    /// not working
    /// </summary>
    /// <param name="il"></param>
    private static void SleepAndDeathScreen_GetDataFromGame(MonoMod.Cil.ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            q => q.MatchCall<ExtEnum<SlugcatStats.Name>>("op_Inequality")))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<bool, KarmaLadderScreen.SleepDeathScreenDataPackage, bool>>(static (orig, package) => orig && package.characterStats.name != StaticStuff.TheVoid);
        }
        else logerr($"IL hook at MenuTinkery.DisablePasssage:24 didn't work");
    }

    private static void RemoveButtonForVoid(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, Menu.SleepAndDeathScreen self, bool buttonBlack)
    {
        if (self.saveState != null && (self.saveState.saveStateNumber == StaticStuff.TheVoid)) return; //no need in calling orig if it's void, because the button is not supposed to be here at all
        orig(self, buttonBlack);
    }
}
