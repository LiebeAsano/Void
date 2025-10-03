using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate
{
    public static class AddQuaterFood
    {
        public static void Hook()
        {
            On.Player.AddFood += Player_AddFood;
            On.Player.AddQuarterFood += Player_AddQuarterFood;
            On.Player.JollyFoodUpdate += Player_JollyFoodUpdate;
        }

        private static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
        {
            if (self.slugcatStats.name != VoidEnums.SlugcatID.Void && self.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                orig(self, add);
                return;
            }

            if (ModManager.CoopAvailable && self.abstractCreature.world.game.IsStorySession &&
                self.abstractCreature.world.game.Players[0] != self.abstractCreature && !self.isNPC)
            {
                if (self.abstractCreature?.world?.game?.Players?[0]?.realizedCreature is Player mainPlayer)
                {
                    mainPlayer.AddFood(add);
                }
                else
                {
                    orig(self, add);
                }
            }
            else
            {
                add = Math.Min(add, self.MaxFoodInStomach - self.playerState.foodInStomach);

                if (self.abstractCreature.world.game.IsStorySession && self.AI == null)
                {
                    self.abstractCreature.world.game.GetStorySession.saveState.totFood += add;
                }

                self.playerState.foodInStomach += add;
            }

            if (self.FoodInStomach >= self.MaxFoodInStomach)
            {
                self.playerState.quarterFoodPoints = 0;
            }

            if (self.slugcatStats.malnourished && self.playerState.foodInStomach >=
                (self.redsIllness != null ? self.redsIllness.FoodToBeOkay : self.slugcatStats.maxFood))
            {
                if (self.redsIllness != null)
                {
                    self.redsIllness.GetBetter();
                    return;
                }
                if (!self.isSlugpup)
                {
                    self.SetMalnourished(false);
                }
                if (self.playerState is PlayerNPCState)
                {
                    (self.playerState as PlayerNPCState).Malnourished = false;
                }
            }
        }

        private static void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
        {
            if (self.slugcatStats.name != VoidEnums.SlugcatID.Void && self.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                orig(self);
                return;
            }

            if (self.FoodInStomach >= self.MaxFoodInStomach)
            {
                return;
            }

            if (ModManager.CoopAvailable && self.abstractCreature.world.game.IsStorySession &&
                self.abstractCreature.world.game.Players[0] != self.abstractCreature && !self.isNPC)
            {
                if (self.abstractCreature?.world?.game?.Players?[0]?.realizedCreature is Player mainPlayer)
                {
                    mainPlayer.AddQuarterFood();
                }
                else
                {
                    orig(self);
                }
            }
            else
            {
                self.playerState.quarterFoodPoints++;
                if (self.playerState.quarterFoodPoints > 3)
                {
                    self.playerState.quarterFoodPoints -= 4;
                    self.AddFood(1);
                }
            }
        }

        private static void Player_JollyFoodUpdate(On.Player.orig_JollyFoodUpdate orig, Player self)
        {
            if (self.slugcatStats.name != VoidEnums.SlugcatID.Void && self.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                orig(self);
                return;
            }

            if (ModManager.CoopAvailable && self.playerState.playerNumber != 0 &&
                self.abstractCreature.world.game.IsStorySession && !self.isNPC)
            {
                if (self.abstractCreature?.world?.game?.Players == null ||
                    self.abstractCreature.world.game.Players.Count == 0)
                {
                    orig(self);
                    return;
                }


                if (self.abstractCreature?.world?.game?.Players[0]?.realizedCreature is Player mainPlayer)
                {
                    self.playerState.foodInStomach = Mathf.Clamp(mainPlayer.playerState.foodInStomach, 0, self.MaxFoodInStomach);
                    self.playerState.quarterFoodPoints = mainPlayer.playerState.quarterFoodPoints;
                }
                else
                {
                    orig(self);
                }
            }
            else
            {
                orig(self);
            }
        }
    }
}
