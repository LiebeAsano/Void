using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.Objects.NoodleEgg;
internal static class ShellGrabUpdate
{
    public static void Hook()
    {
        IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    private static void Player_GrabUpdate(ILContext il)
    {
    }
}