using System.Collections.Generic;
using Fisobs.Creatures;
using Fisobs.Core;
using Fisobs.Sandbox;
using static PathCost.Legality;
using UnityEngine;
using DevInterface;
using RWCustom;
using MoreSlugcats;

namespace VoidTemplate.Creatures
{
    sealed class OutspectorBCritob : Critob
    {
        public OutspectorBCritob() : base(CreatureTemplateType.OutspectorB)
        {
            Icon = new SimpleIcon("Kill_Inspector", Color.blue);
            RegisterUnlock(KillScore.Configurable(25), SandboxUnlockID.OutspectorB);
            SandboxPerformanceCost = new(3f, 1.5f);
            LoadedPerformanceCost = 200f;
            ShelterDanger = ShelterDanger.Hostile;
            OutspectorHooks.Apply();
        }

        public override int ExpeditionScore() => 25;

        public override Color DevtoolsMapColor(AbstractCreature acrit) => Color.red;

        public override string DevtoolsMapName(AbstractCreature acrit) => "Outspc";

        public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction() => new[] { RoomAttractivenessPanel.Category.LikesInside };

        public override IEnumerable<string> WorldFileAliases() => new[] { "OutspectorB" };

        public override CreatureTemplate CreateTemplate()
        {
            CreatureTemplate t = new CreatureFormula(this)
            {
                TileResistances = new()
                {
                    Air = new(10, Allowed)
                },
                ConnectionResistances = new()
                {
                    Standard = new(.1f, Allowed),
                    ShortCut = new(.1f, Allowed),
                    BigCreatureShortCutSqueeze = new(10f, Allowed),
                    OffScreenMovement = new(.1f, Allowed),
                    BetweenRooms = new(10f, Allowed)
                },
                DefaultRelationship = new(CreatureTemplate.Relationship.Type.Eats, 1f),
                DamageResistances = new() { Base = 200f, Explosion = .03f },
                StunResistances = new() { Base = 200f },
                HasAI = true,
                Pathing = PreBakedPathing.Ancestral(DLCSharedEnums.CreatureTemplateType.Inspector),
            }.IntoTemplate();
            t.canFly = true;
            return t;
        }

        public override void EstablishRelationships()
        {

            Relationships self = new(CreatureTemplateType.OutspectorB);

            foreach (var template in StaticWorld.creatureTemplates)

                self.EatenBy(DLCSharedEnums.CreatureTemplateType.Inspector, 3f);
            self.FearedBy(CreatureTemplate.Type.LizardTemplate, 1f);
            self.FearedBy(CreatureTemplate.Type.Slugcat, 0.5f);
            self.FearedBy(CreatureTemplate.Type.Scavenger, 0.7f);
            self.FearedBy(CreatureTemplate.Type.LizardTemplate, 1f);
            self.AttackedBy(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, 0.5f);
            self.FearedBy(DLCSharedEnums.CreatureTemplateType.ScavengerElite, 0.7f);
            self.Eats(CreatureTemplateType.OutspectorB, 3f);
            self.EatenBy(CreatureTemplateType.OutspectorB, 3f);
        }



        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit) => new OutspectorBAI(acrit, acrit.world);


        public override Creature CreateRealizedCreature(AbstractCreature acrit) => new OutspectorB(acrit, acrit.world);

        public override CreatureState CreateState(AbstractCreature acrit) => new OutspectorB.OutspectorBState(acrit);

        public override void LoadResources(RainWorld rainWorld) { }

        public override CreatureTemplate.Type ArenaFallback() => DLCSharedEnums.CreatureTemplateType.Inspector;
    }
}
