using RWCustom;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics
{
    internal static class ViyBodyChanges
    {
        public static void Hook()
        {
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        //sprites[0] BodyA
        //sprites[1] HipsA
        //sprites[2] Tail
        //sprites[3] HeadB0-HeadA0 Normal and Saint
        //sprites[4] LegsA0
        //sprites[5] PlayerArmA0
        //sprites[6] PlayerArmA0
        //sprites[7] OnTopOfTerrainHand
        //sprites[8] OnTopOfTerrainHand
        //sprites[9] FaceA0
        //sprites[10] LightForPlayer
        //sprites[11] MarkOfCommunication

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            //Call original method
            orig(self, sLeaser, rCam, timeStacker, camPos);

            //Don't run the rest of the code if player or room is not available, or if not a Viy
            if (self.player == null || self.player.room == null || !self.player.IsViy())
                return;

            //Calculate breathing oscillation
            float interpolatedBreath = Mathf.Lerp(self.lastBreath, self.breath, timeStacker);
            float useBreath = 0.5f + 0.5f * Mathf.Sin(interpolatedBreath * Mathf.PI * 2f);

            //Note: Since the Viy have a larger body, the head needs to be adjusted to be higher up
            //HipsA, Head and Face are the sprites that need to be adjusted

            //Interpolate body positions
            Vector2 upperBodyDrawPos = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 lowerBodyDrawPos = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            Vector2 headPos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);

            //Update sprite scale with breathing and malnourishment effects
            sLeaser.sprites[1].scaleX = 1f + self.player.sleepCurlUp * 0.2f + 0.05f * useBreath - 0.05f * self.malnourished;

            //Calculate head rotation based on the position between lower and upper body to head
            float headRotation = Custom.AimFromOneVectorToAnother(Vector2.Lerp(lowerBodyDrawPos, upperBodyDrawPos, 0.5f), headPos);
            int headGraphic = Mathf.RoundToInt(Mathf.Abs(headRotation / 360f * 34f));

            //Determine adjustments if player is in sleep curl state
            if (self.player.sleepCurlUp > 0f)
            {
                headGraphic = 7;
                headGraphic = Custom.IntClamp((int)Mathf.Lerp(headGraphic, 4f, self.player.sleepCurlUp), 0, 8);
            }

            //Calculate face displacement
            Vector2 faceDisplacement = Vector2.Lerp(self.lastLookDir, self.lookDirection, timeStacker) * 3f * (1f - self.player.sleepCurlUp);

            //Calculate the horizontal sign difference between body parts (used repeatedly)
            float bodySign = Mathf.Sign(upperBodyDrawPos.x - lowerBodyDrawPos.x);

            //Modify parameters based on player state
            if (self.player.sleepCurlUp > 0f)
            {
                sLeaser.sprites[9].scaleX = bodySign;
                sLeaser.sprites[9].rotation = headRotation * (1f - self.player.sleepCurlUp);
                headRotation = Mathf.Lerp(headRotation, 45f * bodySign, self.player.sleepCurlUp);
                headPos.y += 1f * self.player.sleepCurlUp + 5f;
                headPos.x += bodySign * 2f * self.player.sleepCurlUp;
                faceDisplacement.y -= 2f * self.player.sleepCurlUp;
                faceDisplacement.x -= 4f * bodySign * self.player.sleepCurlUp;
            }
            else if (self.owner.room != null && self.owner.EffectiveRoomGravity == 0f)
            {
                headGraphic = 0;
                sLeaser.sprites[9].rotation = headRotation;
            }
            else if (self.player.Consious)
            {
                //Handle body mode specific adjustments
                if ((self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x != 0) || self.player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    if (self.player.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        headGraphic = 7;
                        sLeaser.sprites[9].scaleX = bodySign;
                    }
                    else
                    {
                        headGraphic = 6;
                        sLeaser.sprites[9].scaleX = (headRotation < 0f ? -1f : 1f);
                    }
                    faceDisplacement.x = 0f;
                    sLeaser.sprites[9].y += 1f;
                }
                else
                {
                    //Adjust face displacement based on direction vector
                    Vector2 v = headPos - lowerBodyDrawPos;
                    v.x *= 1f - faceDisplacement.magnitude / 3f;
                    v.Normalize();
                    sLeaser.sprites[9].scaleX = (Mathf.Abs(faceDisplacement.x) < 0.1f ? (headRotation < 0f ? -1f : 1f) : Mathf.Sign(faceDisplacement.x));
                }
                sLeaser.sprites[9].rotation = 0f;
            }
            else
            {
                faceDisplacement = Vector2.zero;
                headGraphic = 0;
                sLeaser.sprites[9].rotation = headRotation;
            }

            //Apply modifications for cooperative play
            if (ModManager.CoopAvailable && self.player.bool1)
            {
                sLeaser.sprites[0].scaleX += 0.35f;
                sLeaser.sprites[1].rotation += 0.1f;
                sLeaser.sprites[9].rotation = headRotation + 0.2f;

                headPos.y -= 1.9f;
                headRotation = Mathf.Lerp(headRotation, 45f * bodySign, 0.7f);
                faceDisplacement.x -= 0.2f;
            }

            //Set final sprite positions and rotations for head and face
            sLeaser.sprites[3].x = headPos.x - camPos.x;
            sLeaser.sprites[3].y = 5f + headPos.y - camPos.y;
            sLeaser.sprites[3].rotation = headRotation;
            sLeaser.sprites[3].scaleX = (headRotation < 0f ? -1f : 1f);

            sLeaser.sprites[9].x = headPos.x + faceDisplacement.x - camPos.x;
            sLeaser.sprites[9].y = 5f + headPos.y + faceDisplacement.y - 2f - camPos.y;
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);

            //Early exit if player or room is not available, or if not a Viy
            if (self.player == null || self.player.room == null || !self.player.IsViy())
                return;

            //Set initial sprite scale for a Viy
            sLeaser.sprites[0].scaleY = 2.5f;
            sLeaser.sprites[0].scaleX = 2.5f;
        }
    }
}