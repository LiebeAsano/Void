// This code was made by ratrat (https://github.com/ratrat44) and is included in Fisobs with his permission.

using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Noise;

namespace VoidTemplate;

public class LWMimicGraphics : GraphicsModule
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

    public int LegSprite(int leg)
    {
        return leg;
    }

    public int BodySprite(int chunk)
    {
        return this.totalLegSprites + chunk;
    }

    public int EyeSprite(int eye, int part)
    {
        return this.totalLegSprites + this.star.bodyChunks.Length + eye * (2) + part;
    }

    public int TotalSprites
    {
        get
        {
            return this.totalLegSprites + this.star.bodyChunks.Length * (3);
        }
    }

    public LWMimicstarfish star
    {
        get
        {
            return base.owner as LWMimicstarfish;
        }
    }

    public bool colorClass
    {
        get
        {
            return this.star.colorClass;
        }
    }

    public Color EffectColor
    {
        get
        {
            return this.star.effectColor;
        }
    }

    public LWMimicGraphics(PhysicalObject ow) : base(ow, false)
    {
        this.cullRange = 1400f;
        Random.State state = Random.state;
        Random.InitState(this.star.graphicsSeed);
        int num = 0;
        int num2 = 0;
        this.legGraphics = new LWMimicGraphics.StarLegGraphic[this.star.tentacles.Length];
        for (int i = 0; i < this.legGraphics.Length; i++)
        {
            this.legGraphics[i] = new LWMimicGraphics.StarLegGraphic(this, i, num2);
            num2 += this.legGraphics[i].sprites;
        }
        this.totalLegSprites = num2;
        List<int> list = new List<int>();


        bool flag = Random.value < 0.5f;
        this.chunksRotats = new float[this.star.bodyChunks.Length, 2];
        this.eyes = new LWMimicGraphics.Eye[this.star.bodyChunks.Length];
        for (int m = 0; m < this.star.bodyChunks.Length; m++)
        {
            this.chunksRotats[m, 0] = Random.value * 360f;
            this.chunksRotats[m, 1] = Random.value;
            this.eyes[m] = new LWMimicGraphics.Eye(this, m);
        }
        Random.state = state;

        this.bodyParts = new BodyPart[num];

        num = 0;

        this.internalContainerObjects = new List<GraphicsModule.ObjectHeldInInternalContainer>();
        this.digestLoop = new StaticSoundLoop(SoundID.Daddy_Digestion_LOOP, base.owner.firstChunk.pos, base.owner.room, 0f, 1f);
    }


    public override void Update()
    {
        base.Update();
        if (!this.culled)
        {
            for (int i = 0; i < this.legGraphics.Length; i++)
            {
                this.legGraphics[i].Update();
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
            this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 0.25f, 0.01f);
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
        if (!changeVisibleState && (((isVisible) && star.AI.runSpeed <= .3f) || ((!isVisible) && star.AI.runSpeed >= .3f)))
        {
            changeVisibleState = true;
            isVisible = !isVisible;
        }
    }
    public void CamoAmountControlled()
    {
        if (!this.star.safariControlled)
        {
            this.whiteCamoColorAmountDrag = Mathf.Lerp(this.whiteCamoColorAmountDrag, Mathf.InverseLerp(0.65f, 0.4f, this.star.AI.runSpeed), Random.value);
            return;
        }
        if (this.star.inputWithDiagonals == null || !this.star.inputWithDiagonals.Value.thrw)
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
    public Color DynamicBodyColor(float f)
    {

        return Color.Lerp(star.bodyColor, (whiteCamoColorAmount <= .2f) ? star.bodyColor : this.whiteCamoColor, this.whiteCamoColorAmount);
    }
    public override void Reset()
    {
        base.Reset();
        for (int i = 0; i < this.legGraphics.Length; i++)
        {
            this.legGraphics[i].Reset(this.star.mainBodyChunk.pos);
        }

    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[this.TotalSprites];
        for (int i = 0; i < this.star.bodyChunks.Length; i++)
        {
            sLeaser.sprites[this.BodySprite(i)] = new FSprite("Futile_White", true)
            {
                scale = (base.owner.bodyChunks[i].rad * 1.1f + 2f) / 8f,
                shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"],
                alpha = 0.25f
            };
            sLeaser.sprites[this.EyeSprite(i, 0)] = this.MakeSlitMesh();
            sLeaser.sprites[this.EyeSprite(i, 1)] = this.MakeSlitMesh();

        }
        for (int j = 0; j < this.star.tentacles.Length; j++)
        {
            this.legGraphics[j].InitiateSprites(sLeaser, rCam);
        }

        sLeaser.containers = new FContainer[]
        {
            new FContainer()
        };
        this.AddToContainer(sLeaser, rCam, null);
        base.InitiateSprites(sLeaser, rCam);
    }

    public void ColorBody(RoomCamera.SpriteLeaser sLeaser, Color col)
    {
        for (int j = 0; j < this.star.bodyChunks.Length; j++)
        {
            sLeaser.sprites[this.BodySprite(j)].color = col;
        }



    }

    public LWMimicGraphics.StarLegGraphic[] starlegGraphics;
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        sLeaser.RemoveAllSpritesFromContainer();
        if (newContatiner == null)
        {
            newContatiner = rCam.ReturnFContainer("Midground");
        }
        if (sLeaser.containers != null)
        {
            foreach (FContainer node in sLeaser.containers)
            {
                newContatiner.AddChild(node);
            }
        }
        for (int j = 0; j < sLeaser.sprites.Length; j++)
        {
            newContatiner.AddChild(sLeaser.sprites[j]);
        }

    }

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

            sLeaser.sprites[this.BodySprite(j)].x = vector2.x - camPos.x;
            sLeaser.sprites[this.BodySprite(j)].y = vector2.y - camPos.y;
            sLeaser.sprites[this.BodySprite(j)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector) + this.chunksRotats[j, 0];

            this.RenderSlits(j, vector2, vector, Custom.AimFromOneVectorToAnother(vector2, vector) + this.chunksRotats[j, 0], sLeaser, rCam, timeStacker, camPos);
        }
        for (int k = 0; k < this.legGraphics.Length; k++)
        {
            this.legGraphics[k].DrawSprite(sLeaser, rCam, timeStacker, camPos);
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
        whiteCamoColor = whitePickUpColor;
        if (changeVisibleState)
            {
                if (isVisible)
                {
                    if (invisAmount >= 90)
                    {
                    for (int i = 0; i < this.star.bodyChunks.Length; i++)
                    {
                        sLeaser.sprites[this.BodySprite(i)].isVisible = true;

                    }
                    }
                    if (invisAmount > 0)
                    {
                        invisAmount--;
                    }
                    else
                    {
                        changeVisibleState = false;
                    }
                }
                else
                {
                    if (invisAmount < 90)
                    {
                        invisAmount++;
                    }
                    else
                    {
                        changeVisibleState = false;
                    }
                }

                
            }
            whiteCamoColorAmount = Mathf.InverseLerp(0f, 90f, invisAmount);
        if (!isVisible && whiteCamoColorAmount == 1)
        {
            for (int i = 0; i < this.star.bodyChunks.Length; i++)
            {
                sLeaser.sprites[this.BodySprite(i)].isVisible = false;
            }
        }
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
        this.blackColor = palette.blackColor;
        for (int i = 0; i < this.star.bodyChunks.Length; i++)
        {

            sLeaser.sprites[this.BodySprite(i)].color = star.bodyColor;

        }
        for (int j = 0; j < this.legGraphics.Length; j++)
        {
            this.legGraphics[j].ApplyPalette(sLeaser, rCam, palette);
        }

    }

    public TriangleMesh MakeSlitMesh()
    {
        TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[8];
        for (int i = 0; i < 8; i++)
        {
            array[i] = new TriangleMesh.Triangle(i, i + 1, i + 2);
        }
        return new TriangleMesh("Futile_White", array, false, false);
    }

    public void RenderSlits(int chunk, Vector2 pos, Vector2 middleOfBody, float rotation, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {

        float rad = this.star.bodyChunks[chunk].rad;
        float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.eyes[chunk].lastClosed, this.eyes[chunk].closed, timeStacker)), 0.6f);
        float num2 = (0.9f) * (1f - num);
        Vector2 b = Vector2.Lerp(this.eyes[chunk].lastDir, this.eyes[chunk].dir, timeStacker);
        float num3 = Mathf.Lerp(this.eyes[chunk].lastFocus, this.eyes[chunk].focus, timeStacker) * Mathf.Pow(Mathf.InverseLerp(-1f, 1f, Vector2.Dot(Custom.DirVec(middleOfBody, pos), b.normalized)), 0.7f);
        num3 = Mathf.Max(num3, num);
        float num4 = Mathf.InverseLerp(0f, Mathf.Lerp(30f, 50f, this.chunksRotats[chunk, 1]), Vector2.Distance(middleOfBody, pos + Custom.DirVec(middleOfBody, pos) * rad)) * 0.9f;
        num4 = Mathf.Lerp(num4, 1f, 0.5f * num3);
        Vector2 vector = Vector2.Lerp(Custom.DirVec(middleOfBody, pos) * num4, b, b.magnitude * 0.5f);
        this.eyes[chunk].centerRenderPos = pos + vector * rad;
        this.eyes[chunk].renderColor = Color.Lerp(this.star.eyeColor, new Color(1f, 1f, 1f), Mathf.Lerp(UnityEngine.Random.value * this.eyes[chunk].light, 1f, num));
        if (num > 0f)
        {
            this.eyes[chunk].renderColor = Color.Lerp(this.eyes[chunk].renderColor, this.blackColor, num);
        }
        this.eyes[chunk].renderColor = Color.Lerp(this.eyes[chunk].renderColor, Color.white, this.eyes[chunk].flash);
        sLeaser.sprites[this.EyeSprite(chunk, 0)].color = this.eyes[chunk].renderColor;
        sLeaser.sprites[this.EyeSprite(chunk, 1)].color = this.eyes[chunk].renderColor;
        for (int i = 0; i < 2; i++)
        {
            Vector2 vector2 = Custom.DegToVec(rotation + 90f * (float)i);
            Vector2 a = Custom.PerpendicularVector(vector2);
            (sLeaser.sprites[this.EyeSprite(chunk, i)] as TriangleMesh).MoveVertice(0, pos + this.BulgeVertex(vector2 * rad * 0.9f * Mathf.Lerp(1f, 0.6f, num3), vector, rad) - camPos);
            (sLeaser.sprites[this.EyeSprite(chunk, i)] as TriangleMesh).MoveVertice(9, pos + this.BulgeVertex(vector2 * -rad * 0.9f * Mathf.Lerp(1f, 0.6f, num3), vector, rad) - camPos);
            for (int j = 1; j < 5; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    float d = rad * ((j < 3) ? 0.7f : 0.25f) * ((k == 0) ? 1f : -1f) * Mathf.Lerp(1f, 0.6f, num3);
                    int num5 = (k == 0) ? j : (9 - j);
                    float d2 = num2 * ((j < 3) ? 0.5f : 1f) * ((num5 % 2 == 0) ? 1f : -1f) * Mathf.Lerp(1f, 2.5f, num3);
                    (sLeaser.sprites[this.EyeSprite(chunk, i)] as TriangleMesh).MoveVertice(num5, pos + this.BulgeVertex(vector2 * d + a * d2, vector, rad) - camPos);
                }
            }
        }
    }

    public Vector2 BulgeVertex(Vector2 v, Vector2 dir, float rad)
    {
        return Vector2.Lerp(v, Vector2.ClampMagnitude(v + dir * rad, rad), dir.magnitude);
    }

    public void ReactToNoise(NoiseTracker.TheorizedSource source, InGameNoise noise)
    {
        return;
    }

    public void FeelSomethingWithTentacle(Tracker.CreatureRepresentation creatureRep, Vector2 feelPos)
    {
        if (this.feelSomethingReactionDelay > 0)
        {
            return;
        }
        this.feelSomethingReactionDelay = Random.Range(10, 40);
        Vector2 middleOfBody = this.star.MiddleOfBody;
        float num = float.MinValue;
        int num2 = -1;
        for (int i = 0; i < this.eyes.Length; i++)
        {
            float num3 = Vector2.Dot(Custom.DirVec(middleOfBody, this.star.bodyChunks[i].pos), Custom.DirVec(middleOfBody, feelPos));
            if (this.eyes[i].soundSource != null)
            {
                num3 -= 1f;
            }
            if (this.eyes[i].creatureRep != null)
            {
                num3 -= 2f;
            }
            if (num3 > num)
            {
                num = num3;
                num2 = i;
            }
        }
        this.eyes[num2].ReactToCreature(creatureRep);
        base.owner.room.PlaySound(SoundID.Daddy_React_To_Tentacle_Touch, middleOfBody);
    }

    public LWMimicGraphics.StarLegGraphic[] legGraphics;

    public float[,] chunksRotats;

    public int totalLegSprites;

    public Color blackColor;

    public float digesting;

    public int reactionSoundDelay;

    public int feelSomethingReactionDelay;

    public LWMimicGraphics.Eye[] eyes;

    public StaticSoundLoop digestLoop;

    public Color whiteCamoColor = Custom.RGB2RGBA(new Color(0f, 0f, 0f), 1f);

    public int invisAmount;

    public bool changeVisibleState;

    public bool isVisible = true;

    public interface DaddyBubbleOwner
    {

        Color GetColor();

        Vector2 GetPosition();
    }

    public class Eye : LWMimicGraphics.DaddyBubbleOwner
    {
        public BodyChunk chunk
        {
            get
            {
                return this.owner.star.bodyChunks[this.index];
            }
        }

        public Eye(LWMimicGraphics owner, int index)
        {
            this.index = index;
            this.owner = owner;
            this.dir = new Vector2(0f, 0f);
            this.lastDir = new Vector2(0f, 0f);
            this.centerRenderPos = owner.star.bodyChunks[index].pos;
        }

        public void Update()
        {
            this.lastDir = this.dir;
            this.lastClosed = this.closed;
            this.closed = Mathf.Max(Mathf.Lerp(this.closed, Mathf.InverseLerp(0f, (float)this.eyesClosedDelay, (float)this.owner.star.eyesClosed), 1f / (float)this.eyesClosedDelay), this.owner.star.Deaf);
            if (this.owner.star.eyesClosed == 0)
            {
                this.eyesClosedDelay = Random.Range(1, 20);
            }
            if (this.owner.star.dead)
            {
                this.eyesClosedDelay = Mathf.Min(this.eyesClosedDelay + 2, 15);
            }
            Vector2 vector = new Vector2(0f, 0f);
            if (this.soundSource != null)
            {
                vector = this.soundSource.pos;
                if (this.soundSource.slatedForDeletion)
                {
                    this.soundSource = null;
                }
            }
            else if (this.creatureRep != null)
            {
                if (this.creatureRep.VisualContact)
                {
                    vector = this.creatureRep.representedCreature.realizedCreature.DangerPos;
                }
                else
                {
                    vector = this.owner.star.room.MiddleOfTile(this.creatureRep.BestGuessForPosition());
                }
                if (this.creatureRep.deleteMeNextFrame)
                {
                    this.creatureRep = null;
                }
            }
            float num = 0f;
            if (vector.x != 0f && vector.y != 0f)
            {
                this.dir = Vector3.Slerp(this.dir, Custom.DirVec(this.chunk.pos, vector) * Mathf.InverseLerp(0f, 200f, Vector2.Distance(this.chunk.pos, vector)), 0.3f);
                num = this.light * Mathf.InverseLerp(0f, 1f, Vector2.Distance(this.lastDir, this.dir));
                this.light = Mathf.Max(this.owner.star.dead ? 0f : 0.2f, this.light - 0.05f);
            }
            else
            {
                this.dir *= 0.9f;
                this.light = Mathf.Max(this.owner.star.dead ? 0f : 0.1f, this.light - 0.05f);
                this.FindNewLookObject();
            }
            this.flash = Mathf.Max(0f, this.flash - 0.16666667f);
            if (Random.value < num)
            {
                this.getToFocus = Mathf.Max(this.getToFocus, Random.value);
            }
            else if (Random.value < 0.014285714f)
            {
                this.getToFocus = 0f;
            }
            this.lastFocus = this.focus;
            if (this.focus < this.getToFocus)
            {
                this.focus = Mathf.Min(this.focus + 0.05f, this.getToFocus);
                return;
            }
            this.focus = Mathf.Max(this.focus - 0.05f, this.getToFocus);
        }

        public void FindNewLookObject()
        {
            bool flag = false;
            if (this.owner.star.AI.tracker.CreaturesCount > 0)
            {
                flag = true;
                Tracker.CreatureRepresentation rep = this.owner.star.AI.tracker.GetRep(Random.Range(0, this.owner.star.AI.tracker.CreaturesCount));
                int num = 0;
                while (num < this.owner.eyes.Length && flag)
                {
                    if (this.owner.eyes[num].creatureRep == rep)
                    {
                        flag = false;
                    }
                    num++;
                }
                if (flag)
                {
                    this.creatureRep = rep;
                    this.light = Mathf.Max(0.75f, this.light);
                }
            }
        }

        public void ReactToSound(NoiseTracker.TheorizedSource newSound)
        {
            this.creatureRep = null;
            this.soundSource = newSound;
            this.light = 1f;
            this.flash = 1f;
        }

        public void ReactToCreature(Tracker.CreatureRepresentation newCrit)
        {
            this.soundSource = null;
            this.creatureRep = newCrit;
            this.light = Mathf.Max(this.light, Random.value);
        }

        public Color GetColor()
        {
            return this.renderColor;
        }

        public Vector2 GetPosition()
        {
            return this.centerRenderPos;
        }

        public int index;

        public Vector2 dir;

        public Vector2 lastDir;

        public float focus;

        public float lastFocus;

        public float getToFocus;

        public float closed;

        public float lastClosed;

        public int eyesClosedDelay;

        public LWMimicGraphics owner;

        public NoiseTracker.TheorizedSource soundSource;

        public Tracker.CreatureRepresentation creatureRep;

        public float light;

        public float flash;

        public Vector2 centerRenderPos;

        public Color renderColor;
    }

    public abstract class StarTubeGraphic : RopeGraphic
    {

        public StarTubeGraphic(LWMimicGraphics owner, int segments, int firstSprite) : base(segments)
        {
            this.owner = owner;
            this.firstSprite = firstSprite;
        }

        public override void Update()
        {
            if (this.owner.star.dead)
            {
                this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 0.25f, 0.01f);
            }
            else
            {
                if ((this.owner.star.State as HealthState).health < 0.6f && Random.value * 1.5f < (this.owner.star.State as HealthState).health && Random.value < 1f / (this.owner.star.Stunned ? 10f : 40f))
                {
                    whiteGlitchFit = (int)Mathf.Lerp(5f, 40f, (1f - (this.owner.star.State as HealthState).health) * Random.value);
                }
                if (whiteGlitchFit == 0 && this.owner.star.Stunned && Random.value < 0.05f)
                {
                    whiteGlitchFit = 2;
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
            if (!this.owner.star.safariControlled)
            {
                this.whiteCamoColorAmountDrag = Mathf.Lerp(this.whiteCamoColorAmountDrag, Mathf.InverseLerp(0.65f, 0.4f, this.owner.star.AI.runSpeed), this.owner.star.bodyChunks[0].vel.magnitude / 5);
                return;
            }
            if (this.owner.star.inputWithDiagonals == null || !this.owner.star.inputWithDiagonals.Value.thrw)
            {
                this.whiteCamoColorAmountDrag = 0f;
                return;
            }
            if (this.owner.star.bodyChunks[0].vel.magnitude > 0f)
            {
                this.whiteCamoColorAmountDrag = Mathf.InverseLerp(0.65f, 0.4f, this.owner.star.bodyChunks[0].vel.magnitude / 5);
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
            if (this.owner.star.room.GetTile(smoothedGoalPos).Solid && !this.owner.star.room.GetTile(goalPos).Solid)
            {
                FloatRect floatRect = Custom.RectCollision(smoothedGoalPos, goalPos, this.owner.star.room.TileRect(this.owner.star.room.GetTilePosition(smoothedGoalPos)).Grow(3f));
                this.segments[segment].pos = new Vector2(floatRect.left, floatRect.bottom);
                return;
            }
            this.segments[segment].pos = smoothedGoalPos;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[this.firstSprite] = TriangleMesh.MakeLongMeshAtlased(this.segments.Length, false, true);
            int num = 0;
            for (int i = 0; i < this.bumps.Length; i++)
            {
                sLeaser.sprites[this.firstSprite + 1 + i] = new FSprite("Circle20", false);
                sLeaser.sprites[this.firstSprite + 1 + i].scale = Mathf.Lerp(2f, 6f, this.bumps[i].size) / 10f;
                if (this.bumps[i].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num] = new FSprite("Circle20", false);
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].scale = Mathf.Lerp(2f, 6f, this.bumps[i].size) * this.bumps[i].eyeSize / 10f;
                    num++;
                }
            }
        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(this.segments[0].lastPos, this.segments[0].pos, timeStacker);
            vector += Custom.DirVec(Vector2.Lerp(this.segments[1].lastPos, this.segments[1].pos, timeStacker), vector) * 1f;
            float d = 2.7f;
            for (int i = 0; i < this.segments.Length; i++)
            {
                Vector2 vector2 = Vector2.Lerp(this.segments[i].lastPos, this.segments[i].pos, timeStacker);
                Vector2 a = Custom.PerpendicularVector((vector - vector2).normalized);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4, vector - a * d - camPos);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d - camPos);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * d - camPos);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * d - camPos);
                vector = vector2;
            }
            int num = 0;
            for (int j = 0; j < this.bumps.Length; j++)
            {
                Vector2 vector3 = this.OnTubePos(this.bumps[j].pos, timeStacker);
                sLeaser.sprites[this.firstSprite + 1 + j].x = vector3.x - camPos.x;
                sLeaser.sprites[this.firstSprite + 1 + j].y = vector3.y - camPos.y;
                if (this.bumps[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].x = vector3.x - camPos.x;
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].y = vector3.y - camPos.y;
                    num++;
                }
            }
        }

        public Color DynamicBodyColor(float f)
        {

            return Color.Lerp(owner.star.bodyColor, (whiteCamoColorAmount <= .2f) ? owner.star.bodyColor : this.whiteCamoColor, this.whiteCamoColorAmount);
        }
        public Color DynamicEffectColorColor(float f)
        {

            return Color.Lerp(owner.star.abstractCreature.IsVoided() ? RainWorld.SaturatedGold : this.owner.EffectColor, (whiteCamoColorAmount <= .2f) ? owner.EffectColor : this.whiteCamoColor, this.whiteCamoColorAmount);
        }
        public void Colorleg(RoomCamera.SpriteLeaser sLeaser, Color col, Color col2)
        {

            for (int i = 0; i < (sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length; i++)
            {
                float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length - 1));
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(col, col2, this.OnTubeEffectColorFac(floatPos));
            }
            int num = 0;
            for (int j = 0; j < this.bumps.Length; j++)
            {
                sLeaser.sprites[this.firstSprite + 1 + j].color = Color.Lerp(col, col2, this.OnTubeEffectColorFac(this.bumps[j].pos.y));
                if (this.bumps[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].color = (col);
                    num++;
                }
            }



        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            Color color = palette.blackColor;

            for (int i = 0; i < (sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length; i++)
            {
                float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length - 1));
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(color, this.owner.EffectColor, this.OnTubeEffectColorFac(floatPos));
            }
            int num = 0;
            for (int j = 0; j < this.bumps.Length; j++)
            {
                sLeaser.sprites[this.firstSprite + 1 + j].color = Color.Lerp(color, this.owner.EffectColor, this.OnTubeEffectColorFac(this.bumps[j].pos.y));
                if (this.bumps[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].color = (this.owner.colorClass ? this.owner.EffectColor : color);
                    num++;
                }
            }
        }

        public virtual float OnTubeEffectColorFac(float floatPos)
        {
            return Mathf.Pow(floatPos, 1.5f) * 0.4f;
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

        public LWMimicGraphics owner;

        public int firstSprite;

        public int sprites;

        public float whiteCamoColorAmount = -1f;

        public float whiteCamoColorAmountDrag = 1f;

        public Color whiteCamoColor = Custom.RGB2RGBA(new Color(0f, 0f, 0f), 1f);
        public Color whitePickUpColor;
        public float showDominance;
        public float whiteDominanceHue;
        public int whiteGlitchFit;
        public LWMimicGraphics.StarTubeGraphic.Bump[] bumps;
        public int invisAmount;
        public bool changeVisibleState;
        public bool isVisible = true;

        public struct Bump
        {

            public Bump(Vector2 pos, float size, float eyeSize)
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

    public class StarLegGraphic : LWMimicGraphics.StarTubeGraphic
    {

        public StarTentacle tentacle
        {
            get
            {
                return this.owner.star.tentacles[this.tentacleIndex];
            }
        }

        public StarLegGraphic(LWMimicGraphics owner, int tentacleIndex, int firstSprite) : base(owner, (int)(owner.star.tentacles[tentacleIndex].idealLength / 10f), firstSprite)
        {
            this.tentacleIndex = tentacleIndex;
            this.sprites = 1;
            int num = (int)(owner.star.tentacles[tentacleIndex].idealLength / 10f);
            this.bumps = new LWMimicGraphics.StarTubeGraphic.Bump[num / 2 + Random.Range(5, 8)];
            for (int i = 0; i < this.bumps.Length; i++)
            {
                float num2 = Mathf.Pow(Random.value, 0.3f);
                if (i == 0)
                {
                    num2 = 1f;
                }
                this.bumps[i] = new LWMimicGraphics.StarTubeGraphic.Bump(new Vector2(Mathf.Lerp(-1f, 1f, Random.value) * 3f * num2, Mathf.Lerp(Mathf.InverseLerp(0f, (float)num, (float)(num - 20)), 1f, num2)), Mathf.Lerp(Random.value, num2, Random.value) * (1f), (Random.value < Mathf.Lerp(0f, 0.6f, num2)) ? Mathf.Lerp(0.2f, 0.8f, Mathf.Pow(Random.value, Mathf.Lerp(1.5f, 0.5f, num2))) : 0f);
                this.sprites++;
                if (this.bumps[i].eyeSize > 0f)
                {
                    this.sprites++;
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
            if (this.owner.star.dead)
            {
                this.whiteCamoColorAmount = Mathf.Lerp(this.whiteCamoColorAmount, 0.25f, 0.01f);
            }
            else
            {
                if ((this.owner.star.State as HealthState).health < 0.6f && Random.value * 1.5f < (this.owner.star.State as HealthState).health && Random.value < 1f / (this.owner.star.Stunned ? 10f : 40f))
                {
                    whiteGlitchFit = (int)Mathf.Lerp(5f, 40f, (1f - (this.owner.star.State as HealthState).health) * Random.value);
                }
                if (whiteGlitchFit == 0 && this.owner.star.Stunned && Random.value < 0.05f)
                {
                    whiteGlitchFit = 2;
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
            if (!changeVisibleState && (((isVisible) && owner.star.AI.runSpeed <= .3f) || ((!isVisible) && owner.star.AI.runSpeed >= .3f)))
            {
                changeVisibleState = true;
                isVisible = !isVisible;
            }
        }

        public override void ConnectPhase(float totalRopeLength)
        {
        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprite(sLeaser, rCam, timeStacker, camPos);
            int num = 0;
            Colorleg(sLeaser, DynamicBodyColor(0f), DynamicEffectColorColor(0f));
            Color color = rCam.PixelColorAtCoordinate(this.owner.star.mainBodyChunk.pos);
            Color color2 = rCam.PixelColorAtCoordinate(this.owner.star.bodyChunks[0].pos);
            Color color3 = rCam.PixelColorAtCoordinate(this.owner.star.bodyChunks[2].pos);
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
            whiteCamoColor = whitePickUpColor;
            if (changeVisibleState)
            {
                if (isVisible)
                {
                    if (invisAmount >= 90)
                    {
                        sLeaser.sprites[this.firstSprite].isVisible = true;
                        for (int i = 0; i < this.bumps.Length; i++)
                        {
                            sLeaser.sprites[this.firstSprite + 1 + i].isVisible = true;
                            if (this.bumps[i].eyeSize > 0f)
                            {
                                sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].isVisible = true;
                                num++;
                            }
                        }
                    }
                    if (invisAmount > 0)
                    {
                        invisAmount--;
                    }
                    else
                    {
                        changeVisibleState = false;
                    }
                }
                else
                {
                    if (invisAmount < 90)
                    {
                        invisAmount++;
                    }
                    else
                    {
                        changeVisibleState = false;
                    }
                }


            }
            whiteCamoColorAmount = Mathf.InverseLerp(0f, 90f, invisAmount);
            if (!isVisible && whiteCamoColorAmount == 1)
            {
                sLeaser.sprites[this.firstSprite].isVisible = false;
                for (int i = 0; i < this.bumps.Length; i++)
                {
                    sLeaser.sprites[this.firstSprite + 1 + i].isVisible = false;
                    if (this.bumps[i].eyeSize > 0f)
                    {
                        sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].isVisible = false;
                        num++;
                    }
                }
            }
        }

        public int tentacleIndex;
    }


    public float whiteCamoColorAmount = -1f;

    public float whiteCamoColorAmountDrag = 1f;

    public Color whitePickUpColor;
    public float showDominance;
    public float whiteDominanceHue;
    public int whiteGlitchFit;


}
