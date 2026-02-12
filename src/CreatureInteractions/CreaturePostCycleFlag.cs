using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.RainCycleChanges;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions
{
    public static class CreaturePostCycleFlag
    {
        private static ConditionalWeakTable<AbstractCreature, StrongBox<bool>> PostCycleFlag = new();

        public static StrongBox<bool> GetPostCycleFlag(this AbstractCreature crit) => PostCycleFlag.GetOrCreateValue(crit);

        public static void Hook()
        {
            On.AbstractCreature.setCustomFlags += AbstractCreature_setCustomFlags;
            On.AbstractCreature.WantToStayInDenUntilEndOfCycle += AbstractCreature_WantToStayInDenUntilEndOfCycle;
            On.RainTracker.Utility += RainTracker_Utility;
            On.RainTracker.Update += RainTracker_Update;
            On.AbstractCreature.InDenUpdate += AbstractCreature_InDenUpdate;
            IL.AbstractCreature.IsEnteringDen += AbstractCreature_IsEnteringDen;
        }

        private static void AbstractCreature_IsEnteringDen(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<AbstractWorldEntity>("Abstractize")))
            {
                ILLabel label = c.MarkLabel();
                if (c.TryGotoPrev(MoveType.After,
                    x => x.MatchLdarg(0)))
                {
                    c.EmitDelegate((AbstractCreature self) =>
                    {
                        return self.GetPostCycleFlag().Value;
                    });
                    c.Emit(OpCodes.Brtrue, label);
                    c.Emit(OpCodes.Ldarg_0);
                }
                else
                    Utils.LogExErr("IL hook second match error!");
            }
            else
                Utils.LogExErr("IL hook first match error!");
        }

        private static void AbstractCreature_InDenUpdate(On.AbstractCreature.orig_InDenUpdate orig, AbstractCreature self, int time)
        {
            if (!self.WantToStayInDenUntilEndOfCycle() && self.remainInDenCounter == -1)
            {
                self.remainInDenCounter = 0;
            }
            orig(self, time);
            if (!self.GetPostCycleFlag().Value && self.world.rainCycle.GetRainCycleExt().PostCycleStarted && self.realizedCreature != null)
            {
                self.Abstractize(self.pos);
            }
            if (self.GetPostCycleFlag().Value && !self.world.rainCycle.GetRainCycleExt().PostCycleStarted && self.realizedCreature != null)
            {
                self.Abstractize(self.pos);
            }
        }

        private static void RainTracker_Update(On.RainTracker.orig_Update orig, RainTracker self)
        {
            orig(self);
            if (self.rainCycle != self.AI.creature.world.rainCycle)
            {
                self.rainCycle = self.AI.creature.world.rainCycle;
            }
        }

        private static float RainTracker_Utility(On.RainTracker.orig_Utility orig, RainTracker self)
        {
            if (self.AI.creature.GetPostCycleFlag().Value)
            {
                if (self.rainCycle.GetRainCycleExt().PostCycleStarted)
                {
                    return Mathf.InverseLerp(1f, 0f, Custom.SCurve(Mathf.InverseLerp(800f, 4000f, self.rainCycle.GetRainCycleExt().TimeToStartNewCycle), 0.1f));
                }
                else
                {
                    return 1;
                }
            }
            return orig(self);
        }

        private static bool AbstractCreature_WantToStayInDenUntilEndOfCycle(On.AbstractCreature.orig_WantToStayInDenUntilEndOfCycle orig, AbstractCreature self)
        {
            if (self.GetPostCycleFlag().Value)
            {
                if (!self.world.rainCycle.GetRainCycleExt().PostCycleStarted)
                {
                    return true;
                }
                else if (self.abstractAI.RealAI != null)
                {
                    return self.abstractAI.RealAI.WantToStayInDenUntilEndOfCycle();
                }
            }
            return orig(self);
        }

        private static void AbstractCreature_setCustomFlags(On.AbstractCreature.orig_setCustomFlags orig, AbstractCreature self)
        {
            orig(self);

            if (self.unrecognizedFlags.Contains("PostCycle") && !self.Room.shelter)
            {
                self.GetPostCycleFlag().Value = true;
                self.ignoreCycle = true;
            }
        }
    }
}
