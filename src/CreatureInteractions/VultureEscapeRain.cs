using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidTemplate.RainCycleChanges;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class VultureEscapeRain
{
    public static void Hook()
    {
        IL.VultureAI.Update += VultureAI_Update;
    }

    private static void VultureAI_Update(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<VultureAI.Behavior>(nameof(VultureAI.Behavior.Disencouraged)),
            x => x.MatchStfld<VultureAI>(nameof(VultureAI.behavior))))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(EscapeRainIfRaining);
        }
        else Utils.LogExErr("VultureEscapeRain: behavior selection not found");
    }

    private static void EscapeRainIfRaining(VultureAI self)
    {
        if (!PostRainCycle.HasRainCycle(self.creature.world.game))
            return;

        if (self.behavior != VultureAI.Behavior.Idle && self.behavior != VultureAI.Behavior.Hunt)
            return;

        float rain = self.utilityComparer.GetUtilityTracker(self.rainTracker).SmoothedUtility();

        if (rain > 0.01f && rain >= self.utilityComparer.HighestUtility())
            self.behavior = VultureAI.Behavior.EscapeRain;
    }
}
