
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DevInterface;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace VoidTemplate;



sealed class MimicstarfishCritob : Critob
    {
    internal MimicstarfishCritob() : base(CreatureTemplateType.Mimicstarfish)
    {
        base.Icon = new SimpleIcon("Mimicstarfish_Icon", new Color(1f, 0.8f, 0.8f));
        
        base.SandboxPerformanceCost = new SandboxPerformanceCost(1f, 0.65f);
        base.RegisterUnlock(KillScore.Configurable(19), SandboxUnlockID.Mimicstarfish, null, 0);
        base.ShelterDanger = ShelterDanger.Hostile;
    }

    public override int ExpeditionScore()
    {
        return 19;
    }


    public override Color DevtoolsMapColor(AbstractCreature acrit)
    {
        return new Color(0.75f, 0.15f, 0f);
    }

    public override string DevtoolsMapName(AbstractCreature acrit)
    {
        return "Msf";
    }

    public override IEnumerable<string> WorldFileAliases()
    {
        return new string[]
        {
                "Mimicstar"
        };
    }
    

    
        public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction()
        {
            return new RoomAttractivenessPanel.Category[]
            {
                RoomAttractivenessPanel.Category.LikesWater,
                RoomAttractivenessPanel.Category.Swimming,
                RoomAttractivenessPanel.Category.Dark
            };
        }

        public override CreatureTemplate CreateTemplate()
        {
        CreatureTemplate creatureTemplate = new CreatureFormula(CreatureTemplate.Type.BrotherLongLegs, base.Type, "Mimicstar")
        {
            TileResistances = new TileResist
            {
    
                Air = new PathCost(1f, 0),
                Corridor = new PathCost(1f, 0),
                Wall = new PathCost(100f, 0),
                Ceiling = new PathCost(100f, 0),
                Floor = new PathCost(100f, 0),
                Climb = new PathCost(100f, 0),
            },
            ConnectionResistances = new ConnectionResist
            {
                Standard = new PathCost(1f, 0),
                ShortCut = new PathCost(1f, 0),
                BigCreatureShortCutSqueeze = new PathCost(10f, 0),
                OffScreenMovement = new PathCost(1f, 0),
                BetweenRooms = new PathCost(10f, 0)
            },
            DefaultRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 1f),
            DamageResistances = new AttackResist
            {
                Base = 5f,
                Explosion = 0f,
                Electric = 0f,
            },
            StunResistances = new AttackResist
            {
                Base = 5f,
                Explosion = 0f,
                Electric = 0f,
            },

            HasAI = true,
            Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.BrotherLongLegs),
        }.IntoTemplate();
            creatureTemplate.requireAImap = true;
            creatureTemplate.canFly = false;
            creatureTemplate.offScreenSpeed = 0.1f;
            creatureTemplate.grasps = 1;
            creatureTemplate.stowFoodInDen = true;
            creatureTemplate.bodySize = 3.3f;
            creatureTemplate.visualRadius = 0f;
            creatureTemplate.movementBasedVision = 0f;
            creatureTemplate.dangerousToPlayer = 0.55f;
            creatureTemplate.communityID = CreatureCommunities.CommunityID.None;
            creatureTemplate.daddyCorruptionImmune = false;
            creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.Amphibious;
            creatureTemplate.meatPoints = 7;
            creatureTemplate.BlizzardWanderer = false;
            creatureTemplate.BlizzardAdapted = false;
            creatureTemplate.shortcutColor = Color.clear;
            creatureTemplate.shortcutSegments = 3;
            creatureTemplate.jumpAction = "Cannonball";
            creatureTemplate.pickupAction = "Consume";
            creatureTemplate.throwAction = "Release";
            return creatureTemplate;
        }

        // Token: 0x06000019 RID: 25 RVA: 0x00002744 File Offset: 0x00000944
        public override void EstablishRelationships()
        {
       
        var relationships = new Relationships(base.Type);

        relationships.Ignores(CreatureTemplate.Type.GreenLizard);
        relationships.Ignores(CreatureTemplate.Type.Vulture);
        relationships.Ignores(CreatureTemplate.Type.Fly);
        relationships.Ignores(CreatureTemplate.Type.Spider);
        relationships.Ignores(CreatureTemplate.Type.Leech);
        relationships.Ignores(CreatureTemplate.Type.SeaLeech);
        relationships.Eats(CreatureTemplate.Type.LizardTemplate, 1f);
        relationships.Eats(CreatureTemplate.Type.TubeWorm, 1f);
        relationships.FearedBy(CreatureTemplate.Type.LizardTemplate, 0.8f);
        relationships.FearedBy(CreatureTemplate.Type.Scavenger, 0.8f);
        relationships.FearedBy(CreatureTemplate.Type.CicadaA, 1f);
        relationships.FearedBy(CreatureTemplate.Type.CicadaB, 1f);
        relationships.FearedBy(CreatureTemplate.Type.DropBug, 1f);
        relationships.FearedBy(CreatureTemplate.Type.Snail, 0.8f);
        relationships.FearedBy(CreatureTemplate.Type.JetFish, 0.8f);
        relationships.FearedBy(CreatureTemplate.Type.EggBug, 0.2f);
        relationships.Fears(CreatureTemplate.Type.BigEel, 1f);
        relationships.Fears(CreatureTemplate.Type.KingVulture, 1f);
        relationships.Fears(CreatureTemplate.Type.MirosBird, 1f);
        relationships.Fears(CreatureTemplate.Type.PoleMimic, 0.6f);
        relationships.Fears(CreatureTemplate.Type.TentaclePlant, 0.8f);
        relationships.Fears(CreatureTemplate.Type.RedCentipede, 1f);
        /*if (ModManager.MSC)
        {
            relationships.Fears(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, 1f);
            relationships.Ignores(MoreSlugcatsEnums.CreatureTemplateType.TrainLizard);
            relationships.Fears(MoreSlugcatsEnums.CreatureTemplateType.AquaCenti,0.8f);
            relationships.Fears(MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs, 1f);
        }*/
        relationships.Ignores(base.Type);
        }

        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new mimicAI(acrit, acrit.world);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new Mimicstarfish(acrit, acrit.world);
        }

    public override void CorpseIsEdible(Player player, Creature crit, ref bool canEatMeat)
    {
        canEatMeat = true;
    }

    public override CreatureState CreateState(AbstractCreature acrit)
    {
        return new Mimicstarfish.StarState(acrit);
        {
            
        };
    }

    public override void LoadResources(RainWorld rainWorld)
        {
        }

 
        public override CreatureTemplate.Type ArenaFallback()
        {
            return CreatureTemplate.Type.BrotherLongLegs;
        }
    }
