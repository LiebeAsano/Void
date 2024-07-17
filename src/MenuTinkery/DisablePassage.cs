using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Menu;
using Mono.Cecil;
using MonoMod.Utils;

namespace VoidTemplate.MenuTinkery;

internal static class DisablePassage
{
    public static void Hook()
    {
        On.Menu.SleepAndDeathScreen.AddPassageButton += RemoveButtonForVoid;
        IL.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
        //On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame1;
    }

    private static void SleepAndDeathScreen_GetDataFromGame1(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        if(package.characterStats.name == StaticStuff.TheVoid)
        {
            try
            {
                if(self.endgameTokens != null)
                {
                self.endgameTokens.RemoveSprites();
                self.pages[0].subObjects.Remove(self.endgameTokens);
                self.endgameTokens = null;

                }
            }
            catch (Exception e)
            {
                logerr(e);
            }
        }
    }

    private static void logerr(object e) => _Plugin.logger.LogError(e);
    private static void loginf(object e) => _Plugin.logger.LogInfo(e);
    /// <summary>
    /// not working
    /// </summary>
    /// <param name="il"></param>
    private static void SleepAndDeathScreen_GetDataFromGame(MonoMod.Cil.ILContext il)
    {
        var c = new ILCursor(il);
        //i spent an hour figuring out the issue in matching operator in generic. turns out type argument hates ExtEnum
        if (c.TryGotoNext(MoveType.After,
            q => q.MatchCall("ExtEnum`1<SlugcatStats/Name>", "op_Inequality")
            ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<bool, KarmaLadderScreen.SleepDeathScreenDataPackage, bool>>(static (orig, package) => orig && package.characterStats.name != StaticStuff.TheVoid);
        }
        else logerr("IL hook failed to match. MenuTinkery.DisablePassage:54");
    }

    private static void RemoveButtonForVoid(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, Menu.SleepAndDeathScreen self, bool buttonBlack)
    {
        if (self.saveState != null && (self.saveState.saveStateNumber == StaticStuff.TheVoid)) return; //no need in calling orig if it's void, because the button is not supposed to be here at all
        orig(self, buttonBlack);
    }
}
