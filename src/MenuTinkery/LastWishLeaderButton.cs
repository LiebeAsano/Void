using Menu;
using UnityEngine;
using static Menu.Menu;

namespace VoidTemplate.MenuTinkery
{
    public class LastWishLeaderButton : SimpleButton
    {
        private const float Margin = 10f;
        private const float Gap = 10f;

        private readonly FSprite symbolSprite;

        public LastWishLeaderButton(Menu.Menu menu, MenuObject owner)
            : base(menu, owner, "", "", new Vector2(1246f, Margin), new Vector2(50f, 50f))
        {
            symbolSprite = new FSprite("atlas-void/leader_icon");
            Container.AddChild(symbolSprite);
        }

        public override void Update()
        {
            base.Update();

            float screenWidth = menu.manager.rainWorld.options.ScreenSize.x;
            float rightEdge = 683f + screenWidth * 0.5f;

            pos.x = rightEdge - size.x * 2f - Margin - Gap;
            pos.y = Margin;
        }

        public override void Clicked()
        {
            menu.PlaySound(SoundID.MENU_Switch_Page_In);
            menu.manager.RequestMainProcessSwitch(
                VoidEnums.ProcessID.LeaderTableMenu);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            Vector2 drawPos = DrawPos(timeStacker);
            Vector2 drawSize = DrawSize(timeStacker);

            symbolSprite.SetPosition(drawPos + drawSize / 2f);

            float num =
                0.5f -
                0.5f * Mathf.Sin(
                    Mathf.Lerp(
                        buttonBehav.lastSin,
                        buttonBehav.sin,
                        timeStacker)
                    / 30f *
                    Mathf.PI *
                    2f);

            num *= buttonBehav.sizeBump;

            symbolSprite.color = buttonBehav.greyedOut
                ? MenuRGB(MenuColors.VeryDarkGrey)
                : Color.Lerp(
                    MyColor(timeStacker),
                    MenuRGB(MenuColors.VeryDarkGrey),
                    num);
        }

        public override void RemoveSprites()
        {
            symbolSprite.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
}