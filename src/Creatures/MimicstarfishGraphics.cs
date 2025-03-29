// This code was made by ratrat (https://github.com/ratrat44) and is included in Fisobs with his permission.

using RWCustom;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace VoidTemplate;

internal sealed class MimicstarfishGraphics : DaddyGraphics
{
    public float Camouflaged
    {
        get
        {
            if (this.whiteCamoColorAmount == -1f)
            {
                return 1f;
            }
            return this.whiteCamoColorAmount;
        }
    }
    public Mimicstarfish star
    {
        get
        {
            return base.owner as Mimicstarfish;
        }
    }

    public MimicstarfishGraphics(Mimicstarfish ow) : base(ow)
    {
        this.cullRange = 1400f;
        Random.State state = Random.state;
        Random.InitState(this.daddy.graphicsSeed);
        int num2 = 0;
        starlegGraphics = new MimicstarfishGraphics.StarLegGraphic[star.tentacles.Length];
        for (int i = 0; i < this.legGraphics.Length; i++)
        {
            starlegGraphics[i] = new MimicstarfishGraphics.StarLegGraphic(this, i, num2);
            num2 += this.starlegGraphics[i].sprite;
        }
        this.deadLegs = new DaddyGraphics.DaddyDeadLeg[0];
        this.totalDeadLegSprites = num2 - this.totalLegSprites;
        DaddyGraphics.DaddyDangleTube.Connection connection = this.GenerateDangleCon(Random.value < 0.5f);
        DaddyGraphics.DaddyDangleTube.Connection connection2 = this.GenerateDangleCon(connection.chunk != null);
        this.danglers = new DaddyGraphics.DaddyDangleTube[0];

        this.totalDanglers = num2 - this.totalLegSprites - this.totalDeadLegSprites;

        // Create the eyes array once with the proper length.
        this.eyes = new DaddyGraphics.Eye[this.daddy.bodyChunks.Length];
        for (int m = 0; m < this.daddy.bodyChunks.Length; m++)
        {
            // Pass a default value (0) as the firstSprite parameter.
            this.eyes[m] = new Eye(this, this, m, daddy.bodyChunks[m].rad, daddy.HDmode, 0);
        }
    }

    public override void Update()
    {
        base.Update();
        if (!this.culled)
        {
            for (int j = 0; j < this.legGraphics.Length; j++)
            {
                this.starlegGraphics[j].Update();
            }
            for (int l = 0; l < this.star.bodyChunks.Length; l++)
            {
                this.eyes[l].Update();
            }
        }

        this.digestLoop.Update();
        if (this.digestLoop.volume > 0f)
        {
            this.digestLoop.pos = this.star.MiddleOfBody;
        }
        this.digestLoop.volume = Mathf.Lerp(this.digestLoop.volume, Mathf.Pow(this.digesting, 0.8f), 0.2f);
        if (this.reactionSoundDelay > 0)
        {
            this.reactionSoundDelay--;
        }
        if (this.feelSomethingReactionDelay > 0)
        {
            this.feelSomethingReactionDelay--;
        }
        this.digesting = Mathf.Lerp(this.digesting, Mathf.Clamp(Mathf.Pow(this.star.MostDigestedEatObject, 0.5f), 0f, 1f), 0.1f);
        if (this.star.dead)
        {
            this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 0.25f, 0.0001f);
        }
        else
        {
            if (((HealthState)this.star.State).health < 0.6f && Random.value * 1.5f < ((HealthState)this.star.State).health && Random.value < 1f / (this.star.Stunned ? 10f : 40f))
            {
                this.whiteGlitchFit = (int)Mathf.Lerp(5f, 40f, (1f - ((HealthState)this.star.State).health) * Random.value);
            }
            if (this.whiteGlitchFit == 0 && this.star.Stunned && Random.value < 0.05f)
            {
                this.whiteGlitchFit = 2;
            }
            if (this.whiteGlitchFit > 0)
            {
                this.whiteGlitchFit--;
                float f = 1f - ((HealthState)this.star.State).health;
                if (Random.value < 0.2f)
                {
                    this.whiteCamoColorAmountDrag = 1f;
                }
                if (Random.value < 0.2f)
                {
                    this.whiteCamoColorAmount = 1f;
                }
                if (Random.value < 0.5f)
                {
                    this.whiteCamoColor = Color.Lerp(this.whiteCamoColor, new Color(Random.value, Random.value, Random.value), Mathf.Pow(f, 0.2f) * Mathf.Pow(Random.value, 0.1f));
                }
                if (Random.value < 0.33333334f)
                {
                    this.whitePickUpColor = new Color(Random.value, Random.value, Random.value);
                }
            }
            else if (this.showDominance > 0f)
            {
                this.whiteDominanceHue += Random.value * Mathf.Pow(this.showDominance, 2f) * 0.2f;
                if (this.whiteDominanceHue > 1f)
                {
                    this.whiteDominanceHue -= 1f;
                }
                this.whiteCamoColor = Color.Lerp(this.whiteCamoColor, Custom.HSL2RGB(this.whiteDominanceHue, 1f, 0.5f), Mathf.InverseLerp(0.5f, 1f, Mathf.Pow(this.showDominance, 0.5f)) * Random.value);
                this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 1f - Mathf.Sin(Mathf.InverseLerp(0f, 1.1f, Mathf.Pow(this.showDominance, 0.5f)) * 3.1415927f), 0.1f);
            }
            else
            {
                if (Random.value < 0.1f)
                {
                    this.CamoAmountControlled();
                }
                this.whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(this.whiteCamoColorAmount, this.whiteCamoColorAmountDrag, 0.1f * Random.value), 0.15f, 1f);
                this.whiteCamoColor = Color.Lerp(this.whiteCamoColor, this.whitePickUpColor, 0.1f);
            }
        }
    }

    public void CamoAmountControlled()
    {
        if (!this.star.safariControlled)
        {
            this.whiteCamoColorAmountDrag = Mathf.Lerp(this.whiteCamoColorAmountDrag, Mathf.InverseLerp(0.65f, 0.4f, this.star.bodyChunks[0].vel.magnitude - 4f), Random.value);
            return;
        }
        if (this.daddy.inputWithDiagonals == null || !this.daddy.inputWithDiagonals.Value.thrw)
        {
            this.whiteCamoColorAmountDrag = 0f;
            return;
        }
        if (this.star.bodyChunks[0].vel.magnitude > 0f)
        {
            this.whiteCamoColorAmountDrag = Mathf.InverseLerp(0.65f, 0.4f, this.star.bodyChunks[0].vel.magnitude / 5f);
            return;
        }
        this.whiteCamoColorAmountDrag = 1f;
    }

    public Color whiteCamoColor = new Color(0f, 0f, 0f);
    public Color DynamicBodyColor(float f) => Color.Lerp(new Color(0f, 0f, 0f), this.whiteCamoColor, this.whiteCamoColorAmount);

    /* public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
     {
         sLeaser.sprites = new FSprite[this.TotalSprites];
         for (int i = 0; i < this.star.bodyChunks.Length; i++)
         {
             sLeaser.sprites[this.BodySprite(i)] = new FSprite("Futile_White", true);
             sLeaser.sprites[this.BodySprite(i)].scale = (base.owner.bodyChunks[i].rad * 1.1f + 2f) / 8f;
             sLeaser.sprites[this.BodySprite(i)].shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"];
             sLeaser.sprites[this.BodySprite(i)].alpha = 0.25f;
             sLeaser.sprites[this.EyeSprite(i, 0)] = this.MakeSlitMesh();
             sLeaser.sprites[this.EyeSprite(i, 1)] = this.MakeSlitMesh();

         }
         for (int j = 0; j < this.star.tentacles.Length; j++)
         {
             this.starlegGraphics[j].InitiateSprites(sLeaser, rCam);
         }
         for (int k = 0; k < this.deadLegs.Length; k++)
         {
             this.deadLegs[k].InitiateSprites(sLeaser, rCam);
         }
         for (int l = 0; l < this.danglers.Length; l++)
         {
             this.danglers[l].InitiateSprites(sLeaser, rCam);
         }

         sLeaser.containers = new FContainer[]
         {
         new FContainer()
         };
         this.AddToContainer(sLeaser, rCam, null);
         base.InitiateSprites(sLeaser, rCam);
     }*/
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        if (this.culled)
        {
            return;
        }
        Vector2 vector = Vector2.Lerp(base.owner.bodyChunks[0].lastPos, base.owner.bodyChunks[0].pos, timeStacker) * base.owner.bodyChunks[0].mass;
        for (int i = 1; i < this.star.bodyChunks.Length; i++)
        {
            vector += Vector2.Lerp(base.owner.bodyChunks[i].lastPos, base.owner.bodyChunks[i].pos, timeStacker) * base.owner.bodyChunks[i].mass;
        }
        vector /= this.star.TotalMass;
        for (int j = 0; j < this.star.bodyChunks.Length; j++)
        {
            Vector2 vector2 = Vector2.Lerp(base.owner.bodyChunks[j].lastPos, base.owner.bodyChunks[j].pos, timeStacker) + Custom.RNV() * this.digesting * 4f * Random.value;
            if (this.star.HDmode && j < 2)
            {
                sLeaser.sprites[this.BodySprite(j)].isVisible = false;
            }
            sLeaser.sprites[this.BodySprite(j)].x = vector2.x - camPos.x;
            sLeaser.sprites[this.BodySprite(j)].y = vector2.y - camPos.y;
            sLeaser.sprites[this.BodySprite(j)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector);
            if (this.star.HDmode)
            {
                sLeaser.sprites[this.EyeSprite(j, 2)].color = Color.Lerp(this.eyes[j].renderColor, Color.black, 0.4f);
                sLeaser.sprites[this.EyeSprite(j, 2)].x = vector2.x - camPos.x;
                sLeaser.sprites[this.EyeSprite(j, 2)].y = vector2.y - camPos.y;
            }
            //this.RenderSlits(j, vector2, vector, Custom.AimFromOneVectorToAnother(vector2, vector) + this.chunksRotats[j, 0], sLeaser, rCam, timeStacker, camPos);
        }
        for (int k = 0; k < this.legGraphics.Length; k++)
        {
            this.starlegGraphics[k].DrawSprite(sLeaser, rCam, timeStacker, camPos);
        }


        this.ColorBody(sLeaser, this.DynamicBodyColor(0f));
        Color color = rCam.PixelColorAtCoordinate(this.star.mainBodyChunk.pos);
        Color color2 = rCam.PixelColorAtCoordinate(this.star.bodyChunks[0].pos);
        Color color3 = rCam.PixelColorAtCoordinate(this.star.bodyChunks[2].pos);
        if (color == color2)
        {
            this.whitePickUpColor = color;
        }
        else if (color2 == color3)
        {
            this.whitePickUpColor = color2;
        }
        else if (color3 == color)
        {
            this.whitePickUpColor = color3;
        }
        else
        {
            this.whitePickUpColor = (color + color2 + color3) / 3f;
        }
        if (this.whiteCamoColorAmount == -1f)
        {
            this.whiteCamoColor = this.whitePickUpColor;
            this.whiteCamoColorAmount = 1f;
        }

    }


    public void ColorBody(RoomCamera.SpriteLeaser sLeaser, Color col)
    {
        for (int j = 0; j < this.star.bodyChunks.Length; j++)
        {
            sLeaser.sprites[this.BodySprite(j)].color = col;
        }



    }

    public MimicstarfishGraphics.StarLegGraphic[] starlegGraphics;

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {


        this.blackColor = palette.blackColor;
        for (int i = 0; i < this.star.bodyChunks.Length; i++)
        {
            if (this.star.Template.type == CreatureTemplateType.Mimicstarfish)
            {
                sLeaser.sprites[this.BodySprite(i)].color = new Color(0f, 0f, 0f);
            }
        }
        for (int j = 0; j < this.starlegGraphics.Length; j++)
        {
            this.legGraphics[j].ApplyPalette(sLeaser, rCam, palette);
        }



    }
    public abstract class StarTubeGraphic : RopeGraphic
    {
        public StarTubeGraphic(DaddyGraphics owner, int segments, int firstSprite) : base(segments)
        {
            owne = owner;
            firstSprit = firstSprite;
        }

        public override void Update()
        {
            if (this.owne.daddy.dead)
            {
                this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 0.25f, 0.0001f);
            }
            else
            {
                if ((this.owne.daddy.State as HealthState).health < 0.6f && Random.value * 1.5f < (this.owne.daddy.State as HealthState).health && Random.value < 1f / (this.owne.daddy.Stunned ? 10f : 40f))
                {
                    whiteGlitchFit = (int)Mathf.Lerp(5f, 40f, (1f - (this.owne.daddy.State as HealthState).health) * Random.value);
                }
                if (whiteGlitchFit == 0 && this.owne.daddy.Stunned && Random.value < 0.05f)
                {
                    whiteGlitchFit = 2;
                }
                if (whiteGlitchFit > 0)
                {
                    whiteGlitchFit--;
                    float f = 1f - (this.owne.daddy.State as HealthState).health;
                    if (Random.value < 0.2f)
                    {
                        whiteCamoColorAmountDrag = 1f;
                    }
                    if (Random.value < 0.2f)
                    {
                        whiteCamoColorAmount = 1f;
                    }
                    if (Random.value < 0.5f)
                    {
                        whiteCamoColor = Color.Lerp(whiteCamoColor, new Color(Random.value, Random.value, Random.value), Mathf.Pow(f, 0.2f) * Mathf.Pow(Random.value, 0.1f));
                    }
                    if (Random.value < 0.33333334f)
                    {
                        whitePickUpColor = new Color(Random.value, Random.value, Random.value);
                    }
                }
                else if (showDominance > 0f)
                {
                    whiteDominanceHue += Random.value * Mathf.Pow(showDominance, 2f) * 0.2f;
                    if (whiteDominanceHue > 1f)
                    {
                        whiteDominanceHue -= 1f;
                    }
                    whiteCamoColor = Color.Lerp(whiteCamoColor, Custom.HSL2RGB(whiteDominanceHue, 1f, 0.5f), Mathf.InverseLerp(0.5f, 1f, Mathf.Pow(showDominance, 0.5f)) * Random.value);
                    whiteCamoColorAmount = Mathf.Lerp(whiteCamoColorAmount, 1f - Mathf.Sin(Mathf.InverseLerp(0f, 1.1f, Mathf.Pow(showDominance, 0.5f)) * 3.1415927f), 0.1f);
                }
                else
                {
                    if (Random.value < 0.1f)
                    {
                        CamoAmountControlled();
                    }
                    whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(whiteCamoColorAmount, whiteCamoColorAmountDrag, 0.1f * Random.value), 0.15f, 1f);
                    whiteCamoColor = Color.Lerp(whiteCamoColor, whitePickUpColor, 0.1f);
                }
            }
        }
        public void CamoAmountControlled()
        {
            if (!this.owne.daddy.safariControlled)
            {
                this.whiteCamoColorAmountDrag = Mathf.Lerp(this.whiteCamoColorAmountDrag, Mathf.InverseLerp(0.65f, 0.4f, this.owne.daddy.bodyChunks[0].vel.magnitude - 4f), Random.value);
                return;
            }
            if (this.owne.daddy.inputWithDiagonals == null || !this.owne.daddy.inputWithDiagonals.Value.thrw)
            {
                this.whiteCamoColorAmountDrag = 0f;
                return;
            }
            if (this.owne.daddy.bodyChunks[0].vel.magnitude > 0f)
            {
                this.whiteCamoColorAmountDrag = Mathf.InverseLerp(0.65f, 0.4f, this.owne.daddy.bodyChunks[0].vel.magnitude / 5f);
                return;
            }
            this.whiteCamoColorAmountDrag = 1f;
        }
        public override void ConnectPhase(float totalRopeLength)
        {
        }

        public override void MoveSegment(int segment, Vector2 goalPos, Vector2 smoothedGoalPos)
        {
            this.segments[segment].vel *= 0f;
            if (this.owne.daddy.room.GetTile(smoothedGoalPos).Solid && !this.owne.daddy.room.GetTile(goalPos).Solid)
            {
                FloatRect floatRect = Custom.RectCollision(smoothedGoalPos, goalPos, this.owne.daddy.room.TileRect(this.owne.daddy.room.GetTilePosition(smoothedGoalPos)).Grow(3f));
                this.segments[segment].pos = new Vector2(floatRect.left, floatRect.bottom);
                return;
            }
            this.segments[segment].pos = smoothedGoalPos;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[this.firstSprit] = TriangleMesh.MakeLongMeshAtlased(this.segments.Length, false, true);
            int num = 0;
            for (int i = 0; i < this.bump.Length; i++)
            {
                sLeaser.sprites[this.firstSprit + 1 + i] = new FSprite("Circle20", false);
                sLeaser.sprites[this.firstSprit + 1 + i].scale = Mathf.Lerp(2f, 6f, this.bump[i].size) / 10f;
                if (this.bump[i].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprit + 1 + this.bump.Length + num] = new FSprite("Circle20", false);
                    sLeaser.sprites[this.firstSprit + 1 + this.bump.Length + num].scale = Mathf.Lerp(2f, 6f, this.bump[i].size) * this.bump[i].eyeSize / 10f;
                    num++;
                }
            }

        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(this.segments[0].lastPos, this.segments[0].pos, timeStacker);
            vector += Custom.DirVec(Vector2.Lerp(this.segments[1].lastPos, this.segments[1].pos, timeStacker), vector) * 1f;
            float d = this.owne.SizeClass ? 2f : 1.7f;
            for (int i = 0; i < this.segments.Length; i++)
            {
                Vector2 vector2 = Vector2.Lerp(this.segments[i].lastPos, this.segments[i].pos, timeStacker);
                Vector2 a = Custom.PerpendicularVector((vector - vector2).normalized);
                (sLeaser.sprites[this.firstSprit] as TriangleMesh).MoveVertice(i * 4, vector - a * d - camPos);
                (sLeaser.sprites[this.firstSprit] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d - camPos);
                (sLeaser.sprites[this.firstSprit] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * d - camPos);
                (sLeaser.sprites[this.firstSprit] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * d - camPos);
                vector = vector2;
            }
            int num = 0;
            for (int j = 0; j < this.bump.Length; j++)
            {
                Vector2 vector3 = this.OnTubePos(this.bump[j].pos, timeStacker);
                sLeaser.sprites[this.firstSprit + 1 + j].x = vector3.x - camPos.x;
                sLeaser.sprites[this.firstSprit + 1 + j].y = vector3.y - camPos.y;
                if (this.bump[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprit + 1 + this.bump.Length + num].x = vector3.x - camPos.x;
                    sLeaser.sprites[this.firstSprit + 1 + this.bump.Length + num].y = vector3.y - camPos.y;
                    num++;
                }
            }

        }
        public Color DynamicBodyColor(float f)
        {

            return Color.Lerp(new Color(0f, 0f, 0f), this.whiteCamoColor, this.whiteCamoColorAmount);
        }
        public void Colorleg(RoomCamera.SpriteLeaser sLeaser, Color col)
        {
            for (int i = 0; i < (sLeaser.sprites[this.firstSprit] as TriangleMesh).vertices.Length; i++)
            {
                float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[this.firstSprit] as TriangleMesh).vertices.Length - 1));
                (sLeaser.sprites[this.firstSprit] as TriangleMesh).verticeColors[i] = Color.Lerp(col, this.owne.EffectColor, this.OnTubeEffectColorFac(floatPos));
            }
            int num = 0;
            for (int j = 0; j < this.bump.Length; j++)
            {
                sLeaser.sprites[this.firstSprit + 1 + j].color = Color.Lerp(col, this.owne.EffectColor, this.OnTubeEffectColorFac(this.bump[j].pos.y));
                if (this.bump[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprit + 1 + this.bump.Length + num].color = (col);
                    num++;
                }
            }



        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {


            Color color = whitePickUpColor;
            for (int i = 0; i < (sLeaser.sprites[this.firstSprit] as TriangleMesh).vertices.Length; i++)
            {
                float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[this.firstSprit] as TriangleMesh).vertices.Length - 1));
                (sLeaser.sprites[this.firstSprit] as TriangleMesh).verticeColors[i] = Color.Lerp(color, this.owne.EffectColor, this.OnTubeEffectColorFac(floatPos));
            }
            int num = 0;
            for (int j = 0; j < this.bump.Length; j++)
            {
                sLeaser.sprites[this.firstSprit + 1 + j].color = Color.Lerp(color, this.owne.EffectColor, this.OnTubeEffectColorFac(this.bump[j].pos.y));
                if (this.bump[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprit + 1 + this.bump.Length + num].color = (this.owne.colorClass ? this.owne.EffectColor : color);
                    num++;
                }
            }



        }
        public virtual float OnTubeEffectColorFac(float floatPos)
        {
            return Mathf.Pow(floatPos, 1.5f) * 0.4f;
        }
        public virtual float OnTubeEffectColorFa(float floatPos)
        {
            return Mathf.Pow(floatPos, 1.5f) * 0.4f;
        }

        public Vector2 OnTubePo(Vector2 pos, float timeStacker)
        {
            Vector2 p = this.OneDimensionalTubePo(pos.y - 1f / (float)this.segments.Length, timeStacker);
            Vector2 p2 = this.OneDimensionalTubePo(pos.y + 1f / (float)this.segments.Length, timeStacker);
            return this.OneDimensionalTubePo(pos.y, timeStacker) + Custom.PerpendicularVector(Custom.DirVec(p, p2)) * pos.x;
        }

        public Vector2 OneDimensionalTubePo(float floatPos, float timeStacker)
        {
            int num = Custom.IntClamp(Mathf.FloorToInt(floatPos * (float)(this.segments.Length - 1)), 0, this.segments.Length - 1);
            int num2 = Custom.IntClamp(num + 1, 0, this.segments.Length - 1);
            float t = Mathf.InverseLerp((float)num, (float)num2, floatPos * (float)(this.segments.Length - 1));
            return Vector2.Lerp(Vector2.Lerp(this.segments[num].lastPos, this.segments[num2].lastPos, t), Vector2.Lerp(this.segments[num].pos, this.segments[num2].pos, t), timeStacker);
        }
        public Vector2 OnTubePos(Vector2 pos, float timeStacker)
        {
            Vector2 p = this.OneDimensionalTubePos(pos.y - 1f / (float)this.segments.Length, timeStacker);
            Vector2 p2 = this.OneDimensionalTubePos(pos.y + 1f / (float)this.segments.Length, timeStacker);
            return this.OneDimensionalTubePos(pos.y, timeStacker) + Custom.PerpendicularVector(Custom.DirVec(p, p2)) * pos.x;
        }

        public Vector2 OneDimensionalTubePos(float floatPos, float timeStacker)
        {
            int num = Custom.IntClamp(Mathf.FloorToInt(floatPos * (float)(this.segments.Length - 1)), 0, this.segments.Length - 1);
            int num2 = Custom.IntClamp(num + 1, 0, this.segments.Length - 1);
            float t = Mathf.InverseLerp((float)num, (float)num2, floatPos * (float)(this.segments.Length - 1));
            return Vector2.Lerp(Vector2.Lerp(this.segments[num].lastPos, this.segments[num2].lastPos, t), Vector2.Lerp(this.segments[num].pos, this.segments[num2].pos, t), timeStacker);
        }

        public DaddyGraphics owne;

        public int firstSprit;

        public int sprite;

        public MimicstarfishGraphics.StarTubeGraphic.Bumpo[] bump;


        public float whiteCamoColorAmount = -1f;

        public float whiteCamoColorAmountDrag = 1f;

        public Color whiteCamoColor = new Color(0f, 0f, 0f);
        public Color whitePickUpColor;
        public float showDominance = 0;
        public float whiteDominanceHue;
        public int whiteGlitchFit;
        public struct Bumpo
        {
            public Bumpo(Vector2 pos, float size, float eyeSize)
            {
                this.pos = pos;
                this.size = size;
                this.eyeSize = eyeSize;
            }

            public Vector2 pos;

            public float size;

            public float eyeSize;
        }
    }



    public class StarLegGraphic : MimicstarfishGraphics.StarTubeGraphic
    {

        public DaddyTentacle tentacle
        {
            get
            {
                return this.owne.daddy.tentacles[this.tentacleIndex];
            }
        }

        public StarLegGraphic(DaddyGraphics owner, int tentacleIndex, int firstSprit) : base(owner, (int)(owner.daddy.tentacles[tentacleIndex].idealLength / 10f), firstSprit)
        {
            this.tentacleIndex = tentacleIndex;
            sprite = 1;
            int num = (int)(owner.daddy.tentacles[tentacleIndex].idealLength / 10f);
            bump = new MimicstarfishGraphics.StarTubeGraphic.Bumpo[num / 2 + Random.Range(5, 8)];
            for (int i = 0; i < this.bump.Length; i++)
            {
                float num2 = Mathf.Pow(Random.value, 0.3f);
                if (i == 0)
                {
                    num2 = 1f;
                }
                bump[i] = new MimicstarfishGraphics.StarTubeGraphic.Bumpo(new Vector2(Mathf.Lerp(-1f, 1f, Random.value) * 3f * num2, Mathf.Lerp(Mathf.InverseLerp(0f, (float)num, (float)(num - 20)), 1f, num2)), Mathf.Lerp(Random.value, num2, Random.value) * (owner.SizeClass ? 1f : 0.8f), (Random.value < Mathf.Lerp(0f, 0.6f, num2)) ? Mathf.Lerp(0.2f, 0.8f, Mathf.Pow(Random.value, Mathf.Lerp(1.5f, 0.5f, num2))) : 0f);
                sprite++;
                if (bump[i].eyeSize > 0f)
                {
                    sprite++;
                }
            }
        }

        public override void Update()
        {
            int listCount = 0;
            base.AddToPositionsList(listCount++, this.tentacle.FloatBase);
            for (int i = 0; i < this.tentacle.tChunks.Length; i++)
            {
                for (int j = 1; j < this.tentacle.tChunks[i].rope.TotalPositions; j++)
                {
                    base.AddToPositionsList(listCount++, this.tentacle.tChunks[i].rope.GetPosition(j) + Custom.RNV() * Mathf.InverseLerp(4f, 14f, (float)this.tentacle.stun) * 4f * Random.value);
                }
            }

            if (this.owne.daddy.dead)
            {
                this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 0.25f, 0.0001f);
            }
            else
            {
                if ((this.owne.daddy.State as HealthState).health < 0.6f && Random.value * 1.5f < (this.owne.daddy.State as HealthState).health && Random.value < 1f / (this.owne.daddy.Stunned ? 10f : 40f))
                {
                    whiteGlitchFit = (int)Mathf.Lerp(5f, 40f, (1f - (this.owne.daddy.State as HealthState).health) * Random.value);
                }
                if (whiteGlitchFit == 0 && this.owne.daddy.Stunned && Random.value < 0.05f)
                {
                    whiteGlitchFit = 2;
                }
                if (whiteGlitchFit > 0)
                {
                    whiteGlitchFit--;
                    float f = 1f - (this.owne.daddy.State as HealthState).health;
                    if (Random.value < 0.2f)
                    {
                        whiteCamoColorAmountDrag = 1f;
                    }
                    if (Random.value < 0.2f)
                    {
                        whiteCamoColorAmount = 1f;
                    }
                    if (Random.value < 0.5f)
                    {
                        whiteCamoColor = Color.Lerp(whiteCamoColor, new Color(Random.value, Random.value, Random.value), Mathf.Pow(f, 0.2f) * Mathf.Pow(Random.value, 0.1f));
                    }
                    if (Random.value < 0.33333334f)
                    {
                        whitePickUpColor = new Color(Random.value, Random.value, Random.value);
                    }
                }
                else if (showDominance > 0f)
                {
                    whiteDominanceHue += Random.value * Mathf.Pow(showDominance, 2f) * 0.2f;
                    if (whiteDominanceHue > 1f)
                    {
                        whiteDominanceHue -= 1f;
                    }
                    whiteCamoColor = Color.Lerp(whiteCamoColor, Custom.HSL2RGB(whiteDominanceHue, 1f, 0.5f), Mathf.InverseLerp(0.5f, 1f, Mathf.Pow(showDominance, 0.5f)) * Random.value);
                    whiteCamoColorAmount = Mathf.Lerp(whiteCamoColorAmount, 1f - Mathf.Sin(Mathf.InverseLerp(0f, 1.1f, Mathf.Pow(showDominance, 0.5f)) * 3.1415927f), 0.1f);
                }
                else
                {
                    if (Random.value < 0.1f)
                    {
                        CamoAmountControlled();
                    }
                    whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(whiteCamoColorAmount, whiteCamoColorAmountDrag, 0.1f * Random.value), 0.15f, 1f);
                    whiteCamoColor = Color.Lerp(whiteCamoColor, whitePickUpColor, 0.1f);
                }
            }
            base.AlignAndConnect(listCount);
        }

        public override void ConnectPhase(float totalRopeLength)
        {
        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprite(sLeaser, rCam, timeStacker, camPos);
            Colorleg(sLeaser, DynamicBodyColor(0f));
            Color color = rCam.PixelColorAtCoordinate(this.owne.daddy.mainBodyChunk.pos);
            Color color2 = rCam.PixelColorAtCoordinate(this.owne.daddy.bodyChunks[0].pos);
            Color color3 = rCam.PixelColorAtCoordinate(this.owne.daddy.bodyChunks[2].pos);
            if (color == color2)
            {
                whitePickUpColor = color;
            }
            else if (color2 == color3)
            {
                whitePickUpColor = color2;
            }
            else if (color3 == color)
            {
                whitePickUpColor = color3;
            }
            else
            {
                whitePickUpColor = (color + color2 + color3) / 3f;
            }
            if (whiteCamoColorAmount == -1f)
            {
                whiteCamoColor = whitePickUpColor;
                whiteCamoColorAmount = 1f;
            }
        }

        public int tentacleIndex;
    }

    public float whiteCamoColorAmount = -1f;

    public float whiteCamoColorAmountDrag = 1f;

    public Color whitePickUpColor;
    public float showDominance = 0;
    public float whiteDominanceHue;
    public int whiteGlitchFit;

}