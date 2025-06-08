using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.OptionInterface;
using RWCustom;


namespace VoidTemplate.Objects.NoodleEgg;

public class EdibleNoodleEgg
{
    public int bites = 4;

    public NeedleEgg sourceEgg;

    public EdibleNoodleEgg(NeedleEgg egg)
    {
        sourceEgg = egg;
    }

    public void Bite(Creature.Grasp grasp, bool eu)
    {
        if (bites == 4)
        {
            sourceEgg.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, grasp.grabber.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
            for (int i = 0; i < 3; i++)
            {
                sourceEgg.room.AddObject(new WaterDrip(sourceEgg.firstChunk.pos, Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(4f, 21f, UnityEngine.Random.value), false));
            }
        }
        bites--;
        sourceEgg.room.PlaySound((bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, sourceEgg.firstChunk);
        sourceEgg.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
        if (bites < 1)
        {
            grasp.Release();
            (grasp.grabber as Player).AddFood(OptionAccessors.SimpleFood ? 4 : 2);
            sourceEgg.Destroy();
        }
    }
}

public static class EdibleNoodleEggCWT
{
    private static ConditionalWeakTable<NeedleEgg, EdibleNoodleEgg> edibleEgg = new();

    public static EdibleNoodleEgg GetEdible(this NeedleEgg egg)
    {
        return edibleEgg.GetValue(egg, _ => new(egg));
    }
}
