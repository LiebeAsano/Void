using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery
{
    internal static class MenuTitle
    {
        public static void Hook()
        {
            IL.Menu.MainMenu.ctor += ModifyMainMenuConstructor;
        }

        private static void ModifyMainMenuConstructor(ILContext il)
        {
            ILCursor cursor = new(il);
            ILCursor cursor2 = new(il);

            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdstr("MainTitleBevel")))
            {

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, MainMenu, string>>((originalName, self) =>
                {

                    if (SaveManager.ExternalSaveData.VoidDead
                    && SaveManager.ExternalSaveData.VoidKarma11)
                    {
                        return "ViyMainTitleBevel";
                    }
                    else
                    {
                        return "VoidMainTitleBevel";
                    }
                });
            }
            else LogExErr("Failed IL.Menu.MainMenu.ctor MainTitleBevel");

            if (cursor2.TryGotoNext(MoveType.After,
                x => x.MatchLdstr("MainTitleShadow")))
            {

                cursor2.Emit(OpCodes.Ldarg_0);
                cursor2.EmitDelegate<Func<string, MainMenu, string>>((originalName, self) =>
                {

                    if (SaveManager.ExternalSaveData.VoidDead
                    && SaveManager.ExternalSaveData.VoidKarma11)
                    {
                        return "ViyMainTitleShadow";
                    }
                    else
                    {
                        return "VoidMainTitleShadow";
                    }
                });
            }
            else LogExErr("Failed IL.Menu.MainMenu.ctor MainTitleShadow");
        }
    }
}
