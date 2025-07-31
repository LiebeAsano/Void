using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Creatures
{
    public class IceLizard : Lizard
    {
        public IceLizard(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
        }

        public override void InitiateGraphicsModule()
        {
            graphicsModule ??= new IceLizardGraphics(this);
        }
    }
}
