using VoidTemplate.Objects;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class Spasm
{
    public static void Hook()
    {
        On.RainWorldGame.Update += RainWorldGame_Update;
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (self?.Players == null) return;

        for (int i = 0; i < self.Players.Count; i++)
        {
            if (self.Players[i]?.realizedCreature is not Player player || player.slugcatStats == null) continue;

            if (self.IsVoidStoryCampaign() && player.IsVoid() && !player.dead)
            {
                if (self.GetStorySession?.saveState == null) continue;

                if (player.KarmaCap > 3
                    && !Karma11Update.VoidKarma11
                    && !self.GetStorySession.saveState.GetVoidMarkV3()
                    && !KarmaFlowerChanges.SaveVoidCycle)
                {
                    float MaxSize = self.GetStorySession.saveState.deathPersistentSaveData.theMark || player.KarmaCap == 10 ? 110000f : 220000f;
                    if (player.KarmaCap == 9)
                        MaxSize /= self.world.region.name == "SL" || self.world.region.name == "MS" ? 2 : 1;
                    float Lenght = player.KarmaCap == 10 ? 5f : 10f;
                    MaxSize = MaxSize * 0.1f * player.KarmaCap;

                    if (VoidCycleLimit.YieldVoidCycleDisplayNumberWithPlayer(player, self.GetStorySession.saveState.cycleNumber) < 10 && OptionAccessors.PermaDeath)
                    {
                        MaxSize = MaxSize * VoidCycleLimit.YieldVoidCycleDisplayNumberWithPlayer(player, self.GetStorySession.saveState.cycleNumber) / 10;
                        Lenght = 20f;
                    }

                    float random = UnityEngine.Random.Range(1, MaxSize);
                    random = (int)random;
                    if (random == 1)
                    {
                        self.GetStorySession.saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Rot);
                        HunterSpasms.Spasm(player, Lenght, 1f);
                        RedOverlay overlay = null;
                        if (player.room != null)
                        {
                            for (int j = 0; j < player.room.updateList.Count; j++)
                            {
                                if (player.room.updateList[j] is RedOverlay redOver)
                                {
                                    overlay = redOver;
                                    break;
                                }
                            }
                        }

                        if (overlay != null && overlay.ViyVoice)
                        {
                            foreach (AbstractCreature creature in player.room.abstractRoom.creatures)
                            {
                                if (creature.realizedCreature is not Player)
                                    creature.realizedCreature?.Stun(120);
                                if (creature.realizedCreature is Player player2 && !player2.AreVoidViy())
                                    player.SaintStagger(120);
                            }
                            overlay.ViyVoice = false;
                        }

                        if (self.GetStorySession.saveState.deathPersistentSaveData.theMark)
                        {
                            if (!self.GetStorySession.saveState.GetViyMarkAvoidMessage())
                            {
                                player.room.AddObject(new Tutorial(player.room,
                                [
                                    new("Foreign flesh...", 0, 222),
                                    new("Must be removed...", 0, 222),
                                    new("Press 'Down' and 'Grab' to get rid of the mark of communication.", 0, 444)
                                ]));
                                self.GetStorySession.saveState.SetViyMarkAvoid(true);
                                self.GetStorySession.saveState.SetViyMarkAvoidMessage(true);
                            }
                        }
                        else if (self.GetStorySession.saveState.deathPersistentSaveData.karmaCap != 10)
                        {
                            if (self.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 9)
                            {
                                if (self.world.region.name == "MS")
                                {
                                    switch (UnityEngine.Random.Range(0, 3))
                                    {
                                        case 0:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Higher...", 0, 222),
                                                    new("Climb even higher...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 1:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Very close...", 0, 222),
                                                    new("An alluring howl...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 2:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Nearby...", 0, 222),
                                                    new("The Void Sea is crying out...", 0, 222),
                                                ]));
                                                break;
                                            }
                                    }
                                }
                                else if (self.world.region.name == "SL")
                                {
                                    switch (UnityEngine.Random.Range(0, 3))
                                    {
                                        case 0:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Close...", 0, 222),
                                                    new("Deeper under the water...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 1:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Feel it...", 0, 222),
                                                    new("The ground is falling out...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 2:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("The hum is getting louder...", 0, 222),
                                                ]));
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    switch (UnityEngine.Random.Range(0, 6))
                                    {
                                        case 0:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Cold...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 1:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("The ruins...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 2:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("The forgotten...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 3:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("The fallen...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 4:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Lonely...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 5:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Last...", 0, 222),
                                                ]));
                                                break;
                                            }
                                    }
                                }
                            }
                            else
                            {
                                if (self.world.region.name == "MS")
                                {
                                    switch (UnityEngine.Random.Range(0, 6))
                                    {
                                        case 0:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Makes sleepy...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 1:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Feel the pressure...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 2:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Tediously...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 3:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Cannot...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 4:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Too early...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 5:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Freezing...", 0, 222),
                                                ]));
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    switch (UnityEngine.Random.Range(0, 12))
                                    {
                                        case 0:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Move...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 1:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Free...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 2:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Joy...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 3:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Endless hunger...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 4:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Hurts so much...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 5:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("See the light...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 6:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("So familiar...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 7:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Do not want be part of...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 8:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("What is rot...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 9:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("The gift of reason...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 10:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Born to cling...", 0, 222),
                                                ]));
                                                break;
                                            }
                                        case 11:
                                            {
                                                player.room.AddObject(new Tutorial(player.room,
                                                [
                                                    new("Last...", 0, 222),
                                                ]));
                                                break;
                                            }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }
    }
}