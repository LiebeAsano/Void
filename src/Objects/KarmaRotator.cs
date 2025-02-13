using HUD;
using UnityEngine;

namespace VoidTemplate.Objects;

/// <summary>
/// Rotates karma symbol. Note: time to do this shouldn't be more than 10s
/// </summary>
public class KarmaRotator : UpdatableAndDeletable
{
	
	private FSprite karmaSprite => karmaMeter.karmaSprite;
	private readonly int ticksToRotate;
	private readonly float rotationDegrees;
    private readonly KarmaMeter karmaMeter;


    /// <param name="room">room that takes the role of updater. should be slugcat room</param> 
    /// <param name="secondsToRotate">how many seconds it would take to rotate karma</param>
    /// <param name="rotationDegrees">how many degrees the rotation would use</param>
    public KarmaRotator(Room room, float secondsToRotate = 3f, float rotationDegrees = 72f) : this(room, room.game.cameras[0].hud, secondsToRotate, rotationDegrees)
	{}
	private KarmaRotator(Room room, HUD.HUD hud, float secondsToRotate, float rotationDegrees)
	{
		//RW works at 40 ticks per second
		this.ticksToRotate = (int)(secondsToRotate * 40);
		this.rotationDegrees = rotationDegrees;
		this.karmaMeter = hud.karmaMeter;
		this.room = room;
		room.AddObject(this);
	}

	private const float xNormalization = 4.89898f;
	private const float yNormalization = 78.38367f;
	//the function, the speed of change of which abides by -x^2+24
	//integrated to -(x^3)/3+24x
	//and then normalized to its peak at (xNormalization;yNormalization)
	private static float FunctionOfRotationProgress(float progress) => (- Mathf.Pow(progress * xNormalization, 3) / 3f
	                                                                    + 24f * progress * xNormalization ) / yNormalization;

	private int lifetimeTicks;
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (lifetimeTicks == 0) karmaMeter.reinforceAnimation = 0;
		//normalizing lifetime progression
		float progress = lifetimeTicks / (float)ticksToRotate;
		//getting fancy curve that dictates rotation progression
		float rotationProgress = FunctionOfRotationProgress(progress);
		//un-normalizing rotation progression
		karmaSprite.rotation = rotationProgress * rotationDegrees;
		lifetimeTicks++;
		if (lifetimeTicks > ticksToRotate)
		{
			slatedForDeletetion = true;
			karmaSprite.rotation = 0;
        }
	}
}