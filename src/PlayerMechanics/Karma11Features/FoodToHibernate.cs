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

internal static class FoodToHibernate
{
    public static void Hook()
    {
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        IL.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
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
