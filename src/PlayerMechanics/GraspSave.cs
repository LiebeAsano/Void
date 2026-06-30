using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

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

        if (self is not Player player || !player.AreVoidViy())
        {
            if (self?.grasps == null || self.grasps.Length == 0)
                return;

            for (int i = 0; i < self.grasps.Length; i++)
            {
                Creature.Grasp grasp = self.grasps[i];
                if (grasp == null)
                    continue;

                if (grasp.grabbed is not Player playerInGrasp || !playerInGrasp.AreVoidViy())
                    continue;

                self.SetKillTag(playerInGrasp.abstractCreature);

                if (self is not Player)
                {
                    if (self.State is HealthState healthState)
                    {
                        healthState.health -= 0.00025f;

                        if (self.Template.quickDeath &&
                            (Random.value < -healthState.health ||
                             healthState.health < -1f ||
                             (healthState.health < 0f && Random.value < 0.33f)))
                        {
                            self.Die();
                        }
                    }
                }
                else if (self is Player grabbedPlayer && !grabbedPlayer.AreVoidViy())
                {
                    if (grabbedPlayer.playerState != null)
                    {
                        if (grabbedPlayer.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher &&
                            grabbedPlayer.room?.game?.GetStorySession.saveState.miscWorldSaveData.hasVoidWeaverAbility == true)
                        {
                            grabbedPlayer.SetHaloDisplayTime(20);
                        }
                        else
                        {
                            grabbedPlayer.playerState.permanentDamageTracking += 0.00025f;
                            if (grabbedPlayer.playerState.permanentDamageTracking >= 1f)
                            {
                                self.Die();
                            }
                        }
                    }
                }

                if (playerInGrasp.input != null &&
                    playerInGrasp.input.Length > 1 &&
                    playerInGrasp.input[0].pckp &&
                    !playerInGrasp.input[1].pckp &&
                    !playerInGrasp.dead &&
                    Random.Range(0, playerInGrasp.IsViy() ? 100 : Karma11Update.VoidKarma11 ? 150 : 200) == 0)
                {
                    if (self.room != null && self.mainBodyChunk != null)
                    {
                        self.Stun(20);
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                        self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        self.Violence
                        (
                            self.mainBodyChunk,
                            new Vector2?(Vector2.zero),
                            self.mainBodyChunk,
                            null,
                            Creature.DamageType.Bite,
                            1f,
                            30f
                        );
                    }
                }
            }
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