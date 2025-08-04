using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.MenuTinkery
{
    public class PauseMenuTips
    {
        public static void Hook()
        {
            On.Menu.ControlMap.ctor += ControlMap_ctor;
        }

        private static void ControlMap_ctor(On.Menu.ControlMap.orig_ctor orig, ControlMap self, Menu.Menu menu, MenuObject owner, UnityEngine.Vector2 pos, Options.ControlSetup.Preset preset, bool showPickupInstructions)
        {
            orig(self, menu, owner, pos, preset, showPickupInstructions);
            if (menu is PauseMenu)
            {
                self.pickupButtonInstructions.text += $"\r\n  - {menu.Translate("Pivo")}";
            }
        }
    }
}
