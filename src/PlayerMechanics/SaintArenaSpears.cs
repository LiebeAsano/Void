using MonoMod.Cil;
using Mono.Cecil.Cil;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.OptionInterface.OptionAccessors;
using VoidTemplate.OptionInterface;
using System;

namespace VoidTemplate.PlayerMechanics;

internal static class SaintArenaSpears
{
    public static void Hook()
    {
        IL.Player.ThrowObject += Player_ThrowObject;
    }

    private static void Player_ThrowObject(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new(il);
        if (c.TryGotoNext(MoveType.After,
            q => q.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
            q => q.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, Player, bool>>((bool res, Player self) =>
            {
                if (OptionAccessors.SaintArenaSpears) return res && !self.abstractCreature.world.game.IsArenaSession;
                return res;

            });


        }
        else
        {
            logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(SaintArenaSpears)}.{nameof(Player_ThrowObject)}: first match failed");
        }
        if (c.TryGotoNext(MoveType.After,
            q => q.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
            q => q.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, Player, bool>>((bool res, Player self) =>
            {
                if (OptionAccessors.SaintArenaSpears) return res && !self.abstractCreature.world.game.IsArenaSession;
                return res;
            });
        }
        else
        {
            logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(SaintArenaSpears)}.{nameof(Player_ThrowObject)}: second match failed");
        }
    }
}
