using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using VoidTemplate.MenuTinkery;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class FoodChange
{
	public static void Hook()
	{
		On.Player.Update += Player_Update;
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
			c.Emit(OpCodes.Ldarg, 4);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, SlugcatStats.Name, Menu.SlugcatSelectMenu.SlugcatPageContinue, int>>((int origRess, SlugcatStats.Name name, Menu.SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue) =>
			{
				bool hasMark = slugcatPageContinue.HasMark;
                if (name == VoidEnums.SlugcatID.Void && (slugcatPageContinue.saveGameData.karmaCap == 10 || hasMark)) return 6;
				return origRess;
			});
		}
		else LogExErr("couldn't find creation of food meter instruction. expect main menu food meter to always be at 10 required pips for survival");
	}

	//fix for whether slugcat will be malnourished in next cycle
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
                var game = self.room.game;
                bool hasMark = game.IsStorySession && (game.GetStorySession.saveState.deathPersistentSaveData.theMark);
                if (self.room.world.game.StoryCharacter == VoidEnums.SlugcatID.Void && ((self.room.game.Players[0].realizedCreature as Player).KarmaCap == 10 || hasMark))
				{
					return 6;
				}
				return orig;
			});
		}
		else LogExErr("failed to locate slugcatfoodmeter call in shelterdoor closing. expect mismatch between food requirements and success of hybernation");
	}
	/// <summary>
	/// replaces main menu food requirements
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	/// <param name="saveStateNumber"></param>
	/// <param name="game"></param>
	private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
	{
		orig(self, eu);
        var game = self.abstractCreature.world.game;
        bool hasMark = game.IsStorySession && (game.GetStorySession.saveState.deathPersistentSaveData.theMark);
        if (self.IsVoid() && (self.KarmaCap == 10 || hasMark))
		{
			self.slugcatStats.foodToHibernate = self.Malnourished ? 9 : 6;
			self.slugcatStats.maxFood = 9;
        }
    }
}
