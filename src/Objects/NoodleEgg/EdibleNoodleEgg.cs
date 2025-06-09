using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.OptionInterface;
using RWCustom;
using static VoidTemplate.Useful.Utils;
using MoreSlugcats;
using VoidTemplate.CreatureInteractions;


namespace VoidTemplate.Objects.NoodleEgg;

public class EdibleNoodleEgg
{
    public int bites = 4;
    public bool shellCrack = false;
    public NeedleEgg sourceEgg;

    public EdibleNoodleEgg(NeedleEgg egg)
    {
        sourceEgg = egg;
    }


    public bool CanEat(Player grabber)
    {
        return (grabber != null && grabber.AreVoidViy()) || shellCrack;
    }


    public void Bite(Creature.Grasp grasp, bool eu)
    {
        if (bites == 4)
        {
            sourceEgg.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, grasp.grabber.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
            shellCrack = true;
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
            if (grasp.grabber is Player player)
            {
                if (player.AreVoidViy())
                {
                    player.AddFood(OptionAccessors.SimpleFood ? 4 : 2);
                }
                else 
                {
                    player.AddFood(4);
                }
            }
            sourceEgg.Destroy();
            sourceEgg.RemoveEdible();
        }
    }
}

public static class EdibleNoodleEggCWT
{
    private static readonly ConditionalWeakTable<NeedleEgg, EdibleNoodleEgg> edibleEgg = new();

    public static EdibleNoodleEgg GetEdible(this NeedleEgg egg)
    {
        return edibleEgg.GetValue(egg, e =>
        {
            var edible = new EdibleNoodleEgg(e);
            return edible;
        });
    }

    public static void RemoveEdible(this NeedleEgg egg)
    {
        if (edibleEgg.TryGetValue(egg, out var edible))
        {
            edibleEgg.Remove(egg);
        }
    }
}