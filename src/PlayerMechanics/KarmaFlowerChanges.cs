using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using Random = UnityEngine.Random;

namespace VoidTemplate.PlayerMechanics;

public static class KarmaFlowerChanges
{
    private static readonly ConditionalWeakTable<KarmaFlower, KarmaFlowerExtention> flowerExt = new();
    private static readonly ConditionalWeakTable<Player, PlayerExtention> playerExt = new();

    public static KarmaFlowerExtention GetFlowerExt(this KarmaFlower flower) => flowerExt.GetOrCreateValue(flower);
    public static PlayerExtention GetPlayerExt(this Player player) => playerExt.GetOrCreateValue(player);

    private const int VanillaPetalCount = 4;
    private const int VoidPetalCount = 5;
    private const int TotalSpritesWithExtra = 10;
    private const int ExtraPetalSpriteIndex = 9;

    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo ColorField = typeof(KarmaFlower).GetField("color", Flags);
    private static readonly FieldInfo StalkColorField = typeof(KarmaFlower).GetField("stalkColor", Flags);
    private static readonly FieldInfo MovementField = typeof(KarmaFlower).GetField("movement", Flags);
    private static readonly FieldInfo LastMovementField = typeof(KarmaFlower).GetField("lastMovement", Flags);
    private static readonly FieldInfo FaceCameraField = typeof(KarmaFlower).GetField("faceCamera", Flags);

    public static void Initiate()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Plyaer_Update;

        On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
        On.Player.FoodInRoom_Room_bool += Player_FoodInRoom_Room_bool;

        On.KarmaFlower.Update += KarmaFlower_Update;
        On.KarmaFlower.DrawSprites += KarmaFlower_DrawSprites;
        On.KarmaFlower.InitiateSprites += KarmaFlower_InitiateSprites;
        On.KarmaFlower.AddToContainer += KarmaFlower_AddToContainer;
        On.KarmaFlower.NewRoom += KarmaFlower_NewRoom;
        On.KarmaFlower.ApplyPalette += KarmaFlower_ApplyPalette;
    }

    private static Color GetFlowerColor(KarmaFlower self)
    {
        if (ColorField?.GetValue(self) is Color color)
            return color;
        return RainWorld.GoldRGB;
    }

    private static void SetFlowerColor(KarmaFlower self, Color color)
    {
        ColorField?.SetValue(self, color);
    }

    private static Color GetStalkColor(KarmaFlower self)
    {
        if (StalkColorField?.GetValue(self) is Color color)
            return color;
        return Color.white;
    }

    private static void SetStalkColor(KarmaFlower self, Color color)
    {
        StalkColorField?.SetValue(self, color);
    }

    private static float GetMovement(KarmaFlower self)
    {
        if (MovementField?.GetValue(self) is float movement)
            return movement;
        return 0f;
    }

    private static float GetLastMovement(KarmaFlower self)
    {
        if (LastMovementField?.GetValue(self) is float lastMovement)
            return lastMovement;
        return 0f;
    }

    private static float GetFaceCamera(KarmaFlower self)
    {
        if (FaceCameraField?.GetValue(self) is float faceCamera)
            return faceCamera;
        return 0.5f;
    }

    private static bool HasExtraSprite(RoomCamera.SpriteLeaser sLeaser)
    {
        return sLeaser?.sprites != null && sLeaser.sprites.Length > ExtraPetalSpriteIndex;
    }

    private static void EnsureExtraPetal(KarmaFlower self)
    {
        var ext = self.GetFlowerExt();
        if (ext.extraPetal == null)
        {
            ext.extraPetal = new KarmaFlower.Part(self);
            ext.extraPetal.Reset();
        }
    }

    private static void ResetExtraPetal(KarmaFlower self)
    {
        EnsureExtraPetal(self);
        self.GetFlowerExt().extraPetal!.Reset();
    }

    private static void ApplyVoidColor(KarmaFlower self)
    {
        var ext = self.GetFlowerExt();
        if (!ext.voidRot)
            return;

        if (ext.toVoidColor < 1f)
            ext.toVoidColor = Mathf.Min(1f, ext.toVoidColor + 0.00025f);

        Color targetVoid = new(0f, 0f, 0.005f);
        SetFlowerColor(self, Color.Lerp(GetFlowerColor(self), targetVoid, ext.toVoidColor));
        SetStalkColor(self, Color.Lerp(GetStalkColor(self), targetVoid, ext.toVoidColor));
    }

    private static Vector2 GetPetalTarget(KarmaFlower self, int petalIndex, int totalPetals)
    {
        float step = 360f / totalPetals;
        float angle = step * petalIndex;
        float faceCamera = GetFaceCamera(self);

        return self.firstChunk.pos
               + self.rotation * 5.25f
               + Custom.FlattenVectorAlongAxis(
                   Custom.DegToVec(Custom.VecToDeg(self.rotation) + angle) * 9.75f,
                   Custom.VecToDeg(self.rotation),
                   faceCamera);
    }

    private static void UpdateExtraPetal(KarmaFlower self)
    {
        var ext = self.GetFlowerExt();
        if (ext.extraPetal == null)
            return;

        ext.extraPetal.Update();

        Vector2 target = GetPetalTarget(self, 4, VoidPetalCount);
        Vector2 toOwner = self.firstChunk.pos - ext.extraPetal.pos;
        Vector2 toTarget = self.firstChunk.pos - target;

        float val = 0f;
        if (toOwner != Vector2.zero && toTarget != Vector2.zero)
            val = Vector2.Dot(toOwner.normalized, toTarget.normalized);

        ext.extraPetal.vel = Vector2.Lerp(
            ext.extraPetal.vel,
            self.firstChunk.pos - self.firstChunk.lastPos,
            Custom.LerpMap(val, 1f, -1f, 0f, 1f)
        );

        ext.extraPetal.vel += (target - ext.extraPetal.pos) / Custom.LerpMap(val, -1f, 1f, 3f, 30f);
        ext.extraPetal.pos += (target - ext.extraPetal.pos) / Custom.LerpMap(val, -1f, 1f, 3f, 60f);

        if (!Custom.DistLess(self.firstChunk.pos, ext.extraPetal.pos, 13.5f))
        {
            Vector2 dir = Custom.DirVec(ext.extraPetal.pos, self.firstChunk.pos);
            float dist = Vector2.Distance(ext.extraPetal.pos, self.firstChunk.pos);
            ext.extraPetal.pos -= (13.5f - dist) * dir;
            ext.extraPetal.vel -= (13.5f - dist) * dir;
        }
    }

    private static void UpdateVoidPetals(KarmaFlower self)
    {
        float faceCamera = GetFaceCamera(self);

        for (int i = 0; i < VanillaPetalCount && i < self.petals.Length; i++)
        {
            self.petals[i].Update();

            Vector2 target = self.firstChunk.pos
                             + self.rotation * 5.25f
                             + Custom.FlattenVectorAlongAxis(
                                 Custom.DegToVec(Custom.VecToDeg(self.rotation) + (360f / VoidPetalCount) * i) * 9.75f,
                                 Custom.VecToDeg(self.rotation),
                                 faceCamera);

            Vector2 toOwner = self.firstChunk.pos - self.petals[i].pos;
            Vector2 toTarget = self.firstChunk.pos - target;

            float val = 0f;
            if (toOwner != Vector2.zero && toTarget != Vector2.zero)
                val = Vector2.Dot(toOwner.normalized, toTarget.normalized);

            self.petals[i].vel = Vector2.Lerp(
                self.petals[i].vel,
                self.firstChunk.pos - self.firstChunk.lastPos,
                Custom.LerpMap(val, 1f, -1f, 0f, 1f)
            );

            self.petals[i].vel += (target - self.petals[i].pos) / Custom.LerpMap(val, -1f, 1f, 3f, 30f);
            self.petals[i].pos += (target - self.petals[i].pos) / Custom.LerpMap(val, -1f, 1f, 3f, 60f);

            if (!Custom.DistLess(self.firstChunk.pos, self.petals[i].pos, 13.5f))
            {
                Vector2 dir = Custom.DirVec(self.petals[i].pos, self.firstChunk.pos);
                float dist = Vector2.Distance(self.petals[i].pos, self.firstChunk.pos);
                self.petals[i].pos -= (13.5f - dist) * dir;
                self.petals[i].vel -= (13.5f - dist) * dir;
            }
        }

        UpdateExtraPetal(self);
    }

    private static void KarmaFlower_NewRoom(On.KarmaFlower.orig_NewRoom orig, KarmaFlower self, Room newRoom)
    {
        orig(self, newRoom);
        ResetExtraPetal(self);
    }

    private static void KarmaFlower_ApplyPalette(On.KarmaFlower.orig_ApplyPalette orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);

        if (self.GetFlowerExt().voidRot)
            ApplyVoidColor(self);
    }

    private static void KarmaFlower_InitiateSprites(On.KarmaFlower.orig_InitiateSprites orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        EnsureExtraPetal(self);

        if (sLeaser?.sprites == null)
            return;

        if (sLeaser.sprites.Length >= TotalSpritesWithExtra)
            return;

        FSprite[] oldSprites = sLeaser.sprites;
        sLeaser.sprites = new FSprite[TotalSpritesWithExtra];

        for (int i = 0; i < oldSprites.Length; i++)
            sLeaser.sprites[i] = oldSprites[i];

        sLeaser.sprites[ExtraPetalSpriteIndex] = new FSprite("KarmaPetal", true)
        {
            anchorY = 0f,
            isVisible = false
        };

        self.AddToContainer(sLeaser, rCam, null);
    }

    private static void KarmaFlower_AddToContainer(On.KarmaFlower.orig_AddToContainer orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        orig(self, sLeaser, rCam, newContainer);

        if (!HasExtraSprite(sLeaser))
            return;

        FContainer container = newContainer ?? rCam.ReturnFContainer("Items");
        sLeaser.sprites[ExtraPetalSpriteIndex].RemoveFromContainer();
        container.AddChild(sLeaser.sprites[ExtraPetalSpriteIndex]);
    }

    private static void KarmaFlower_Update(On.KarmaFlower.orig_Update orig, KarmaFlower self, bool eu)
    {
        EnsureExtraPetal(self);

        if (self.grabbedBy.Count > 0 &&
            self.grabbedBy[0].grabber is Player player &&
            player.IsVoid() &&
            Karma11Update.VoidPermaNightmare)
        {
            var ext = self.GetFlowerExt();
            ext.voidRot = true;

            if (!ext.voidRotApplied)
            {
                ext.voidRotApplied = true;
                self.bites = Mathf.Max(self.bites, 5);
            }
        }

        orig(self, eu);

        if (self.GetFlowerExt().voidRot)
        {
            ApplyVoidColor(self);
            UpdateVoidPetals(self);
        }
    }

    private static void DrawExtraPetal(KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos, bool blink, Color flowerColor)
    {
        if (!HasExtraSprite(sLeaser))
            return;

        var ext = self.GetFlowerExt();
        if (ext.extraPetal == null)
        {
            sLeaser.sprites[ExtraPetalSpriteIndex].isVisible = false;
            return;
        }

        bool showExtraPetal = ext.voidRot && self.bites > 4;
        if (!showExtraPetal)
        {
            sLeaser.sprites[ExtraPetalSpriteIndex].isVisible = false;
            return;
        }

        Vector2 center = Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, timeStacker);
        Vector2 petalPos = Vector2.Lerp(ext.extraPetal.lastPos, ext.extraPetal.pos, timeStacker);

        FSprite sprite = sLeaser.sprites[ExtraPetalSpriteIndex];
        sprite.x = center.x - camPos.x;
        sprite.y = center.y - camPos.y;
        sprite.rotation = Custom.AimFromOneVectorToAnother(center, petalPos);
        sprite.scaleY = Vector2.Distance(center, petalPos) / 20f;
        sprite.scaleX = 0.375f;
        sprite.isVisible = true;
        sprite.color = blink ? self.blinkColor : flowerColor;
    }

    private static void DrawVoidFlower(
        KarmaFlower self,
        RoomCamera.SpriteLeaser sLeaser,
        float timeStacker,
        Vector2 camPos)
    {
        bool blink = self.blink > 0 && Random.value < 0.5f;
        Vector2 center = Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, timeStacker);

        Color flowerColor = GetFlowerColor(self);
        Color stalkColor = GetStalkColor(self);

        Vector2 average = center;
        int averageCount = 1;

        for (int i = 0; i < VanillaPetalCount; i++)
        {
            if (i < self.bites)
            {
                Vector2 petalPos = Vector2.Lerp(self.petals[i].lastPos, self.petals[i].pos, timeStacker);
                FSprite sprite = sLeaser.sprites[self.PetalSprite(i)];

                sprite.x = center.x - camPos.x;
                sprite.y = center.y - camPos.y;
                sprite.rotation = Custom.AimFromOneVectorToAnother(center, petalPos);
                sprite.scaleY = Vector2.Distance(center, petalPos) / 20f;
                sprite.scaleX = 0.375f;
                sprite.isVisible = true;
                sprite.color = blink ? self.blinkColor : flowerColor;

                average += petalPos;
                averageCount++;
            }
            else
            {
                sLeaser.sprites[self.PetalSprite(i)].isVisible = false;
            }
        }

        if (self.GetFlowerExt().extraPetal != null && self.bites > 4)
        {
            Vector2 extraPos = Vector2.Lerp(self.GetFlowerExt().extraPetal!.lastPos, self.GetFlowerExt().extraPetal.pos, timeStacker);
            average += extraPos;
            averageCount++;
        }

        average /= averageCount;

        DrawExtraPetal(self, sLeaser, timeStacker, camPos, blink, flowerColor);

        sLeaser.sprites[self.StalkSprite].color = blink ? Color.white : flowerColor;

        if (self.RingSprite >= 0 && self.RingSprite < sLeaser.sprites.Length)
            sLeaser.sprites[self.RingSprite].isVisible = false;

        for (int j = 0; j < 3; j++)
        {
            sLeaser.sprites[self.EffectSprite(j)].x = average.x - camPos.x;
            sLeaser.sprites[self.EffectSprite(j)].y = average.y - camPos.y;
        }

        float t = Mathf.InverseLerp(0f, 5f, self.bites);
        float movement = GetMovement(self);
        float lastMovement = GetLastMovement(self);

        sLeaser.sprites[self.EffectSprite(0)].scale = 75f * Mathf.Lerp(0.5f, 1f, t) / 16f;
        sLeaser.sprites[self.EffectSprite(0)].alpha = blink ? 0f : (0.4f * (1f - Mathf.Lerp(lastMovement, movement, timeStacker)) * Mathf.Lerp(0.5f, 1f, t));
        sLeaser.sprites[self.EffectSprite(0)].color = Custom.HSL2RGB(RainWorld.AntiGold.hue, 0.6f, 0.2f);

        sLeaser.sprites[self.EffectSprite(1)].scale = (blink ? 20f : 40f) * Mathf.Lerp(0.5f, 1f, t) / 16f;
        sLeaser.sprites[self.EffectSprite(1)].alpha = (blink ? 0.5f : 0.7f) * Mathf.Lerp(0.5f, 1f, t);
        sLeaser.sprites[self.EffectSprite(1)].color = blink ? Color.white : flowerColor;

        sLeaser.sprites[self.EffectSprite(2)].scale = 40f * Mathf.Lerp(0.5f, 1f, t) / 16f;
        sLeaser.sprites[self.EffectSprite(2)].alpha = blink ? 0f : (0.8f * Mathf.Lerp(0.5f, 1f, t));

        Vector2 last = center;
        float width = 0.75f;
        TriangleMesh stalkMesh = sLeaser.sprites[self.StalkSprite] as TriangleMesh;

        for (int k = 0; k < self.stalk.Length; k++)
        {
            Vector2 stalkPos = Vector2.Lerp(self.stalk[k].lastPos, self.stalk[k].pos, timeStacker);
            Vector2 normalized = (stalkPos - last).normalized;
            Vector2 perp = Custom.PerpendicularVector(normalized);
            float d2 = Vector2.Distance(stalkPos, last) / 5f;

            if (k == 0)
            {
                stalkMesh.MoveVertice(k * 4, last - perp * width - camPos);
                stalkMesh.MoveVertice(k * 4 + 1, last + perp * width - camPos);
            }
            else
            {
                stalkMesh.MoveVertice(k * 4, last - perp * width + normalized * d2 - camPos);
                stalkMesh.MoveVertice(k * 4 + 1, last + perp * width + normalized * d2 - camPos);
            }

            stalkMesh.MoveVertice(k * 4 + 2, stalkPos - perp * width - normalized * d2 - camPos);
            stalkMesh.MoveVertice(k * 4 + 3, stalkPos + perp * width - normalized * d2 - camPos);
            last = stalkPos;
        }

        for (int i = 0; i < stalkMesh.verticeColors.Length; i++)
        {
            float t2 = (float)i / (stalkMesh.verticeColors.Length - 1);
            stalkMesh.verticeColors[i] = Color.Lerp(blink ? Color.white : flowerColor, stalkColor, t2);
        }
    }

    private static void KarmaFlower_DrawSprites(
        On.KarmaFlower.orig_DrawSprites orig,
        KarmaFlower self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera rCam,
        float timeStacker,
        Vector2 camPos)
    {
        if (self.slatedForDeletetion || self.room != rCam.room)
        {
            sLeaser?.CleanSpritesAndRemove();
            return;
        }

        if (!self.GetFlowerExt().voidRot)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (HasExtraSprite(sLeaser))
                sLeaser.sprites[ExtraPetalSpriteIndex].isVisible = false;

            return;
        }

        if (sLeaser?.sprites == null)
            return;

        DrawVoidFlower(self, sLeaser, timeStacker, camPos);
    }

    public static bool SaveVoidCycle = false;

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        var ext = self.GetPlayerExt();
        ext.voidPoisonBody = false;
        ext.voidPoisonWeaknessApplied = false;

        if (self.abstractCreature.world.game.IsVoidStoryCampaign())
            SaveVoidCycle = false;
    }

    private static void Plyaer_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        var ext = self.GetPlayerExt();

        bool shouldBeWeakened = !self.AreVoidViy() && ext.voidPoisonBody;

        if (shouldBeWeakened && !ext.voidPoisonWeaknessApplied)
        {
            self.SetMalnourished(true, true);
            ext.voidPoisonWeaknessApplied = true;
        }
        else if (!shouldBeWeakened && ext.voidPoisonWeaknessApplied)
        {
            self.SetMalnourished(false, true);
            ext.voidPoisonWeaknessApplied = false;
        }

        if (shouldBeWeakened && self.playerState != null)
        {
            self.playerState.permanentDamageTracking += 0.000125f;
            if (self.playerState.permanentDamageTracking >= 1.0f)
            {
                self.Die();
            }
        }
    }

    private static int Player_FoodInRoom_Room_bool(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
    {
        int result = orig(self, checkRoom, eatAndDestroy);
        if (self.IsVoid() && checkRoom.game.IsStorySession)
            checkRoom.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = false;
        return result;
    }

    private static void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && !player.AreVoidViy() && self.GetFlowerExt().voidRot)
        {
            self.bites--;
            self.room.PlaySound((self.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk);
            self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);

            if (self.bites < 1)
            {
                self.room.game.cameras[0].hud.karmaMeter.blinkRed = true;
                self.room.game.cameras[0].hud.karmaMeter.blinkRedCounter = 240;

                if (player.room.game.session is StoryGameSession &&
                    !(player.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma)
                {
                    if (!player.abstractCreature.world.game.IsVoidStoryCampaign())
                    {
                        if ((player.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma > 0)
                        {
                            (player.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma--;
                            for (int rooms = 0; rooms < player.room.game.cameras.Length; rooms++)
                                player.room.game.cameras[rooms].hud.karmaMeter?.UpdateGraphic();
                        }
                    }
                }
                player.room.PlaySound(Utils.ViyVoiceBad(), player.bodyChunks[0]);
                player.GetPlayerExt().voidPoisonBody = true;
                player.SaintStagger(240);
                player.Stun(240);
                grasp.Release();
                self.Destroy();
            }

            return;
        }

        if (grasp.grabber is Player player2 && ((player2.IsVoid() && self.bites < 2) || player2.IsViy()))
        {
            var saveState = player2.abstractCreature?.world?.game?.GetStorySession.saveState;
            if (saveState != null)
            {
                self.room.PlaySound(self.bites == 0 ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk.pos);
                self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);

                if (Random.Range(0, 3) == 0)
                    saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidNSH);

                if (player2.abstractCreature.world.game.IsVoidStoryCampaign())
                {
                    if (player2.KarmaCap != 10 && !saveState.GetVoidMarkV3() && !SaveVoidCycle && OptionAccessors.PermaDeath)
                    {
                        SaveVoidCycle = true;
                        self.room.game.cameras[0].hud.karmaMeter.blinkRed = true;
                        self.room.game.cameras[0].hud.karmaMeter.blinkRedCounter = 300;
                        HunterSpasms.Spasm(player2, 10f, 0.5f);

                        if (!saveState.GetKarmaFlowerMessageShown())
                        {
                            self.room.AddObject(new Tutorial(self.room,
                            [
                                new("It is painful... but Karma Flower saves your current cycle.", 222, 333)
                            ]));
                            saveState.SetKarmaFlowerMessageShown(true);
                        }
                    }

                    if (self.bites == 1 && player2.KarmaCap == 10 && !Karma11Update.VoidPermaNightmare && !player2.IsViy())
                    {
                        int newTokenCount = Math.Min(5, saveState.GetKarmaToken() + 1);
                        saveState.SetKarmaToken(newTokenCount);

                        bool needBumpTokenAnim = Karma11Foundation.Karma11Symbol.currentKarmaTokens != 5;
                        Karma11Foundation.Karma11Symbol.currentKarmaTokens = (ushort)newTokenCount;

                        if (needBumpTokenAnim)
                            self.room.game.cameras[0].hud.karmaMeter.reinforceAnimation = 0;
                    }
                }
            }

            grasp.Release();
            self.Destroy();
            return;
        }

        if (grasp.grabber is Player player3 && player3.abstractCreature.world.game.IsVoidStoryCampaign())
        {
            self.bites--;
            self.room.PlaySound((self.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk);
            self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);

            if (self.bites < 1)
            {
                grasp.Release();
                self.Destroy();
            }

            return;
        }

        orig(self, grasp, eu);
    }

    public class KarmaFlowerExtention
    {
        public float toVoidColor;
        public bool voidRot;
        public bool voidRotApplied;
        public KarmaFlower.Part extraPetal;
    }

    public class PlayerExtention
    {
        public bool voidPoisonBody = false;
        public bool voidPoisonWeaknessApplied = false;
    }
}