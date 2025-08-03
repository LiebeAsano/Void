using RWCustom;
using UnityEngine;

namespace VoidTemplate;

    
    public class DartSpiGraphics : BigSpiderGraphics
    {
        public DartSpiGraphics(PhysicalObject ow) : base(ow)
        {
            this.bug = (ow as Dartspider);
            this.tailEnd = new GenericBodyPart(this, 3f, 0.5f, 0.99f, this.bug.bodyChunks[1]);
            this.lastDarkness = -1f;
            this.legLength =  80f;
            this.mandibles = new GenericBodyPart[2];
            for (int i = 0; i < this.mandibles.GetLength(0); i++)
            {
                this.mandibles[i] = new GenericBodyPart(this, 1f, 0.5f, 0.9f, base.owner.bodyChunks[0]);
            }
            this.legs = new Limb[2, 4];
            this.legFlips = new float[2, 4, 2];
            this.legsTravelDirs = new Vector2[2, 4];
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                for (int k = 0; k < this.legs.GetLength(1); k++)
                {
                    this.legs[j, k] = new Limb(this, this.bug.mainBodyChunk, j * 4 + k, 0.1f, 0.7f, 0.99f, 12f , 0.95f);
                }
            }
            this.bodyParts = new BodyPart[11];
            this.bodyParts[0] = this.tailEnd;
            this.bodyParts[1] = this.mandibles[0];
            this.bodyParts[2] = this.mandibles[1];
            int num = 3;
            for (int l = 0; l < this.legs.GetLength(0); l++)
            {
                for (int m = 0; m < this.legs.GetLength(1); m++)
                {
                    this.bodyParts[num] = this.legs[l, m];
                    num++;
                }
            }
            this.totalScales = 0;
            Random.State state = Random.state;
            Random.InitState(this.bug.abstractCreature.ID.RandomSeed);
            this.scales = new Vector2[(this.Spitter ? 10 : 0) + Random.Range(this.Spitter ? 16 : 10, Random.Range(20, 28))][,];
            this.scaleStuckPositions = new Vector2[this.scales.Length];
            this.scaleSpecs = new Vector2[this.scales.Length, 2];
            this.legsThickness = Mathf.Lerp(0.7f, 1.1f, Random.value);
            this.bodyThickness = Mathf.Lerp(0.9f, 1.1f, Random.value) + (2.5f );
            if (this.Mother)
            {
                this.bodyThickness = Mathf.Lerp(0.9f, 1.1f, Random.value) + 5f;
            }
            if (Random.value < 0.5f)
            {
                this.deadLeg = new IntVector2(Random.Range(0, 2), Random.Range(0, 4));
            }
            num = 0; 
        for (int num3 = 0; num3 < this.scales.Length; num3++)
        {
            this.scaleSpecs[num3, 0] = new Vector2(Random.value, 5f);
            if (num3 % 3 == 0)
            {
                this.scales[num3] = new Vector2[Random.Range(2, Random.Range(7, 13)), 4];
            }
            else
            {
                this.scales[num3] = new Vector2[Random.Range(2, Random.Range(4, 5)), 4];
            }
            this.totalScales += this.scales[num3].GetLength(0);
            for (int num4 = 0; num4 < this.scales[num3].GetLength(0); num4++)
            {
                this.scales[num3][num4, 3].x = (float)num;
                num++;
            }
            this.scaleStuckPositions[num3] = new Vector2(Mathf.Lerp(-0.5f, 0.5f, Random.value), Mathf.Pow(Random.value, Custom.LerpMap((float)this.scales[num3].GetLength(0), 2f, 9f, 0.5f, 2f)));
            if (num3 % 3 == 0 && this.scaleStuckPositions[num3].y > 0.5f)
            {
                Vector2[] array = this.scaleStuckPositions;
                int num5 = num3;
                array[num5].y = array[num5].y * 0.5f;
            }
        }
        


        Random.state = state;
            this.soundLoop = new ChunkDynamicSoundLoop(this.bug.mainBodyChunk);
            this.Reset();
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            float num;
            if (ModManager.DLCShared && (base.owner as Creature).abstractCreature.Winterized)
            {
                this.blackColor = new Color(1f, 1f, 1f);
                this.yellowCol = this.bug.yellowCol;
                num = 1f;
            }
            else
            {
                this.blackColor = new Color(1f, 1f, 1f);
                this.yellowCol = Color.Lerp(this.bug.yellowCol, palette.fogColor, 0.2f);
                num = 1f - this.darkness;
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].color = this.blackColor;
            }
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    (sLeaser.sprites[this.MandibleSprite(j, 1)] as CustomFSprite).verticeColors[k] = this.blackColor;
                }
            }
            for (int l = 0; l < this.scales.Length; l++)
            {
                for (int m = 0; m < this.scales[l].GetLength(0); m++)
                {
                    float num2 = (Mathf.InverseLerp(0f, (float)(this.scales[l].GetLength(0) - 1), (float)m) + Mathf.InverseLerp(0f, 5f, (float)m)) / 2f;
                    sLeaser.sprites[this.FirstScaleSprite + (int)this.scales[l][m, 3].x].color = Color.Lerp(this.blackColor, this.yellowCol, num2 * Mathf.Lerp(0.3f, 0.9f, this.scaleSpecs[l, 0].x) * num);
                }
            }
        }
    }

