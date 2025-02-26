using CoralBrain;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoidTemplate.Creatures
{
    public class OutspectorGraphics : GraphicsModule, IDrawable, IOwnMycelia
    {
        public OutspectorGraphics(PhysicalObject ow) : base(ow, false)
        {
            this.ropeGraphics = new OutspectorGraphics.OutspectorHeadRopeGraphics[Outspector.headCount()];
            for (int i = 0; i < Outspector.headCount(); i++)
            {
                this.ropeGraphics[i] = new OutspectorGraphics.OutspectorHeadRopeGraphics(this, i);
            }
            this.cullRange = 300f;
            this.JawAngleWiggler = new float[Outspector.headCount()];
            this.mycelia = new Mycelium[(int)(5f + (5f * this.myOutspector.room.world.game.SeededRandom(this.myOutspector.abstractCreature.ID.RandomSeed)))];
            this.blinks = new float[Outspector.headCount()];
            this.wingBodyParts = new GenericBodyPart[(this.myOutspector.State as Outspector.OutspectorState).Wingnumber];
            for (int j = 0; j < this.wingBodyParts.Length; j++)
            {
                this.wingBodyParts[j] = new GenericBodyPart(this, 15f, 0.2f, 0.1f, this.myOutspector.firstChunk);
            }
            this.wingBodyPartDistance = 75f;
            for (int k = 0; k < this.mycelia.GetLength(0); k++)
            {
                this.mycelia[k] = new Mycelium(this.myOutspector.neuronSystem, this, k, Mathf.Lerp(120f, 300f, this.myOutspector.room.world.game.SeededRandom(this.myOutspector.abstractCreature.ID.RandomSeed + k)), this.myOutspector.mainBodyChunk.pos)
                {
                    useStaticCulling = false,
                    color = this.myOutspector.bodyColor
                };
                this.bodyRotation = 0f;
            }
        }

        private Outspector myOutspector
        {
            get
            {
                return base.owner as Outspector;
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.blackColor = palette.blackColor;
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.SpritesTotal_All];
            sLeaser.sprites[this.SpritesBegin_Core] = new FSprite("Circle20", true);
            sLeaser.sprites[this.SpritesBegin_Core + 1] = new FSprite("Circle20", true);
            sLeaser.sprites[this.SpritesBegin_Core + 2] = new FSprite("Circle20", true);
            sLeaser.sprites[this.SpritesBegin_Core + 3] = new FSprite("Circle20", true);
            for (int i = 0; i < this.SpritesTotal_mycelium; i++)
            {
                this.mycelia[i].InitiateSprites(this.SpritesBegin_mycelium + i, sLeaser, rCam);
            }
            this.wingflapCounters = new float[this.SpritesTotal_wings];
            for (int j = 0; j < this.SpritesTotal_wings; j++)
            {
                sLeaser.sprites[this.SpritesBegin_wings + j] = new FSprite("CicadaWingA", true)
                {
                    anchorX = 0f,
                    scaleX = Mathf.Lerp(3.6f, 5f, this.myOutspector.room.world.game.SeededRandom(this.myOutspector.abstractCreature.ID.RandomSeed)),
                    scaleY = 0.8f,
                    alpha = 0.3f,
                    shader = rCam.room.game.rainWorld.Shaders["CicadaWing"]
                };
            }
            int num = 0;
            for (int k = 0; k < Outspector.headCount(); k++)
            {
                for (int l = 0; l < this.SpritesTotal_singlehead(); l++)
                {
                    this.ropeGraphics[k].InitiateSprites(sLeaser, rCam, this.SpritesBegin_heads + num);
                    sLeaser.sprites[this.SpritesBegin_heads + num + 1] = new FSprite("Circle20", true)
                    {
                        scale = 0.75f
                    };
                    sLeaser.sprites[this.SpritesBegin_heads + num + 2] = new FSprite("FlyWing", true)
                    {
                        anchorY = 0f,
                        scaleY = 1.5f
                    };
                    sLeaser.sprites[this.SpritesBegin_heads + num + 3] = new FSprite("FlyWing", true)
                    {
                        anchorY = 0f,
                        scaleY = 1.5f
                    };
                    sLeaser.sprites[this.SpritesBegin_heads + num + 4] = new FSprite("FlyWing", true)
                    {
                        anchorY = 0f
                    };
                    sLeaser.sprites[this.SpritesBegin_heads + num + 5] = new FSprite("FlyWing", true)
                    {
                        anchorY = 0f
                    };
                }
                num += this.SpritesTotal_singlehead();
                sLeaser.sprites[this.SpritesBegin_Eye(k)] = new FSprite("Circle20", true)
                {
                    scaleY = 0.625f,
                    scaleX = 0.45f
                };
            }
            this.AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (this.culled || this.myOutspector.room == null)
            {
                return;
            }
            Vector2 pos = this.myOutspector.mainBodyChunk.pos;
            for (int i = 0; i < this.mycelia.Length; i++)
            {
                this.mycelia[i].DrawSprites(this.SpritesBegin_mycelium + i, sLeaser, rCam, timeStacker, camPos);
                sLeaser.sprites[i].isVisible = !this.mycelia[i].culled;
            }
            Color bodyColor = this.myOutspector.bodyColor;
            HSLColor hslcolor = new(0.2f, 0.2f, 0.4f);
            this.wingColor = Color.Lerp(bodyColor, hslcolor.rgb, 0.4f);
            this.wingColor.a = 0.2f;
            for (int j = 0; j < this.SpritesTotal_wings; j++)
            {
                Vector2 b = Custom.DegToVec((360f / SpritesTotal_wings * j) + this.bodyRotation) * this.wingBodyPartDistance;
                float num = 360f / wingBodyParts.Length * j;
                sLeaser.sprites[this.SpritesBegin_wings + j].x = pos.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_wings + j].y = pos.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_wings + j].color = this.wingColor;
                sLeaser.sprites[this.SpritesBegin_wings + j].scaleX = Mathf.Lerp(0f, Mathf.Lerp(3.6f, 6f, this.myOutspector.room.world.game.SeededRandom(this.myOutspector.abstractCreature.ID.RandomSeed)) * Mathf.InverseLerp(1f, 0.8f, this.myOutspector.squeezeFac), Mathf.InverseLerp(1f, 0.55f, Vector2.Distance(this.wingBodyParts[j].pos, this.myOutspector.firstChunk.pos + b) / this.wingBodyPartDistance));
                this.wingflapCounters[j] += 0.02f;
                this.wingflapCounters[j] += this.findWingFlapIntensity(j, this.myOutspector.mainBodyChunk.vel * -1f) / 8f;
                this.wingflapCounters[j] += this.findWingFlapIntensity(j, this.myOutspector.flyingPower) * 3f;
                float num2 = Mathf.Sin(this.wingflapCounters[j]) * 8f;
                sLeaser.sprites[this.SpritesBegin_wings + j].rotation = num + this.bodyRotation - 90f;
                sLeaser.sprites[this.SpritesBegin_wings + j].rotation += num2;
            }
            for (int k = 0; k < Outspector.headCount(); k++)
            {
                this.ropeGraphics[k].DrawSprite(sLeaser, rCam, timeStacker, camPos);
            }
            for (int l = 0; l < this.SpritesTotal_Core; l++)
            {
                sLeaser.sprites[l].x = pos.x - camPos.x;
                sLeaser.sprites[l].y = pos.y - camPos.y;
                sLeaser.sprites[l].scale = 1.3f + (Mathf.Sin(this.myOutspector.lightpulse + (l / (float)l)) / 4f);
                sLeaser.sprites[l].color = this.myOutspector.bodyColor;
            }
            int num3 = 0;
            for (int m = 0; m < Outspector.headCount(); m++)
            {
                Vector2 lastPos = this.myOutspector.heads[m].tChunks[this.myOutspector.heads[m].tChunks.Length - 2].lastPos;
                Vector2 lastPos2 = this.myOutspector.heads[m].Tip.lastPos;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].x = lastPos2.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].y = lastPos2.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].color = this.myOutspector.bodyColor;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].scaleX = 0.8f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].scaleY = 1.4f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].anchorY = 0.3f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 1].rotation = Custom.AimFromOneVectorToAnother(lastPos, lastPos2);
                Vector2 vector = Custom.DegToVec(Custom.AimFromOneVectorToAnother(lastPos, lastPos2));
                vector *= 10f;
                Color blue;
                if (this.myOutspector.activeEye == m && !this.myOutspector.HeadsCrippled(this.myOutspector.activeEye))
                {
                    blue = Color.blue;
                }
                else
                {
                    blue = new Color(0f, 0.02f, 0.2f);
                }
                sLeaser.sprites[this.SpritesBegin_Eye(m)].x = lastPos2.x + vector.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_Eye(m)].y = lastPos2.y + vector.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_Eye(m)].color = Color.Lerp(sLeaser.sprites[this.SpritesBegin_Eye(m)].color, blue, 0.2f);
                sLeaser.sprites[this.SpritesBegin_Eye(m)].rotation = Custom.AimFromOneVectorToAnother(lastPos, lastPos2);
                sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleY = 0.625f;
                if ((this.myOutspector.State as Outspector.OutspectorState).headHealth[m] > 0f)
                {
                    sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleX = Mathf.Lerp(Mathf.Lerp(sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleX, Mathf.Lerp(0.525f, 0.225f, this.myOutspector.anger), 0.15f), 0.1f, this.blinks[m]);
                    sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleX = Mathf.Lerp(sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleX, 0.25f + (Mathf.Sin(myOutspector.blind) / 4f), Mathf.InverseLerp(0f, 500f, myOutspector.blind));
                }
                else
                {
                    sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleX = Mathf.Lerp(sLeaser.sprites[this.SpritesBegin_Eye(m)].scaleX, 0.125f, 0.06f);
                }
                vector = Custom.DegToVec(Custom.AimFromOneVectorToAnother(lastPos, lastPos2));
                vector *= 12f;
                Vector2 vector2 = vector;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 2].x = lastPos2.x + vector2.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 2].y = lastPos2.y + vector2.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 2].color = this.myOutspector.bodyColor;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 2].rotation = Custom.AimFromOneVectorToAnother(lastPos, lastPos2) + this.JawAngle[m];
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 2].scaleY = 1.1f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 2].scaleX = 0.8f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 3].x = lastPos2.x + vector2.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 3].y = lastPos2.y + vector2.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 3].color = this.myOutspector.bodyColor;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 3].rotation = Custom.AimFromOneVectorToAnother(lastPos, lastPos2) + 180f - this.JawAngle[m];
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 3].scaleY = -1.1f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 3].scaleX = 0.8f;
                vector = Custom.DegToVec(Custom.AimFromOneVectorToAnother(lastPos, lastPos2));
                vector *= -5f;
                vector2 = vector;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 4].x = lastPos2.x + vector2.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 4].y = lastPos2.y + vector2.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 4].color = this.myOutspector.bodyColor;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 4].rotation = Custom.AimFromOneVectorToAnother(lastPos, lastPos2) + this.JawAngle[m] - 5f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 4].scaleY = 2.2f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 4].scaleX = -1.3f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 5].x = lastPos2.x + vector2.x - camPos.x;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 5].y = lastPos2.y + vector2.y - camPos.y;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 5].color = this.myOutspector.bodyColor;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 5].rotation = Custom.AimFromOneVectorToAnother(lastPos, lastPos2) + 180f - this.JawAngle[m] + 5f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 5].scaleY = -2.2f;
                sLeaser.sprites[this.SpritesBegin_heads + num3 + 5].scaleX = -1.3f;
                num3 += this.SpritesTotal_singlehead();
            }
            for (int n = 0; n < this.mycelia.GetLength(0); n++)
            {
                this.mycelia[n].UpdateColor(this.myOutspector.bodyColor, 0f, this.SpritesBegin_mycelium + n, sLeaser);
            }
        }

        public override void Update()
        {
            base.Update();
            if (this.culled)
            {
                return;
            }
            for (int i = 0; i < this.wingBodyParts.Length; i++)
            {
                Vector2 a = Custom.DegToVec((360f / SpritesTotal_wings * i) + this.bodyRotation) * this.wingBodyPartDistance;
                this.wingBodyParts[i].ConnectToPoint(this.myOutspector.firstChunk.pos, this.wingBodyPartDistance, false, 0.3f, this.myOutspector.firstChunk.vel + (a / 3f), 0.25f, 0.1f);
                if (Vector2.Distance(this.myOutspector.firstChunk.pos, this.wingBodyParts[i].pos) > this.wingBodyPartDistance * 1.1f || this.OwnerRoom.aimap.getAItile(this.myOutspector.firstChunk.pos).narrowSpace)
                {
                    this.wingBodyParts[i].pos = this.myOutspector.firstChunk.pos;
                }
                this.wingBodyParts[i].Update();
            }
            for (int j = 0; j < this.mycelia.Length; j++)
            {
                this.mycelia[j].Update();
            }
            if (this.JawAngle == null)
            {
                this.JawAngle = new float[Outspector.headCount()];
            }
            for (int k = 0; k < Outspector.headCount(); k++)
            {
                if (this.blinks[k] > 0f)
                {
                    this.blinks[k] += 0.1f;
                    if (this.blinks[k] > 1f)
                    {
                        this.blinks[k] = 0f;
                    }
                }
                else if (Random.value < 0.01f)
                {
                    this.blinks[k] = 0.01f;
                }
                this.ropeGraphics[k].Update();
                if ((this.myOutspector.State as Outspector.OutspectorState).headHealth[k] <= 0f)
                {
                    this.JawAngleWiggler[k] += Random.value * 0.01f;
                    this.JawAngle[k] = Mathf.Lerp(this.JawAngle[k], this.JawAngle[k] + (Mathf.Sin(this.JawAngleWiggler[k]) * 1.2f), 0.1f);
                }
                else
                {
                    this.JawAngleWiggler[k] += Random.value * (0.1f + this.myOutspector.anger);
                    if (this.myOutspector.headWantToGrabChunk[k] != null)
                    {
                        this.JawAngle[k] = Mathf.Lerp(this.JawAngle[k], 25f, 0.15f) + Mathf.Sin(this.JawAngleWiggler[k]);
                    }
                    if (this.myOutspector.headGrabChunk[k] != null)
                    {
                        this.JawAngle[k] = Mathf.Lerp(this.JawAngle[k], -15f, 0.45f) + Mathf.Sin(this.JawAngleWiggler[k]);
                    }
                    this.JawAngle[k] = Mathf.Lerp(this.JawAngle[k], -5f, 0.1f) + Mathf.Sin(this.JawAngleWiggler[k]);
                }
                if (this.myOutspector.dying > 0f && this.myOutspector.room.ViewedByAnyCamera(this.myOutspector.mainBodyChunk.pos, 900f))
                {
                    for (int l = 0; l < 15; l++)
                    {
                        int num = (int)Random.Range(0f, myOutspector.heads[k].tChunks.Length);
                        this.myOutspector.room.AddObject(new OverseerEffect(this.myOutspector.heads[k].tChunks[num].pos, Custom.RNV() * Random.value * 0.1f, this.myOutspector.bodyColor, Mathf.Lerp(200f, 15f, this.myOutspector.dying), Mathf.Lerp(1.5f, 0.1f, this.myOutspector.dying)));
                    }
                    for (int m = 0; m < 8; m++)
                    {
                        int num2 = (int)Random.Range(0f, myOutspector.heads[k].tChunks.Length);
                        this.myOutspector.room.AddObject(new Spark(this.myOutspector.heads[k].tChunks[num2].pos, (this.myOutspector.mainBodyChunk.vel * 0.5f) + (Custom.RNV() * 14f * Random.value), this.myOutspector.bodyColor, null, 14, 21));
                    }
                }
            }
            if (this.myOutspector.Consious)
            {
                this.bodyRotation += this.myOutspector.flyingPower.x * 3f;
            }
        }

        public Vector2 ConnectionPos(int index, float timeStacker)
        {
            return this.myOutspector.mainBodyChunk.pos;
        }

        public Vector2 ResetDir(int index)
        {
            return this.myOutspector.mainBodyChunk.vel;
        }

        public Room OwnerRoom
        {
            get
            {
                return base.owner.room;
            }
        }

        public void UpdateNeuronSystemForMycelia()
        {
            for (int i = 0; i < this.mycelia.Length; i++)
            {
                if (this.mycelia[i].system != this.myOutspector.neuronSystem)
                {
                    this.mycelia[i].system?.mycelia.Remove(this.mycelia[i]);
                    this.myOutspector.neuronSystem?.mycelia.Add(this.mycelia[i]);
                    this.mycelia[i].system = this.myOutspector.neuronSystem;
                }
            }
        }

        private int SpritesBegin_Core
        {
            get
            {
                return 0;
            }
        }

        private int SpritesTotal_Core
        {
            get
            {
                return 4;
            }
        }

        private int SpritesBegin_mycelium
        {
            get
            {
                return this.SpritesTotal_Core;
            }
        }

        private int SpritesTotal_mycelium
        {
            get
            {
                return this.mycelia.Length;
            }
        }

        private int SpritesTotal_All
        {
            get
            {
                return this.SpritesTotal_Core + this.SpritesTotal_mycelium + this.SpritesTotal_wings + this.SpritesTotal_heads;
            }
        }

        public override void Reset()
        {
            for (int i = 0; i < Outspector.headCount(); i++)
            {
                this.ropeGraphics[i].AddToPositionsList(0, this.myOutspector.mainBodyChunk.pos);
                this.ropeGraphics[i].AddToPositionsList(1, this.myOutspector.heads[i].Tip.pos);
                this.ropeGraphics[i].AlignAndConnect(2);
            }
            base.Reset();
            for (int j = 0; j < this.mycelia.Length; j++)
            {
                this.mycelia[j].Reset(this.myOutspector.mainBodyChunk.pos);
            }
        }

        public int SpritesBegin_wings
        {
            get
            {
                return this.SpritesTotal_Core + this.SpritesTotal_mycelium;
            }
        }

        public int SpritesTotal_wings
        {
            get
            {
                return (this.myOutspector.State as Outspector.OutspectorState).Wingnumber;
            }
        }

        private float findWingFlapIntensity(int wing, Vector2 inputvec)
        {
            float num = 360f / SpritesTotal_wings * wing;
            return Mathf.InverseLerp(0f, 180f, Mathf.DeltaAngle(num + this.bodyRotation, inputvec.GetAngle())) * inputvec.magnitude;
        }

        public int SpritesBegin_heads
        {
            get
            {
                return this.SpritesTotal_Core + this.SpritesTotal_mycelium + this.SpritesTotal_wings;
            }
        }

        public int SpritesTotal_heads
        {
            get
            {
                int i = 0;
                int num = 0;
                while (i < Outspector.headCount())
                {
                    num += this.SpritesTotal_singlehead();
                    i++;
                }
                return num;
            }
        }

        public int SpritesBegin_SingleNeck(int index)
        {
            return this.SpritesBegin_heads + (this.SpritesTotal_singlehead() * index);
        }

        public int SpritesTotal_singlehead()
        {
            return 7;
        }

        public int SpritesBegin_Eye(int index)
        {
            return this.SpritesBegin_heads + (this.SpritesTotal_singlehead() * index) + (this.SpritesTotal_singlehead() - 1);
        }

        private float RadOfSegment(float f, float timeStacker)
        {
            return this.myOutspector.Rad(f) * Mathf.Pow(1f - Mathf.Lerp(this.myOutspector.lastDying, this.myOutspector.dying, timeStacker), 0.2f);
        }

        private Color blackColor;


        public Mycelium[] mycelia;

        private Color wingColor;

        private float[] wingflapCounters;

        private float bodyRotation;

        public OutspectorGraphics.OutspectorHeadRopeGraphics[] ropeGraphics;

        public float[] JawAngle;

        private float[] JawAngleWiggler;

        public float[] blinks;

        private GenericBodyPart[] wingBodyParts;

        private float wingBodyPartDistance;

        public class OutspectorHeadRopeGraphics : RopeGraphic
        {
            public override void Update()
            {
                int listCount = 0;
                base.AddToPositionsList(listCount++, this.owner.myOutspector.heads[this.headNumber].FloatBase);
                for (int i = 0; i < this.owner.myOutspector.heads[this.headNumber].tChunks.Length; i++)
                {
                    for (int j = 1; j < this.owner.myOutspector.heads[this.headNumber].tChunks[i].rope.TotalPositions; j++)
                    {
                        base.AddToPositionsList(listCount++, this.owner.myOutspector.heads[this.headNumber].tChunks[i].rope.GetPosition(j));
                    }
                }
                base.AlignAndConnect(listCount);
            }

            public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 vector = this.owner.myOutspector.mainBodyChunk.pos;
                vector += Custom.DirVec(Vector2.Lerp(this.segments[1].lastPos, this.segments[1].pos, timeStacker), vector) * 1f;
                float a = (this.owner.RadOfSegment(0f, timeStacker) * 1.7f) + 2f;
                for (int i = 0; i < this.segments.Length; i++)
                {
                    float f = i / (float)(this.segments.Length - 1);
                    Vector2 vector2 = Vector2.Lerp(this.segments[i].lastPos, this.segments[i].pos, timeStacker);
                    if (i >= this.segments.Length - 1)
                    {
                        vector2 += Custom.DirVec(vector, vector2) * 1f;
                    }
                    else
                    {
                        vector2 = Vector2.Lerp(this.segments[i + 1].lastPos, this.segments[i + 1].pos, timeStacker);
                    }
                    Vector2 a2 = Custom.PerpendicularVector((vector - vector2).normalized);
                    float num = (this.owner.RadOfSegment(f, timeStacker) * 1.7f) + 2f;
                    (sLeaser.sprites[this.spriteOffset] as TriangleMesh).color = this.owner.myOutspector.bodyColor;
                    (sLeaser.sprites[this.spriteOffset] as TriangleMesh).alpha = this.owner.myOutspector.lightpulse;
                    (sLeaser.sprites[this.spriteOffset] as TriangleMesh).MoveVertice(i * 4, vector - (a2 * Mathf.Lerp(a, num, 0.5f)) - camPos);
                    (sLeaser.sprites[this.spriteOffset] as TriangleMesh).MoveVertice((i * 4) + 1, vector + (a2 * Mathf.Lerp(a, num, 0.5f)) - camPos);
                    (sLeaser.sprites[this.spriteOffset] as TriangleMesh).MoveVertice((i * 4) + 2, vector2 - (a2 * num) - camPos);
                    (sLeaser.sprites[this.spriteOffset] as TriangleMesh).MoveVertice((i * 4) + 3, vector2 + (a2 * num) - camPos);
                    vector = vector2;
                    a = num;
                }
            }

            public override void MoveSegment(int segment, Vector2 goalPos, Vector2 smoothedGoalPos)
            {
                this.segments[segment].vel *= 0f;
                if (this.owner.myOutspector.room.GetTile(smoothedGoalPos).Solid && !this.owner.myOutspector.room.GetTile(goalPos).Solid)
                {
                    FloatRect floatRect = Custom.RectCollision(smoothedGoalPos, goalPos, this.owner.myOutspector.room.TileRect(this.owner.myOutspector.room.GetTilePosition(smoothedGoalPos)).Grow(3f));
                    this.segments[segment].pos = new Vector2(floatRect.left, floatRect.bottom);
                    return;
                }
                this.segments[segment].pos = smoothedGoalPos;
            }

            public Vector2 OnTubePos(Vector2 pos, float timeStacker)
            {
                Vector2 p = this.OneDimensionalTubePos(pos.y - (1f / segments.Length), timeStacker);
                Vector2 p2 = this.OneDimensionalTubePos(pos.y + (1f / segments.Length), timeStacker);
                return this.OneDimensionalTubePos(pos.y, timeStacker) + (Custom.PerpendicularVector(Custom.DirVec(p, p2)) * pos.x);
            }

            public Vector2 OnTubeDir(float floatPos, float timeStacker)
            {
                Vector2 p = this.OneDimensionalTubePos(floatPos - (1f / segments.Length), timeStacker);
                Vector2 p2 = this.OneDimensionalTubePos(floatPos + (1f / segments.Length), timeStacker);
                return Custom.DirVec(p, p2);
            }

            public Vector2 OneDimensionalTubePos(float floatPos, float timeStacker)
            {
                int num = Custom.IntClamp(Mathf.FloorToInt(floatPos * (this.segments.Length - 1)), 0, this.segments.Length - 1);
                int num2 = Custom.IntClamp(num + 1, 0, this.segments.Length - 1);
                float t = Mathf.InverseLerp(num, num2, floatPos * (this.segments.Length - 1));
                return Vector2.Lerp(Vector2.Lerp(this.segments[num].lastPos, this.segments[num2].lastPos, t), Vector2.Lerp(this.segments[num].pos, this.segments[num2].pos, t), timeStacker);
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, int sproffset)
            {
                this.spriteOffset = sproffset;
                sLeaser.sprites[this.spriteOffset] = TriangleMesh.MakeLongMesh(this.segments.Length, false, false);
                sLeaser.sprites[this.spriteOffset].shader = rCam.game.rainWorld.Shaders["OverseerZip"];
                sLeaser.sprites[this.spriteOffset].alpha = 0.7f + (0.1f * Random.value);
            }

            public OutspectorHeadRopeGraphics(OutspectorGraphics owner, int head) : base(40)
            {
                this.owner = owner;
                this.headNumber = head;
            }

            private int headNumber;

            private OutspectorGraphics owner;

            private int spriteOffset;
        }
    }
}
