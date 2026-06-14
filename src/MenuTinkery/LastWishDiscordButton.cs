using System.Diagnostics;
using Menu;
using UnityEngine;

namespace VoidTemplate.MenuTinkery
{
    public class LastWishDiscordButton : SymbolButton
    {
        public LastWishDiscordButton(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, symbolName: "atlas-void/discord_icon", "", pos)
        {
            roundedRect.size = new(50, 50);
            size = new(50, 50);
        }
        public override void Clicked()
        {
            menu.PlaySound(SoundID.MENU_Switch_Page_In);
            Process.Start(new ProcessStartInfo("https://discord.gg/rainworldlastwish") { UseShellExecute = true });
        }
    }
}
