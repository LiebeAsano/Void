using MonoMod.Cil;
using Mono.Cecil.Cil;
using static VoidTemplate.Useful.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

internal static class SaintArenaSpears
{
    public static void Hook()
    {
        if (VoidTemplate.PlayerMechanics.RemixOptions.Instance?.EnableSaintArenaSpears.Value ?? false)
        {
            IL.Player.ThrowObject += Player_ThrowObject;
        }
    }

    public static void UpdateHooks()
    {
        IL.Player.ThrowObject -= Player_ThrowObject;

        if (VoidTemplate.PlayerMechanics.RemixOptions.Instance?.EnableSaintArenaSpears.Value ?? false)
        {
            IL.Player.ThrowObject += Player_ThrowObject;
        }
    }

    private static void Player_ThrowObject(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new(il);
        if(c.TryGotoNext(MoveType.After, 
            q => q.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
            q => q.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool res, Player self) => res && !self.abstractCreature.world.game.IsArenaSession);
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
            c.EmitDelegate((bool res, Player self) => res && !self.abstractCreature.world.game.IsArenaSession);
        }
        else
        {
            logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(SaintArenaSpears)}.{nameof(Player_ThrowObject)}: second match failed");
        }
    }
}
