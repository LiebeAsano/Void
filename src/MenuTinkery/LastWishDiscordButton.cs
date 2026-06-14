using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;

namespace VoidTemplate.MenuTinkery
{
    public class LastWishDiscordButton : SymbolButton
    {
        public LastWishDiscordButton(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, symbolName: "pixel", "", pos)
        {
            roundedRect.size = new(64, 64);
            size = new(64, 64);
        }
        public override void Clicked()
        {
            menu.PlaySound(SoundID.MENU_Switch_Page_In);
            Process.Start(new ProcessStartInfo("https://discord.gg/rainworldlastwish") { UseShellExecute = true });
        }
    }
}
