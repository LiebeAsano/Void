using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics
{
    internal static class FastCrawl
    {
        public static void Hook()
        {
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
        }

        private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            orig(self);
            if (self.IsVoid())
            {
                if (self.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    self.dynamicRunSpeed[0] *= 2f;
                }
            }
            if (self.IsViy())
            {
                if (self.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    self.dynamicRunSpeed[0] *= 2.5f;
                }
            }
        }
    }
}
