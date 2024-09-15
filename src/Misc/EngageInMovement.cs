using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.Misc;

internal static class EngageInMovement
{
    public static void Hook()
    {
        On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
    }

    public static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, global::SlugcatHand slugcat_hand)
    {
        if (slugcat_hand.owner is not PlayerGraphics player_graphics ||
            player_graphics.owner is not Player player ||
            player.Get_Attached_Fields() is not PlayMod.Player_Attached_Fields attached_fields)
        {
            return orig(slugcat_hand);
        }

        if (player.animation != Player.AnimationIndex.None || player.input[0].y == 0 || (player.bodyMode != Player.BodyModeIndex.WallClimb && player.bodyMode != BodyModeIndexExtension.CeilCrawl))
        {
            attached_fields.initialize_hands = true;
            return orig(slugcat_hand);
        }

        if (attached_fields.initialize_hands)
        {
            if (slugcat_hand.limbNumber == 1)
            {
                attached_fields.initialize_hands = false;
                player.animationFrame = 0;
            }
            return orig(slugcat_hand);
        }

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        // Логика для лазания по потолку
        if (player.bodyMode == BodyModeIndexExtension.CeilCrawl)
        {

            if (body_chunk_0.pos.x > body_chunk_1.pos.x)
            {
                player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(100f, 0.0f), 0f);
                player_graphics.objectLooker.timeLookingAtThis = 6;
            }
            else
            {
                player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(-100f, 0.0f), 0f);
                player_graphics.objectLooker.timeLookingAtThis = 6;
            }
            player.animationFrame++;

            slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;

            orig(slugcat_hand);

            if (!Custom.DistLess(slugcat_hand.pos, slugcat_hand.connection.pos, 20f))
            {
                Vector2 vector = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                Vector2 gripDirectionOffset = new(player.flipDirection * 10f, 5f);
                slugcat_hand.FindGrip(player.room, slugcat_hand.connection.pos, slugcat_hand.connection.pos, 100f,
                    slugcat_hand.connection.pos + (vector + new Vector2(player.input[0].x, player.input[0].y).normalized * 1.5f).normalized * 20f + gripDirectionOffset, 2, 2, false);
            }
            return false;
        }

        if (player.bodyMode == Player.BodyModeIndex.WallClimb)
        {
            Vector2 current_absolute_hunt_position = slugcat_hand.absoluteHuntPos;
            orig(slugcat_hand);
            slugcat_hand.absoluteHuntPos = current_absolute_hunt_position;

            if (!(player.animationFrame == 1 && slugcat_hand.limbNumber == 0 || player.animationFrame == 11 && slugcat_hand.limbNumber == 1)) return false;
            slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;
            Vector2 attached_position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);

            if (player.input[0].y > 0)
            {
                player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(0.0f, 100f), 0f);
                player_graphics.objectLooker.timeLookingAtThis = 6;
            }
            else
            {
                player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(0.0f, -100f), 0f);
                player_graphics.objectLooker.timeLookingAtThis = 6;
            }
            player.animationFrame++;

            if (player.input[0].y > 0 && player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                if (body_chunk_0.pos.y > body_chunk_1.pos.y)
                {
                    slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 30f), -player.flipDirection, 2, false);
                    return false;
                }
                else
                {
                    slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 0f), -player.flipDirection, 2, false);
                    return false;
                }
            }

            slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, -10f), -player.flipDirection, 2, false);
            return false;
        }

        return orig(slugcat_hand);
    }
}
