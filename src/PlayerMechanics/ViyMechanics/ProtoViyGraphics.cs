using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;
using static VoidTemplate.VoidEnums;
namespace VoidTemplate.PlayerMechanics.ViyMechanics;
public static class ProtoViyGraphics
{

    public static void Hook()
    {
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
    }

    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.player == null) return;
        if (!self.player.isNPC) return;
        if (self.player.SlugCatClass != SlugcatID.ProtoViy) return;

        Color bodyColor = new(0.0f, 0.0f, 0.005f);
        Player player = self.player;
        BodyChunk playerBodyChunk0 = player.bodyChunks[0];
        BodyChunk playerBodyChunk1 = player.bodyChunks[1];

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            if (i == 9)
            {
                if (Utils.ProtoViyEyeColors.TryGetValue(self.player.abstractCreature, out var eyeColor))
                    sLeaser.sprites[9].color = eyeColor;
                FSprite faceSprite = sLeaser.sprites[i];
                string faceSpriteName = faceSprite.element.name;
                if (Climbing.IsTouchingDiagonalCeiling(player)
                    && player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!player.input[0].jmp
                        && player.bodyMode != Player.BodyModeIndex.ZeroG
                        && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
                    {
                        SetProtoViyFaceSprite("ViyDCeil-");
                    }
                    else SetProtoViyFaceSprite("Viy-");
                }
                else if (Climbing.IsTouchingCeiling(player)
                    && player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!player.input[0].jmp
                        && player.bodyMode != Player.BodyModeIndex.ZeroG
                        && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam
                        && playerBodyChunk0.pos.y <= playerBodyChunk1.pos.y + 5)
                    {
                        SetProtoViyFaceSprite("ViyCeil-");
                    }
                    else SetProtoViyFaceSprite("Viy-");
                }
                else
                {
                    if (playerBodyChunk0.pos.y + 10f > playerBodyChunk1.pos.y
                        || player.bodyMode == Player.BodyModeIndex.ZeroG
                        || player.bodyMode == Player.BodyModeIndex.Dead
                        || player.bodyMode == Player.BodyModeIndex.Stunned
                        || player.bodyMode == Player.BodyModeIndex.Crawl)
                    {

                        SetProtoViyFaceSprite("Viy-");
                    }
                    else
                    {
                        SetProtoViyFaceSprite("ViyDown-");
                    }
                }
                void SetProtoViyFaceSprite(string spriteName) => SetProtoViySprite(faceSprite, spriteName, faceSpriteName);
            }
            else
            {
                sLeaser.sprites[i].color = bodyColor;
            }
            static void SetProtoViySprite(FSprite toSprite, string spriteName, string origSprite)
            {
                string sprite = spriteName + origSprite;
                if (Futile.atlasManager.DoesContainElementWithName(sprite))
                    toSprite.element = Futile.atlasManager.GetElementWithName(sprite);
            }
        }
        
    }
}


