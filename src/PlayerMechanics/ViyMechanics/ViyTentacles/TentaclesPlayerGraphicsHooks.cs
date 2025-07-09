using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class TentaclesPlayerGraphicsHooks
    {
        public static void Hook()
        {
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.player.TryGetRot(out var rot))
            {
                rot.graphics.InitSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.player.TryGetRot(out var rot))
            {
                rot.graphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (self.player.TryGetRot(out var rot))
            {
                rot.graphics.AddToContainer(sLeaser, rCam, newContatiner);
            }
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (self.player.TryGetRot(out var rot))
            {
                rot.graphics.Reset();
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (self.player.TryGetRot(out var rot))
            {
                rot.graphics.Update();
            }
        }
    }
}
