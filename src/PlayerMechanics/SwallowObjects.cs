using System;
using System.Collections.Generic;
using System.Linq;
using VoidTemplate.Oracles;
using VoidTemplate.Useful;
namespace VoidTemplate.PlayerMechanics;

internal static class SwallowObjects
{
	public static void Hook()
	{
		On.Player.SwallowObject += Player_SwallowObject;
	}

	private static readonly HashSet<Type> HalfFoodObjects =
		[
			typeof(Hazer),
			typeof(VultureGrub)
		];

	private static readonly HashSet<Type> QuarterFoodObjects =
	[
		typeof(WaterNut),
		typeof(FirecrackerPlant),
		typeof(FlyLure),
		typeof(FlareBomb),
		typeof(PuffBall),
		typeof(FlyLure),
		typeof(BubbleGrass)
	];

	private static readonly HashSet<Type> FullPinFoodObjects =
	[
		typeof(SporePlant),
		];

	private static readonly HashSet<Type> TwoFullPinFoodObjects =
	[
		typeof(NeedleEgg),
		];

	private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
	{
		AbstractPhysicalObject abstractGrabbed = self.grasps[grasp]?.grabbed?.abstractPhysicalObject;

		if (self.IsVoid())
		{
			var grabbed = self.grasps[grasp]?.grabbed;

			var game = self.abstractCreature.world.game;

			bool hasMark = game.IsStorySession && (game.GetStorySession.saveState.deathPersistentSaveData.theMark);

			if (grabbed != null)
			{
				if (QuarterFoodObjects.Contains(grabbed.GetType()))
				{

					orig(self, grasp);

					if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
					{
						if (self.Karma != 10)
						{
							self.objectInStomach.Destroy();
							self.objectInStomach = null;
							self.AddQuarterFood();
						}
						else if (self.slugcatStats.foodToHibernate > self.FoodInStomach)
						{
							self.objectInStomach.Destroy();
							self.objectInStomach = null;
							self.AddQuarterFood();
						}
					}

					return;
				}
				else if (FullPinFoodObjects.Contains(grabbed.GetType()))
				{

					orig(self, grasp);

					if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
					{
						if (self.Karma != 10)
						{
							self.objectInStomach.Destroy();
							self.objectInStomach = null;
							if (OptionInterface.OptionAccessors.SimpleFood)
								self.AddFood(2);
							else
								self.AddFood(1);
						}
						else if (self.slugcatStats.foodToHibernate > self.FoodInStomach)
						{
							self.objectInStomach.Destroy();
							self.objectInStomach = null;
							if (OptionInterface.OptionAccessors.SimpleFood)
								self.AddFood(2);
							else
								self.AddFood(1);
						}
					}

					return;
				}
				else if (TwoFullPinFoodObjects.Contains(grabbed.GetType()))
				{

					orig(self, grasp);

					if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
					{
						self.objectInStomach.Destroy();
						self.objectInStomach = null;
						if (OptionInterface.OptionAccessors.SimpleFood)
							self.AddFood(4);
						else
							self.AddFood(2);
					}

					return;
				}
				else if (HalfFoodObjects.Contains(grabbed.GetType()))
				{

					orig(self, grasp);

					if (self.objectInStomach != null && self.objectInStomach == abstractGrabbed)
					{
						if (self.Karma != 10)
						{
							self.objectInStomach.Destroy();
							self.objectInStomach = null;
							if (OptionInterface.OptionAccessors.SimpleFood)
								self.AddFood(1);
							else
							{
								self.AddQuarterFood();
								self.AddQuarterFood();
							}
						}
						else if (self.slugcatStats.foodToHibernate > self.FoodInStomach)
						{
							self.objectInStomach.Destroy();
							self.objectInStomach = null;
							if (OptionInterface.OptionAccessors.SimpleFood)
								self.AddFood(1);
							else
							{
								self.AddQuarterFood();
								self.AddQuarterFood();
							}
						}
					}

					return;
				}
			}
		}

		if (self.IsVoid() && self.Karma != 10)
		{
			if (self.room != null && self.grasps[grasp].grabbed is PebblesPearl &&
				self.room.updateList.Any(i => i is Oracle oracle && oracle.oracleBehavior is SSOracleBehavior))
			{
				((self.room.updateList.First(i => i is Oracle) as Oracle)
					.oracleBehavior as SSOracleBehavior).EatPearlsInterrupt();
			}
		}

		orig(self, grasp);

		if (self.IsVoid() && self.objectInStomach != null && self.Karma != 10)
		{
			self.objectInStomach.Destroy();
			self.objectInStomach = null;
		}

		orig(self, grasp);
	}
}
