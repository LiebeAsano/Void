using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fisobs;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using UnityEngine;
using VoidTemplate.CreatureInteractions;
using Watcher;
using static MonoMod.InlineRT.MonoModRule;

namespace VoidTemplate.Creatures
{
    public class LWIceLizardCritob : Critob
    {
        public LWIceLizardCritob(CreatureTemplate.Type type) : base(type)
        {
            Icon = new IceLizardIcon();
            RegisterUnlock(KillScore.Configurable(25), SandboxUnlockID.IceLizard);
            LWIceLizardHooks.Hook();
        }

        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new LizardAI(acrit, acrit.world);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new LWIceLizard(acrit, acrit.world);
        }

        public override CreatureTemplate CreateTemplate()
        {
            CreatureTemplate creatureTemplate = LizardBreeds.BreedTemplate(CreatureTemplate.Type.RedLizard, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.LizardTemplate), StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard), null, null);
            creatureTemplate.type = Type;
            creatureTemplate.name = CreatureName;
            LizardBreedParams lizardBreedParams = creatureTemplate.breedParameters as LizardBreedParams;
            lizardBreedParams.template = Type;
            lizardBreedParams.standardColor = Type == CreatureTemplateType.LWRainLizard ? Color.blue : Color.white;
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
            var state = new LizardState(acrit);
            if (Type == CreatureTemplateType.LWRainLizard)
            {
                System.Array.Resize(ref state.limbHealth, state.limbHealth.Length + 2);
            }
            return state;
        }

        public override CreatureTemplate.Type ArenaFallback()
        {
            if (Random.value >= 0.75f)
            {
                return CreatureTemplate.Type.GreenLizard;
            }
            return CreatureTemplate.Type.PinkLizard;
        }

        /*public override void Init(AbstractCreature acrit, World world, WorldCoordinate pos, EntityID id)
        {
            if (Type == CreatureTemplateType.LWRainLizard) acrit.GetPostCycleFlag().Value = true;
        }*/

        public class IceLizardIcon : Icon
        {

            public override int Data(AbstractPhysicalObject apo)
            {
                if ((apo as AbstractCreature).creatureTemplate.type == CreatureTemplateType.LWRainLizard)
                {
                    return 1;
                }
                return 0;
            }

            public override Color SpriteColor(int data)
            {
                if (data == 1)
                {
                    return Color.blue;
                }
                return new(0.7f, 0.7f, 0.7f);
            }

            public override string SpriteName(int data)
            {
                return "Kill_Standard_Lizard";
            }
        }
    }
}
