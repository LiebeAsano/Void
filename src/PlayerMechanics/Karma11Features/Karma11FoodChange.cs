using MonoMod.Cil;
using static VoidTemplate.Useful.Utils;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class Karma11FoodChange
{
	public static void Hook()
	{
		On.StoryGameSession.ctor += StoryGameSession_ctor;
		IL.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
		IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
	}

	private static void SlugcatPageContinue_ctor(ILContext il)
	{
		ILCursor c = new(il);
		//this.hud.AddPart(new FoodMeter(this.hud, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x, SlugcatStats.SlugcatFoodMeter(slugcatNumber).y < 6 if void and at karma 11>, null, 0));
		if (c.TryGotoNext(MoveType.After, x => x.MatchNewobj<HUD.FoodMeter>())
			&& c.TryGotoPrev(MoveType.After,
			x => x.MatchLdfld<RWCustom.IntVector2>("y")))
		{
			LogExInf("creation found!");
			c.Emit(OpCodes.Ldarg, 4);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, SlugcatStats.Name, Menu.SlugcatSelectMenu.SlugcatPageContinue, int>>((int origRess, SlugcatStats.Name name, Menu.SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue) =>
			{
				if (name == VoidEnums.SlugcatID.TheVoid && slugcatPageContinue.saveGameData.karmaCap == 10) return 6;
				return origRess;
			});
		}
		else LogExErr("couldn't find creation of food meter instruction. expect main menu food meter to always be at 10 required pips for survival");
	}

	private static void ShelterDoor_DoorClosed(MonoMod.Cil.ILContext il)
	{
		ILCursor c = new(il);
		//used to bypass Jolly's static requirements for food consumption for character
		//int y = SlugcatStats.SlugcatFoodMeter(this.room.game.StoryCharacter).y < if void world and karma 10 make it 6>;
		if (c.TryGotoNext(MoveType.After, x => x.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
			x => x.MatchLdfld(out _)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, ShelterDoor, int>>((int orig, ShelterDoor self) =>
			{
				if (self.room.world.game.StoryCharacter == VoidEnums.SlugcatID.TheVoid && (self.room.game.Players[0].realizedCreature as Player).KarmaCap == 10)
				{
					return 6;
				}
				return orig;
			});
		}
		else LogExErr("failed to locate slugcatfoodmeter call in shelterdoor closing. expect mismatch between food requirements and success of hybernation");
	}

	private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
	{
		orig(self, saveStateNumber, game);
		if (self.saveState.saveStateNumber == VoidEnums.SlugcatID.TheVoid && self.saveState.deathPersistentSaveData.karma == 10)
		{
			self.characterStats.foodToHibernate = 6;
			self.characterStats.maxFood = 9;
		}
	}
}
