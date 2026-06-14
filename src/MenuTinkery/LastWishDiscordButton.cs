using System.Diagnostics;
using Menu;
using UnityEngine;
using static Menu.Menu;

namespace VoidTemplate.MenuTinkery
{
    public class LastWishDiscordButton : SimpleButton
    {
        public FSprite symbolSprite;

        public LastWishDiscordButton(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, "", "", pos, new(50, 50))
        {
            symbolSprite = new("atlas-void/discord_icon");
            Container.AddChild(symbolSprite);
        }
        public override void Clicked()
        {
            menu.PlaySound(SoundID.MENU_Switch_Page_In);
            Process.Start(new ProcessStartInfo("https://discord.gg/rainworldlastwish") { UseShellExecute = true });
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            var pos = DrawPos(timeStacker);
            var size = DrawSize(timeStacker);
            symbolSprite.SetPosition(pos + (size / 2));
            float num = 0.5f - 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
            num *= buttonBehav.sizeBump;
            symbolSprite.color = (buttonBehav.greyedOut ? MenuRGB(MenuColors.VeryDarkGrey) : Color.Lerp(MyColor(timeStacker), MenuRGB(MenuColors.VeryDarkGrey), num));
        }

        public override void RemoveSprites()
        {
            symbolSprite.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
}
