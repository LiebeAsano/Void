using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using UnityEngine;
using static VoidTemplate.Useful.Utils;
using Menu;

namespace VoidTemplate.MenuTinkery
{
    internal static class IntroRollIllustrstion
    {
        public static void Hook()
        {
            IL.Menu.IntroRoll.ctor += IntroRoll_ctor;
        }

        private static void IntroRoll_ctor(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(5)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((IntroRoll self) =>
                {
                    if (self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat != MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                    {
                        self.illustrations[2] = new(self, self.pages[0], "", "Void_Title_Card", Vector2.zero, true, false);
                    }
                });
            }
            else logerr($"{nameof(MenuTinkery)}.{nameof(IntroRollIllustrstion)}.{nameof(IntroRoll_ctor)}: match error");
        }
    }
}
