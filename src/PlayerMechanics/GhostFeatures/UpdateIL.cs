using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

public static class UpdateIL
{
    public static void Hook()
    {
        IL.Ghost.Update += Ghost_UpdateIL;
    }

    static void Ghost_UpdateIL(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
            x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, Ghost, bool>>((prev, self) => prev || self.room.game.StoryCharacter == VoidEnums.SlugcatID.Void);
        }
        else
        {
            LogExErr("Could not find Saint check. Echoes won't talk to void.");
        }
    }
}
