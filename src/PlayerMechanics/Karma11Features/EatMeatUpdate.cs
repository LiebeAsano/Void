using RWCustom;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class EatMeatUpdate
{
	public static void Hook()
	{
		On.Player.EatMeatUpdate += Player_EatMeatUpdate;
	}

	private static void Player_EatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
	{

		if (self.IsVoid())
		{
			if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is Creature creature))
			{
				return;
			}

			if (self.eatMeat > 20)
			{
				if (ModManager.MSC)
				{
					if (creature.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
					{
						creature.bodyChunks[0].mass = 0.5f;
						creature.bodyChunks[1].mass = 0.3f;
						creature.bodyChunks[2].mass = 0.05f;
					}

					if (SlugcatStats.SlugcatCanMaul(self.SlugCatClass) && creature is Vulture vulture && self.grasps[graspIndex].grabbedChunk.index == 4 && vulture.abstractCreature.state is Vulture.VultureState vultureState && vultureState.mask)
					{
						vulture.DropMask(Custom.RNV());
						self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
						self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
						for (int i = UnityEngine.Random.Range(8, 14); i >= 0; i--)
						{
							self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(creature.firstChunk.pos, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
						}
					}
				}

				self.standing = false;
				self.Blink(5);
				if (self.eatMeat % 5 == 0)
				{
					Vector2 b = Custom.RNV() * 3f;
					self.mainBodyChunk.pos += b;
					self.mainBodyChunk.vel += b;
				}

				Vector2 vector = self.grasps[graspIndex].grabbedChunk.pos * self.grasps[graspIndex].grabbedChunk.mass;
				float num = self.grasps[graspIndex].grabbedChunk.mass;
				for (int j = 0; j < self.grasps[graspIndex].grabbed.bodyChunkConnections.Length; j++)
				{
					if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1 == self.grasps[graspIndex].grabbedChunk)
					{
						vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
						num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
					}
					else if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2 == self.grasps[graspIndex].grabbedChunk)
					{
						vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
						num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
					}
				}
				vector /= num;
				self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.5f;
				self.bodyChunks[1].vel -= Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.6f;

				if (self.graphicsModule != null && (self.grasps[graspIndex].grabbed as Creature).State.meatLeft > 0 && self.FoodInStomach < self.MaxFoodInStomach)
				{
					if (!Custom.DistLess(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos, self.grasps[graspIndex].grabbedChunk.rad))
					{
						(self.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos) * (self.grasps[graspIndex].grabbedChunk.rad - Vector2.Distance(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos));
					}
					else if (self.eatMeat % 5 == 3)
					{
						(self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
					}

					if (self.eatMeat > 40 && self.eatMeat % 15 == 3)
					{
						self.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * 4f;
						self.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector, self.mainBodyChunk.pos) * 0.9f / self.grasps[graspIndex].grabbedChunk.mass;
						for (int k = UnityEngine.Random.Range(0, 3); k >= 0; k--)
						{
							self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(vector, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
						}

						if (self.SessionRecord != null)
						{
							self.SessionRecord.AddEat(self.grasps[graspIndex].grabbed);
						}

						(self.grasps[graspIndex].grabbed as Creature).State.meatLeft--;

						var game = self.abstractCreature.world.game;

						bool hasMark = game.IsStorySession && (game.GetStorySession.saveState.deathPersistentSaveData.theMark);

						if (OptionInterface.OptionAccessors.SimpleFood)
						{
							self.AddFood(1);
						}
						else
						{
							self.AddQuarterFood();
							self.AddQuarterFood();
						}

						self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
						return;
					}

					if (self.eatMeat % 15 == 3)
					{
						self.room.PlaySound(SoundID.Slugcat_Eat_Meat_A, self.mainBodyChunk);
					}
				}
			}
		}

		orig(self, graspIndex);
	}
}
