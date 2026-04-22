using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Objects.MarkItem
{
    public class MarkFisobs : Fisob
    {
        public MarkFisobs() : base(CreatureTemplateType.Mark)
        {
            Icon = new DefaultIcon();
            MarkHooks.Hook();
        }

        public override AbstractPhysicalObject Parse(World world, EntitySaveData entitySaveData, SandboxUnlock unlock)
        {
            Enum.TryParse<Mark.MarkType>(entitySaveData.CustomData, out var type);
            return new Mark.MarkAbstract(world, entitySaveData.Pos, entitySaveData.ID)
            {
                markType = type
            };
        }

        public override ItemProperties Properties(PhysicalObject forObject)
        {
            return new MarkProps();
        }

        public class MarkProps : ItemProperties
        {
            public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
            {
                grabability = Player.ObjectGrabability.OneHand;
            }
        }
    }
}
