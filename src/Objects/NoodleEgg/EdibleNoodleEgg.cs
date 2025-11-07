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
        bool wasFirstBite = bites == 4;
        Player bitingPlayer = grasp?.grabber as Player;

        if (bites == 4)
        {
            sourceEgg.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, grasp.grabber.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
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
            if (bitingPlayer != null)
            {
                if (bitingPlayer.AreVoidViy())
                {
                    bitingPlayer.AddFood(OptionAccessors.SimpleFood ? 4 : 2);
                }
                else
                {
                    bitingPlayer.AddFood(4);
                }
            }
            sourceEgg.Destroy();
            sourceEgg.RemoveEdible();
        }

        if (wasFirstBite && bitingPlayer != null && sourceEgg.room != null)
        {
            TriggerNeedleWormAggression(bitingPlayer, sourceEgg);
        }
    }

    private static void TriggerNeedleWormAggression(Player player, NeedleEgg egg)
    {
        if (player.dead || egg.room == null) return;

        int angryWorms = 0;

        foreach (var abstractCreature in egg.room.abstractRoom.creatures)
        {
            if (abstractCreature.realizedCreature is BigNeedleWorm worm &&
                worm.room == egg.room &&
                worm.Consious &&
                !worm.dead)
            {
                float distance = Vector2.Distance(worm.mainBodyChunk.pos, egg.firstChunk.pos);
                bool canSee = egg.room.VisualContact(worm.mainBodyChunk.pos, egg.firstChunk.pos);

                if (canSee || distance < 300f)
                {
                    MakeWormAggressive(worm, player);
                    angryWorms++;
                }
            }
        }
    }

    private static void MakeWormAggressive(BigNeedleWorm worm, Player player)
    {
        if (worm.AI is not BigNeedleWormAI ai) return;

        var relationship = ai.creature.state.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID);
        relationship.InfluenceTempLike(-0.8f);
        relationship.InfluenceLike(-0.3f);

        if (ai.tracker.RepresentationForCreature(player.abstractCreature, false) == null)
        {
            ai.tracker.SeeCreature(player.abstractCreature);
        }

        ai.creature.abstractAI.followCreature = player.abstractCreature;

        ai.attackCounter = Mathf.Max(ai.attackCounter, 80);

        ai.behavior = NeedleWormAI.Behavior.Attack;

        if (worm.room != null && worm.attackReady <= 0.8f && worm.swishDir == null)
        {
            worm.BigCry();
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