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
        
    }


    private static void logerr(object e) => _Plugin.logger.LogError(e);
    private static void loginf(object e) => _Plugin.logger.LogInfo(e);
    /// <summary>
    /// removes region tracker and tokens from rendering in sleep screenв лог
    /// </summary>
    /// <param name="il"></param>
    private static void SleepAndDeathScreen_GetDataFromGame(MonoMod.Cil.ILContext il)
    {
        var c = new ILCursor(il);
        //if (ModManager.MMF <AND NOT VOID>)
        // Create Collectibles Tracker
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<bool, KarmaLadderScreen.SleepDeathScreenDataPackage, bool>>(static (orig, package) => orig && !(package.characterStats.name == StaticStuff.TheVoid));
        }
        else logerr("IL error at voidmod, MenuTinkery.DisablePassage.SleepAndDeathScreen_GetDataFromGame, anti collectible hook (failed to find)");
        //i spent an hour figuring out the issue in matching operator in generic. turns out type argument hates ExtEnum
        //if (package.characterStats.name != (SlugcatStats.Name.Red <OR VOID>))
        // create endgame tokens
        if (c.TryGotoNext(MoveType.After,
            q => q.MatchCall("ExtEnum`1<SlugcatStats/Name>", "op_Inequality")
            ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<bool, KarmaLadderScreen.SleepDeathScreenDataPackage, bool>>(static (orig, package) => orig && package.characterStats.name != StaticStuff.TheVoid);
        }
        else logerr("IL error at voidmod, MenuTinkery.DisablePassage.SleepAndDeathScreen_GetDataFromGame, anti endgame tokens hook (failed to find)");
    }

    private static void RemoveButtonForVoid(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, Menu.SleepAndDeathScreen self, bool buttonBlack)
    {
        if (self.saveState != null && (self.saveState.saveStateNumber == StaticStuff.TheVoid)) return; //no need in calling orig if it's void, because the button is not supposed to be here at all
        orig(self, buttonBlack);
    }
}
