using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;

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
            if (menu is PauseMenu pause && pause.game.IsVoidWorld())
            {
                self.pickupButtonInstructions.text = $"{menu.Translate("Pick up / Eat / Maul button interactions:") + "\r\n\r\n"}";
                self.pickupButtonInstructions.text += "  - " + menu.Translate("Tap to pick up object") + "\r\n";
                self.pickupButtonInstructions.text += "  - " + menu.Translate("Hold to eat / swallow object / maul stunned creature") + "\r\n";
                self.pickupButtonInstructions.text += "  - " + menu.Translate("Press while holding down direction to drop object") + "\r\n";
                self.pickupButtonInstructions.text += "  - " + menu.Translate("Double tap to switch hands") + "\r\n";
                self.pickupButtonInstructions.text += $"{"\r\n\r\n" + menu.Translate("Jump button interactions:") + "\r\n\r\n"}";
                self.pickupButtonInstructions.text += "  - " + menu.Translate("Press while climbing wall + holding up / down direction to jump up / flip") + "\r\n";
                if (pause.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap >= 4)
                {
                    self.pickupButtonInstructions.text += "  - " + menu.Translate("Hold while climbing ceil + press left / right direction to flip") + "\r\n";
                    self.pickupButtonInstructions.text += $"{"\r\n\r\n" + menu.Translate("Throw button interactions:") + "\r\n\r\n"}";
                    self.pickupButtonInstructions.text += "  - " + menu.Translate("Press while climbing ceil + holding jump to throw down") + "\r\n";
                }
                if (OptionAccessors.ComplexControl || pause.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap >= 4)
                {
                    self.pickupButtonInstructions.text += $"{"\r\n\r\n" + menu.Translate("Special button interactions:") + "\r\n\r\n"}";
                    if (OptionAccessors.ComplexControl)
                        self.pickupButtonInstructions.text += "  - " + menu.Translate("Tap to switch climbing / running mode") + "\r\n";
                    if (pause.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap >= 4)
                        self.pickupButtonInstructions.text += "  - " + menu.Translate("Hold while climbing wall to charge wall jump") + "\r\n";
                }
            }
        }
    }
}
