using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

internal static class UpdateIL
{
    public static void Hook()
    {
        IL.Ghost.Update += Ghost_UpdateIL;
    }

    private static void Ghost_UpdateIL(ILContext il)
    {
        try
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
                x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))))
            {
                ILLabel skipLabel = il.DefineLabel();

                c.Index -= 2;

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Ghost, bool>>(self =>
                {
                    if (self.room.game.session is StoryGameSession session)
                    {
                        return session.saveStateNumber == VoidEnums.SlugcatID.Void;
                    }
                    return false;
                });

                c.Emit(OpCodes.Brtrue, skipLabel);

                c.Index += 2;

                c.MarkLabel(skipLabel);
            }
            else
            {
                LogExErr("IL.Ghost.Update += Ghost_UpdateIL error: Could not find Saint check");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            LogExErr($"IL.Ghost.Update += Ghost_UpdateIL error: {e}");
        }
    }
}
