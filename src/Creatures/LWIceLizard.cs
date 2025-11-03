using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.Creatures
{
    public class LWIceLizard : Lizard
    {
        public LizardBreedParams.SpeedMultiplier[] origSpeed;

        public LizardBreedParams.SpeedMultiplier[] greenLizardSpeed;

        public LWIceLizardGraphics IceGraphics
        {
            get
            {
                return graphicsModule as LWIceLizardGraphics;
            }
        }

        public LWIceLizard(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            origSpeed = (LizardBreedParams.SpeedMultiplier[])lizardParams.terrainSpeeds.Clone();
            greenLizardSpeed = (LizardBreedParams.SpeedMultiplier[])(StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.GreenLizard).breedParameters as LizardBreedParams).terrainSpeeds.Clone();
        }

        public override void InitiateGraphicsModule()
        {
            graphicsModule ??= new LWIceLizardGraphics(this);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (IceGraphics == null) return;

            SpeedCamoLerp(1);
            SpeedCamoLerp(3);

            void SpeedCamoLerp(int index)
            {
                lizardParams.terrainSpeeds[index].speed = Mathf.Lerp(origSpeed[index].speed, greenLizardSpeed[index].speed / 2, IceGraphics.whiteCamoColorAmount);
                lizardParams.terrainSpeeds[index].horizontal = Mathf.Lerp(origSpeed[index].horizontal, greenLizardSpeed[index].horizontal / 2, IceGraphics.whiteCamoColorAmount);
                lizardParams.terrainSpeeds[index].down = Mathf.Lerp(origSpeed[index].down, greenLizardSpeed[index].down / 2, IceGraphics.whiteCamoColorAmount);
                lizardParams.terrainSpeeds[index].up = Mathf.Lerp(origSpeed[index].up, greenLizardSpeed[index].up / 2, IceGraphics.whiteCamoColorAmount);
            }
        }
    }
}
