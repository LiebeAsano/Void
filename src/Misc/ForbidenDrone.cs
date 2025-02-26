using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.Misc;

internal static class ForbidenDrone
{
    public static void Hook()
    {
        IL.Player.UpdateMSC += Player_ForbidenDrone;
    }

    private static void Player_ForbidenDrone(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After, i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdfld<Room>("game"),
                i => i.MatchLdfld<RainWorldGame>("wasAnArtificerDream")))
            {

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, Player, bool>>((re, self) =>
                    re && self.abstractCreature.world.game.session is StoryGameSession session &&
                           session.saveStateNumber == VoidEnums.SlugcatID.Void);
            }
            else
            {
                LogExErr("&Player.UpdateMSC error IL Hook");
            }

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
