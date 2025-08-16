using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Sandbox;

namespace VoidTemplate.Objects.SingularityRock
{
    public class MiniEnergyCellFisob : Fisob
    {
        public MiniEnergyCellFisob() : base(CreatureTemplateType.MiniEnergyCell)
        {
            Icon = new SimpleIcon("Symbol_Singularity", new(0.01961f, 0.6451f, 0.85f));
            RegisterUnlock(SandboxUnlockID.MiniEnergyCell);
            MiniEnergyCellHooks.Hook();
        }

        public override AbstractPhysicalObject Parse(World world, EntitySaveData entitySaveData, SandboxUnlock unlock)
        {
            return new MiniEnergyCellAbstract(world, entitySaveData.Pos, entitySaveData.ID);
        }
    }
}
