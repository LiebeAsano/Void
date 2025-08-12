using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Mono.Cecil.Cil;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery
{
    public class StatiscticsScreen
    {
        public static void Hook()
        {
            IL.Menu.StoryGameStatisticsScreen.TickerIsDone += StoryGameStatisticsScreen_TickerIsDone;
        }

        private static void StoryGameStatisticsScreen_TickerIsDone(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchCall<StoryGameStatisticsScreen>(nameof(StoryGameStatisticsScreen.GetNonSandboxKillscore)),
                x => x.MatchStloc(1),
                x => x.MatchLdloc(1)))
            {
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate((int origNum, IconSymbol.IconSymbolData iconData) =>
                {
                    return origNum != 0 || MultiplayerUnlocks.SandboxUnlockForSymbolData(iconData).Index < 0;
                });
            }
            else
            {
                logerr($"{nameof(MenuTinkery)}.{nameof(StatiscticsScreen)}.{nameof(StoryGameStatisticsScreen_TickerIsDone)}: match error.");
            }
        }
    }
}
