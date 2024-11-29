using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace VoidTemplate.Oracles;
/// <summary>
/// HoveringPeral is intended to be used when iterator uses pearl to hover it
/// </summary>
internal class HoveringPearl : DataPearl
{
    public HoveringPearl(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
    {
        collisionLayer = 0;
    }
    public bool lastCarried;
    public bool Carried => grabbedBy.Count > 0;

    float beatScale;
    public Vector2? hoverPos;
    public event Action OnPearlTaken;
    public event Action OnWaitCompleted;

    public override void Update(bool eu)
    {
        base.Update(eu);
        if(Carried 
            && !lastCarried 
            && hoverPos != null)
        {
            hoverPos = null;
            OnPearlTaken?.Invoke();
            beatScale = 0f;
            gravity = 0.9f;
        }
        lastCarried = Carried;
        if (hoverPos != null)
        {
            firstChunk.vel *= Custom.LerpMap(firstChunk.vel.magnitude, 1f, 6f, 0.99f, 0.8f);
            firstChunk.vel += Vector2.ClampMagnitude(hoverPos.Value - firstChunk.pos, 100f) / 100f * 0.4f;
            gravity = 0f;
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        base.InitiateSprites(sLeaser, rCam);
        Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length+1);
        var lastIndexOfResizedArray = sLeaser.sprites.Length - 1;
        sLeaser.sprites[lastIndexOfResizedArray] = new FSprite("LizardBubble6", true);
        AddToContainer(sLeaser, rCam, null);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
        var lizardSprite = sLeaser.sprites[sLeaser.sprites.Length - 1];
        lizardSprite.x = vector.x;
        lizardSprite.y = vector.y;
        lizardSprite.scale = beatScale * 0.75f;
        lizardSprite.color = Color.red;
        lizardSprite.alpha = 0.25f + beatScale * 0.65f;
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }
    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);
        if (firstContact && speed > 2f)
        {
            room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, firstChunk, false, Custom.LerpMap(speed, 0f, 8f, 0.2f, 1f), 1f);
        }
    }

    public async void AsyncHover(int delay)
    {
        await Task.Delay(delay);
        hoverPos = null;
    }
    public async void AsyncWait(int delay)
    {
        await Task.Delay(delay);
        OnWaitCompleted.Invoke();
    }

}
