using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fisobs;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using UnityEngine;

namespace VoidTemplate.Creatures
{
    public class IceLizardCritob : Critob
    {
        public IceLizardCritob() : base(CreatureTemplateType.IceLizard)
        {
            Icon = new SimpleIcon("Kill_Standard_Lizard", Color.magenta);
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
            CreatureTemplate t = new(StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedLizard))
            {
                type = Type,
                name = CreatureName,
            };
            (t.breedParameters as LizardBreedParams).standardColor = Color.white;
            //(t.breedParameters as LizardBreedParams).tongue = true;
            return t;
        }

        public override void EstablishRelationships()
        {

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
