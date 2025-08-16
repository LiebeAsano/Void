using Fisobs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;

namespace VoidTemplate.Objects.SingularityRock
{
    public class MiniEnergyCellAbstract : AbstractPhysicalObject
    {
        public bool charged = true;

        public MiniEnergyCell cell
        {
            get
            {
                return realizedObject as MiniEnergyCell;
            }
        }

        public MiniEnergyCellAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, CreatureTemplateType.MiniEnergyCell, null, pos, ID)
        {
        }

        public override void Realize()
        {
            if (realizedObject != null)
            {
                return;
            }
            realizedObject = new MiniEnergyCell(this);
            for (int i = 0; i < stuckObjects.Count; i++)
            {
                if (stuckObjects[i].A.realizedObject == null && stuckObjects[i].A != this)
                {
                    stuckObjects[i].A.Realize();
                }
                if (stuckObjects[i].B.realizedObject == null && stuckObjects[i].B != this)
                {
                    stuckObjects[i].B.Realize();
                }
            }
        }

        public override string ToString()
        {
            return this.SaveToString();
        }
    }
}
