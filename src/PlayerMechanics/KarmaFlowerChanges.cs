using System;
using VoidTemplate.Objects;
using VoidTemplate.Useful;
using Random = UnityEngine.Random;

namespace VoidTemplate.PlayerMechanics;

public class KarmaFlowerChanges
{
    public static void Initiate()
    {
        On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
        On.Player.FoodInRoom_Room_bool += Player_FoodInRoom_Room_bool;
    }

    private static int Player_FoodInRoom_Room_bool(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
    {
        var result = orig(self, checkRoom, eatAndDestroy);
        if (self.IsVoid() && checkRoom.game.IsStorySession)
            checkRoom.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = false;
        return result;
    }

    private static void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && ((player.IsVoid() && self.bites < 2) || player.IsViy()))
        {
            var saveState = player.abstractCreature?.world?.game?.GetStorySession.saveState;
            if (saveState != null)
            {
                self.room.PlaySound(self.bites == 1 ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk.pos);
                self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);

                if (Random.Range(0, 3) == 0)
                    saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidNSH);

                if (player.abstractCreature.world.game.IsVoidStoryCampaign())
                {
                    if (player.KarmaCap != 10 && !saveState.GetVoidMarkV3())
                    {
                        saveState.SetVoidExtraCycles(saveState.GetVoidExtraCycles() + 1);
                        HunterSpasms.Spasm(player, 5f, 0.2f);
                    }

                    if (self.bites == 1 && player.KarmaCap == 10 && !player.IsViy())
                    {
                        int newTokenCount = Math.Min(10, saveState.GetKarmaToken() + 2);
                        saveState.SetKarmaToken(newTokenCount);

                        bool needBumpTokenAnim = Karma11Foundation.Karma11Symbol.currentKarmaTokens != 10;
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
        orig(self, grasp, eu);
    }
}