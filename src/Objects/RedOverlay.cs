using UnityEngine;

namespace VoidTemplate.Objects;
using static RWCustom.Custom;
using static Mathf;

public class RedOverlay : CosmeticSprite
{
    float lastFade;
    float lastViableFade;
    float lastRot;
    float fade;
    float viableFade;
    float rot;
    float sin;
    float fluctuation1;
    float fluctuation2;
    float fluctuation3;
    float fluctuation4;

    public float strength;
    public float rotationIntensity;
    float rotDir;
    DisembodiedDynamicSoundLoop soundLoop;
    
    public override void Update(bool eu)
    {
        base.Update(eu);
        lastFade = fade;
        lastViableFade = viableFade;
        lastRot = rot;
        sin += 1f / Lerp(120f, 30f, fluctuation4);
        
        fluctuation1 = LerpAndTick(fluctuation1, fluctuation2, 1 / 50f, 1 / 60f);
        fluctuation2 = LerpAndTick(fluctuation2, fluctuation3, 1 / 50f, 1 / 60f);
        fluctuation3 = LerpAndTick(fluctuation3, fluctuation4, 1 / 50f, 1 / 60f);
        if(Abs(fluctuation3 - fluctuation4) < 1/100f) fluctuation4 = Random.value;
        
        fade = Pow(strength * (0.85f + 0.15f * Sin(sin * PI * 2f)), Lerp(1.5f, 0.5f, fluctuation1));
        rot += rotDir * fade * (1f + fluctuation1) * 3.5f * rotationIntensity;
        viableFade = Min(1f, viableFade + 1 / 30f);
        
        if(fade == 0f && lastFade > 0f) rotDir = Random.value < 0.5f ? -1f : 1f;

        SoundID meow = SoundID.None;

        switch (Random.Range(0, 5))
        {
            case 0:
                meow = Watcher.WatcherEnums.WatcherSoundID.RotLiz_Vocalize;
                break;
            case 1:
                meow = Watcher.WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_A;
                break;
            case 2:
                meow = Watcher.WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_B;
                break;
            case 3:
                meow = Watcher.WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_B;
                break;
            case 4:
                meow = Watcher.WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_B;
                break;
        }

        if (soundLoop is null && fade > 0f)
        {
            soundLoop = new DisembodiedDynamicSoundLoop(this)
            {
                sound = meow,
                VolumeGroup = 1
            };
        }
        else if (soundLoop is not null)
        {
            soundLoop.Update();
            soundLoop.Volume = LerpAndTick(soundLoop.Volume, Pow((fade + strength) / 8f, 0.5f), 0.06f, 1 / 7f);
        }
        
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        base.InitiateSprites(sLeaser, rCam);
        sLeaser.sprites = [new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["RedsIllness"] }];
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        var sprite = sLeaser.sprites[0];
        float totalFade = TotalFade(timeStacker);
        if(totalFade == 0) sprite.isVisible = false;
        else
        {
            sprite.isVisible = true;
            Vector2 position = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;
            sprite.x = Clamp(position.x, 0f, rCam.sSize.x);
            sprite.y = Clamp(position.y, 0f, rCam.sSize.y);
            sprite.rotation = Lerp(lastRot, rot, timeStacker);
            sprite.scaleX = (rCam.sSize.x * (6f - 3f * totalFade) + 2f) / 16f;
            sprite.scaleY = (rCam.sSize.y * (6f - 3f * totalFade) + 2f) / 16f;
            sprite.color = new Color(totalFade, totalFade, 0f, 0f);
        }
    }

    float TotalFade(float timeStacker) => Lerp(lastFade, fade, timeStacker) * Lerp(lastViableFade, viableFade, timeStacker);
}