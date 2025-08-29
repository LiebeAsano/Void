
using System.Collections.Generic;
using DevInterface;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using UnityEngine;

namespace VoidTemplate;

sealed class DartspiderPCritob : Critob
{
    public DartspiderPCritob() : base(CreatureTemplateType.DartspiderP)
    {
        base.Icon = new SimpleIcon("Kill_BigSpider", new Color(0.5f, 0.8f, 0f));

        base.SandboxPerformanceCost = new SandboxPerformanceCost(0.55f, 0.65f);
        base.RegisterUnlock(KillScore.Configurable(7), SandboxUnlockID.DartspiderP, null, 0);
        base.ShelterDanger = ShelterDanger.Hostile;
        base.LoadedPerformanceCost = 50f;
        DartPHooks.Apply();
    }

    public override int ExpeditionScore()
    {
        return 7;
    }


    public override Color DevtoolsMapColor(AbstractCreature acrit)
    {
        return new Color(0f, 0.2f, 0f);
    }

    public override string DevtoolsMapName(AbstractCreature acrit)
    {
        return "DSP";
    }

    public override IEnumerable<string> WorldFileAliases()
    {
        return new string[]
        {
           "DartspiderP",
           "Dart Spider Poison"
        };
    }



    public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction()
    {
        return new RoomAttractivenessPanel.Category[]
        {
                    RoomAttractivenessPanel.Category.Dark,
                    RoomAttractivenessPanel.Category.LikesOutside
        };
    }
    public override void GraspParalyzesPlayer(Creature.Grasp grasp, ref bool paralyzing)
    {
        paralyzing = true;
    }

    public override CreatureTemplate CreateTemplate()
    {


        CreatureTemplate creatureTemplate = new CreatureFormula(CreatureTemplate.Type.SpitterSpider, base.Type, "DartspiderP")
        {
            TileResistances = new TileResist
            {

                OffScreen = new PathCost(1f, 0),
                Corridor = new PathCost(1f, 0),
                Wall = new PathCost(3f, 0),
                Ceiling = new PathCost(3f, 0),
                Floor = new PathCost(1f, 0),
                Climb = new PathCost(1f, 0),
            },
            ConnectionResistances = new ConnectionResist
            {
                Standard = new PathCost(1f, 0),
                OpenDiagonal = new PathCost(3f, 0),
                ReachOverGap = new PathCost(3f, 0),
                ReachUp = new PathCost(2f, 0),
                ReachDown = new PathCost(2f, 0),
                SemiDiagonalReach = new PathCost(2f, 0),
                DropToFloor = new PathCost(10f, 0),
                DropToWater = new PathCost(10f, 0),
                DropToClimb = new PathCost(10f, 0),
                ShortCut = new PathCost(1.5f, 0),
                NPCTransportation = new PathCost(3f, 0),
                OffScreenMovement = new PathCost(1f, 0),
                BetweenRooms = new PathCost(5f, 0),
                Slope = new PathCost(1.5f, 0),
                CeilingSlope = new PathCost(1.5f, 0),
            },
            DefaultRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f),
            DamageResistances = new AttackResist
            {
                Base = 1.3f,
            },
            StunResistances = new AttackResist
            {
                Base = 0.8f,
            },

            HasAI = true,
            Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.SpitterSpider),

        }.IntoTemplate();
        creatureTemplate.bodySize = 1.4f;
        creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.AirAndSurface;
        creatureTemplate.meatPoints = 6;
        creatureTemplate.BlizzardWanderer = false;
        creatureTemplate.BlizzardAdapted = false;
        creatureTemplate.dangerousToPlayer = 0.7f;
        creatureTemplate.visualRadius = 1100f;
        creatureTemplate.jumpAction = "Spit";
        creatureTemplate.pickupAction = "Grab";
        creatureTemplate.throwAction = "Release";

        return creatureTemplate;
    }

    public override void EstablishRelationships()
    {

        var relationships = new Relationships(base.Type);

        relationships.Ignores(base.Type);
    }

    public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
    {
        return new BigSpiderAI(acrit, acrit.world);
    }

    public override Creature CreateRealizedCreature(AbstractCreature acrit)
    {
        return new Dartspider(acrit, acrit.world);
    }
    public override CreatureState CreateState(AbstractCreature acrit)
    {
        return new HealthState(acrit);
    }

    public override void LoadResources(RainWorld rainWorld)
    {
    }


    public override CreatureTemplate.Type ArenaFallback()
    {
        return CreatureTemplate.Type.SpitterSpider;
    }
}
