using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics;

public static class ProtoViyAI
{
    class ProtoViyState
    {
        public float interest;
        public float anger;
    }

    static readonly ConditionalWeakTable<SlugNPCAI, ProtoViyState> States = new();

    static ProtoViyState GetState(SlugNPCAI ai)
        => States.GetValue(ai, _ => new ProtoViyState());

    public static void Hook()
    {
        On.MoreSlugcats.SlugNPCAI.ctor += SlugNPCAI_ctor;
        On.MoreSlugcats.SlugNPCAI.Update += SlugNPCAI_Update;
    }

    private static void SlugNPCAI_ctor(
        On.MoreSlugcats.SlugNPCAI.orig_ctor orig,
        SlugNPCAI self,
        AbstractCreature creature,
        World world)
    {
        orig(self, creature, world);

        if (creature.realizedCreature is not Player cat ||
            !cat.isNPC ||
            !cat.IsProtoViy())
            return;

        var pers = self.creature.personality;
        pers.aggression = 0.65f;
        pers.nervous = 0.85f;
        pers.bravery = 0.35f;
        pers.dominance = 0.4f;
        self.creature.personality = pers;

        self.followCloseness = 5f;
    }

    private static void SlugNPCAI_Update(
        On.MoreSlugcats.SlugNPCAI.orig_Update orig,
        SlugNPCAI self)
    {
        orig(self);

        if (self.creature.realizedCreature is not Player cat ||
            !cat.isNPC ||
            !cat.IsProtoViy())
            return;

        var state = GetState(self);

        Player player = cat.room?.game?.FirstAlivePlayer?.realizedCreature as Player;
        if (player == null || player.room != cat.room)
            return;

        float dist = Vector2.Distance(cat.mainBodyChunk.pos, player.mainBodyChunk.pos);

        if (dist > 200f && dist < 450f)
            state.interest = Mathf.Min(1f, state.interest + 0.01f);
        else
            state.interest = Mathf.Max(0f, state.interest - 0.01f);

        if (dist < 120f)
            state.anger = Mathf.Min(1f, state.anger + 0.03f);
        else
            state.anger = Mathf.Max(0f, state.anger - 0.005f);

        for (int i = 0; i < self.relationshipTracker.relationships.Count; i++)
        {
            var rel = self.relationshipTracker.relationships[i];
            var rep = rel.trackerRep;

            if (rep?.representedCreature?.realizedCreature == player &&
                rel.state is SlugNPCAI.SlugNPCTrackState slugState)
            {
                int maxThreat = 9000;
                float effectiveAnger = Mathf.Max(0f, state.anger - 0.25f);
                slugState.annoyingThreat =
                    Mathf.RoundToInt(Mathf.Lerp(0f, maxThreat, effectiveAnger));
                break;
            }
        }

        if (state.anger > 0.7f)
        {
            self.behaviorType = SlugNPCAI.BehaviorType.Attacking;
        }
        else if (state.interest > 0.35f)
        {
            self.behaviorType = SlugNPCAI.BehaviorType.Following;
            self.followCloseness = Mathf.Lerp(3f, 7f, state.interest);
        }
        else
        {
            self.behaviorType = SlugNPCAI.BehaviorType.Idle;
        }
    }
}
