using UnityEngine;
using static UnityEngine.Mathf;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.Objects;

public class HunterSpasms(Player player, float length = 5f, float severity = 1f) : UpdatableAndDeletable
{
    public static void Spasm(Player player, float length = 5f, float severity = 1f)
    {
        player.room.AddObject(new HunterSpasms(player, length, severity));
    }

    #region Mutable
    float progress;
    bool init;
    float ResultingPower => Pow(Clamp01(Sin(progress * PI) * 1.2f), 1.6f) * severity; 
    #endregion

    #region Immutable
    readonly int tickLength = (int)(length * TicksPerSecond);
    RedOverlay redOverlay;
    #endregion

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!init)
        {
            init = true;
            player.SetMalnourished(true);
        }

        player.aerobicLevel = Max(player.aerobicLevel, Pow(ResultingPower, 1.5f));
        if (ResultingPower > 0.7f) player.Blink(6);
        
        if (redOverlay is null)
        {
            redOverlay = new RedOverlay();
            room.AddObject(redOverlay);
        }
        redOverlay.pos = player.mainBodyChunk.pos;
        redOverlay.strength = ResultingPower;
        redOverlay.rotationIntensity = 0.1f + 0.9f * InverseLerp(1f, 4f, Vector2.Distance(player.firstChunk.lastLastPos, player.firstChunk.pos));
        
        progress += 1f / tickLength;
        if (progress >= 1f)
        {
            slatedForDeletetion = true;
            redOverlay.slatedForDeletetion = true;
            redOverlay = null;
        }
        
    }
}