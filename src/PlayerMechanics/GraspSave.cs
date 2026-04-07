using System;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

public static class GraspSave
{

	public static void Hook()
	{
		On.Creature.Update += Creature_Update;
        On.Player.checkInput += Player_checkInput;
    }

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
	{
		orig(self, eu);
		if (!(self is Player player && player.AreVoidViy()))
		{
			Array.ForEach(self.grasps, grasp =>
			{
				if (grasp != null
				&& grasp.grabbed is Player playerInGrasp
				&& playerInGrasp.AreVoidViy())
				{
                    self.SetKillTag(playerInGrasp.abstractCreature);
					if (self is not null && self is not Player)
					{
						if (self.State is HealthState)
						{
							(self.State as HealthState).health -= 0.00025f;
							if (self.Template.quickDeath && (UnityEngine.Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || ((self.State as HealthState).health < 0f && UnityEngine.Random.value < 0.33f)))
							{
								self.Die();
							}
						}
					}
					else if (self is Player player && !player.AreVoidViy())
					{
						if (player.playerState is not null)
						{
							player.playerState.permanentDamageTracking += 0.00025f;
							if (player.playerState.permanentDamageTracking >= 1.0f)
							{
								self.Die();
							}
						}
					}

                    if (playerInGrasp.input[0].pckp && !playerInGrasp.input[1].pckp && !playerInGrasp.dead && UnityEngine.Random.Range(0, Karma11Update.VoidKarma11 ? 150 : 250) == 0)
					{
						self.Stun(20);
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                        self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        self.Violence(self.mainBodyChunk, new Vector2?(new Vector2(0f, 0f)), self.mainBodyChunk, null, Creature.DamageType.Bite, 1f, 30f);
					}
				}
			});
		}
	}

    private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        bool shouldReadInputInStun =
            self.AreVoidViy() &&
            !self.dead &&
            self.stun > 0 &&
            self.grabbedBy != null &&
            self.grabbedBy.Count > 0;

        if (!shouldReadInputInStun)
        {
            orig(self);
            return;
        }

        int savedStun = self.stun;

        self.stun = 0;
        orig(self);
        self.stun = savedStun;
    }
}
	

