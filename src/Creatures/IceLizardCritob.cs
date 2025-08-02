using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fisobs;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using UnityEngine;
using Watcher;
using static MonoMod.InlineRT.MonoModRule;

namespace VoidTemplate.Creatures
{
    public class IceLizardCritob : Critob
    {
        public IceLizardCritob() : base(CreatureTemplateType.IceLizard)
        {
            Icon = new SimpleIcon("Kill_Standard_Lizard", new(0.7f, 0.7f, 0.7f));
            RegisterUnlock(KillScore.Configurable(25), SandboxUnlockID.IceLizard);
            IceLizardHooks.Hook();
        }

        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new LizardAI(acrit, acrit.world);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new IceLizard(acrit, acrit.world);
        }

        public override CreatureTemplate CreateTemplate()
        {
            CreatureTemplate creatureTemplate = LizardBreeds.BreedTemplate(CreatureTemplate.Type.RedLizard, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.LizardTemplate), StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard), null, null);
            creatureTemplate.type = Type;
            creatureTemplate.name = CreatureName;
            LizardBreedParams lizardBreedParams = creatureTemplate.breedParameters as LizardBreedParams;
            lizardBreedParams.template = Type;
            lizardBreedParams.standardColor = Color.white;
            if (lizardBreedParams.tongue == true)
            {
                lizardBreedParams.tongue = false;
                lizardBreedParams.tongueAttackRange = 0;
                lizardBreedParams.tongueWarmUp = 0;
                lizardBreedParams.tongueSegments = 0;
                lizardBreedParams.tongueChance = 0;
            }
            return creatureTemplate;
        }

        public override void EstablishRelationships()
        {
            Relationships relationships = new(Type);
            relationships.Attacks(Type, 1);
            relationships.Attacks(CreatureTemplate.Type.Vulture, 0.4f);
            relationships.Attacks(CreatureTemplate.Type.KingVulture, 0.2f);
            relationships.Attacks(CreatureTemplate.Type.MirosBird, 0.4f);
            relationships.Fears(CreatureTemplate.Type.DaddyLongLegs, 0.2f);
            relationships.FearedBy(CreatureTemplate.Type.BigSpider, 0.4f);
            relationships.FearedBy(CreatureTemplate.Type.DropBug, 0.4f);
            relationships.Eats(DLCSharedEnums.CreatureTemplateType.ZoopLizard, 0.3f);
            relationships.FearedBy(DLCSharedEnums.CreatureTemplateType.ZoopLizard, 1);
            relationships.EatenBy(WatcherEnums.CreatureTemplateType.BigMoth, 0.7f);
            relationships.Attacks(WatcherEnums.CreatureTemplateType.BigMoth, 0.5f);
            relationships.FearedBy(WatcherEnums.CreatureTemplateType.SmallMoth, 1f);
            relationships.Eats(WatcherEnums.CreatureTemplateType.SmallMoth, 0.5f);
        }

        public override CreatureState CreateState(AbstractCreature acrit)
        {
            return new LizardState(acrit);
        }

        public override CreatureTemplate.Type ArenaFallback()
        {
            if (Random.value >= 0.75f)
            {
                return CreatureTemplate.Type.GreenLizard;
            }
            return CreatureTemplate.Type.PinkLizard;
        }
    }
}
