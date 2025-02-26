using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class CrawlJump
{
    public static void Hook()
    {
        On.Player.MovementUpdate += Player_MovementUpdate;
        On.Player.Jump += Player_Jump;
    }

    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.IsVoid())
        {
            int num16 = 0;
            if (self.superLaunchJump > 0 && self.killSuperLaunchJumpCounter < 1)
            {
                num16 = 1;
            }
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.bodyChunks[0].ContactPoint.y < 0 && self.bodyChunks[1].ContactPoint.y < 0)
            {
                if (self.input[0].y == 0)
                {
                    num16 = 0;
                    self.wantToJump = 0;
                    if (self.input[0].jmp)
                    {
                        if (self.superLaunchJump < 20)
                        {
                            self.superLaunchJump += 5;
                        }
                        else
                        {
                            self.killSuperLaunchJumpCounter = 15;
                        }
                    }
                }
                if (!self.input[0].jmp && self.input[1].jmp)
                {
                    self.wantToJump = 1;
                }
            }
            if (self.killSuperLaunchJumpCounter > 0)
            {
                self.killSuperLaunchJumpCounter--;
            }
            if (self.simulateHoldJumpButton > 0)
            {
                self.simulateHoldJumpButton--;
            }
            if (self.canJump > 0 && self.wantToJump > 0)
            {
                self.canJump = 0;
                self.wantToJump = 0;
                self.Jump();
            }
            self.superLaunchJump -= num16;
        }
    }

    private static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        if (self.IsVoid())
        {
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.input[0].jmp)
            {
                return;
            }
        }
        orig(self);
    }
}
