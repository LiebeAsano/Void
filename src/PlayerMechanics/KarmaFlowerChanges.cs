using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;
using Random = UnityEngine.Random;

namespace VoidTemplate.PlayerMechanics;

public static class KarmaFlowerChanges
{
    private static ConditionalWeakTable<KarmaFlower, KarmaFlowerExtention> flowerExt = new();
    public static KarmaFlowerExtention GetFlowerExt(this KarmaFlower flower) => flowerExt.GetOrCreateValue(flower);

    public static void Initiate()
    {
        On.Player.ctor += Player_ctor;
        On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
        On.Player.FoodInRoom_Room_bool += Player_FoodInRoom_Room_bool;
        On.KarmaFlower.Update += KarmaFlower_Update;
    }

    private static void KarmaFlower_Update(On.KarmaFlower.orig_Update orig, KarmaFlower self, bool eu)
    {
        orig(self, eu);
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player && player.IsVoid() && 
            self.AbstrConsumable.world.game.session is StoryGameSession session && session.saveState.GetVoidFoodToHibernate() == 6)
        {
            if (self.GetFlowerExt().toVoidColor < 1) self.GetFlowerExt().toVoidColor += 0.00002f;
            Color voidColor = new(0, 0, 0.005f);
            self.color = Color.Lerp(self.color, voidColor, self.GetFlowerExt().toVoidColor);
            self.stalkColor = Color.Lerp(self.stalkColor, voidColor, self.GetFlowerExt().toVoidColor);
        }
    }

    public static bool SaveVoidCycle = false;
    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (self.abstractCreature.world.game.IsVoidStoryCampaign())
        {
            SaveVoidCycle = false;
        }
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
                    if (player.KarmaCap != 10 && !saveState.GetVoidMarkV3() && !SaveVoidCycle)
                    {
                        SaveVoidCycle = true;
                        saveState.SetVoidExtraCycles(saveState.GetVoidExtraCycles() + 1);
                        self.room.game.cameras[0].hud.karmaMeter.blinkRed = true;
                        self.room.game.cameras[0].hud.karmaMeter.blinkRedCounter = 300;
                        HunterSpasms.Spasm(player, 10f, 0.5f);

                        if (!saveState.GetKarmaFlowerMessageShown())
                        {
                            self.room.AddObject(new Tutorial(self.room,
                            [
                                new("It is painful... but Karma Flower saves your current cycle.", 222, 333)
                            ]));
                            saveState.SetKarmaFlowerMessageShown(true);
                        }
                    }

                    if (self.bites == 1 && player.KarmaCap == 10 && saveState.GetVoidFoodToHibernate() < 6 && !player.IsViy())
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
        if (grasp.grabber is Player player2 && self.bites < 2 && player2.abstractCreature.world.game.IsVoidStoryCampaign())
        {
            grasp.Release();
            self.Destroy();
            return;
        }
        orig(self, grasp, eu);
    }

    public class KarmaFlowerExtention
    {
        public float toVoidColor;
    }
}