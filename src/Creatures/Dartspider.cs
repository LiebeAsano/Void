using RWCustom;
using UnityEngine;

namespace VoidTemplate;

   
public class Dartspider : BigSpider
{
    public Dartspider(AbstractCreature abstractCreature, World world)
        : base(abstractCreature, world)
    {
        spitter = abstractCreature.creatureTemplate.type == CreatureTemplateType.Dartspider;
        spewBabies = false;
        mother = ModManager.DLCShared && abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.MotherSpider;
        float num = (spitter ? 1.4f : 0.8f);
        if (mother)
        {
            num = 2f;
        }

        base.bodyChunks = new BodyChunk[2];
        base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, num * (1f / 3f));
        base.bodyChunks[1] = new BodyChunk(this, 1, new Vector2(0f, 0f), 9f, num * (2f / 3f));
        bodyChunkConnections = new BodyChunkConnection[1];
        bodyChunkConnections[0] = new BodyChunkConnection(base.bodyChunks[0], base.bodyChunks[1], spitter ? 25f : 15f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
        grabChunks = new BodyChunk[2, 4];
        base.airFriction = 0.999f;
        base.gravity = 0.9f;
        bounce = 0.1f;
        surfaceFriction = 0.4f;
        collisionLayer = 1;
        base.waterFriction = 0.96f;
        base.buoyancy = 0.95f;
        Random.State state = Random.state;
        Random.InitState(abstractCreature.ID.RandomSeed);
        yellowCol = Color.Lerp(new Color(0f, .8f, .5f), Custom.HSL2RGB(Random.value, Random.value, Random.value), Random.value * 0.2f);

        Random.state = state;
        deathConvulsions = (State.alive ? 1f : 0f);
    }
    public override void InitiateGraphicsModule()
    {
        base.InitiateGraphicsModule();
        if (base.graphicsModule is not DartSpiGraphics)
        {
            base.graphicsModule = new DartSpiGraphics(this);
        }
    }
    public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        base.Collide(otherObject, myChunk, otherChunk);
        if (!base.Consious)
        {
            return;
        }
        else if (otherObject is Creature)
        {
            this.AI.tracker.SeeCreature((otherObject as Creature).abstractCreature);
            for (int i = 0; i < this.AI.relationshipTracker.relationships.Count; i++)
            {
                if (this.AI.relationshipTracker.relationships[i].trackerRep.representedCreature == (otherObject as Creature).abstractCreature)
                {
                    (this.AI.relationshipTracker.relationships[i].state as BigSpiderAI.SpiderTrackState).accustomed += 10;
                    break;
                }
            }
            bool flag = false;
            bool consious = (otherObject as Creature).Consious;
            if (myChunk == 0 && base.grasps[0] == null)
            {
                bool flag2 = ((!this.spitter && this.mandiblesCharged > 0.8f && this.canBite > 0) || (this.spitter && Random.value < 0.25f && !consious)) && Vector2.Dot(Custom.DirVec(base.bodyChunks[1].pos, base.mainBodyChunk.pos), Custom.DirVec(base.mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos)) > 0f && this.AI.DynamicRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats;
                if (ModManager.MMF)
                {
                    flag2 = (flag2 && this.AI.preyTracker.TotalTrackedPrey > 0 && this.AI.preyTracker.Utility() > 0f && this.AI.preyTracker.MostAttractivePrey.representedCreature == (otherObject as Creature).abstractCreature);
                }
                if (base.safariControlled)
                {
                    flag2 = (this.inputWithDiagonals != null && this.inputWithDiagonals.Value.pckp && Vector2.Dot(Custom.DirVec(base.bodyChunks[1].pos, base.mainBodyChunk.pos), Custom.DirVec(base.mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos)) > 0f && this.AI.DynamicRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats);
                }
                if (flag2)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        this.room.AddObject(new WaterDrip(Vector2.Lerp(base.mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos, Random.value), Custom.RNV() * Random.value * 14f, false));
                    }
                    if (base.safariControlled || Random.value < Custom.LerpMap(otherObject.TotalMass, 0.84f, this.spitter ? 5.5f : 3f, 0.5f, 0.15f, 0.12f) || !consious)
                    {
                        if (this.Grab(otherObject, 0, otherChunk, Creature.Grasp.Shareability.CanNotShare, 0.5f, false, true))
                        {
                            flag = true;
                            this.room.PlaySound(SoundID.Big_Spider_Grab_Creature, base.mainBodyChunk);
                        }
                        else
                        {
                            this.room.PlaySound(SoundID.Big_Spider_Slash_Creature, base.mainBodyChunk);
                        }
                    }
                    else
                    {
                        this.room.PlaySound(SoundID.Big_Spider_Slash_Creature, base.mainBodyChunk);
                    }
                    this.canBite = 0;
                    (otherObject as Creature).Violence(base.mainBodyChunk, new Vector2?(Custom.DirVec(base.mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos) * (this.spitter ? 8f : 6f)), otherObject.bodyChunks[otherChunk], null, Creature.DamageType.Bite,((Random.value < 0.5f) ? 1.2f : 0.8f), 20f);
                    this.ReleaseAllGrabChunks();
                }
                else if (this.AI.StaticRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats && ((otherObject as Creature).Template.CreatureRelationship(base.Template).type != CreatureTemplate.Relationship.Type.Eats || Random.value < 0.1f) && this.LegsGrabby)
                {
                    IntVector2 intVector2 = new IntVector2(Random.Range(0, 2), Random.Range(0, Random.Range(2, 4)));
                    this.grabChunks[intVector2.x, intVector2.y] = otherObject.bodyChunks[otherChunk];
                    flag = true;
                }
            }
            if (!flag && this.revivingBuddy != null && this.jumpStamina > 0.2f && (this.AI.DynamicRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats || this.AI.DynamicRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Afraid))
            {
                Vector2 vector = (base.bodyChunks[0].pos + base.bodyChunks[1].pos) / 2f;
                Vector2 vector2 = Custom.DirVec(vector, otherObject.bodyChunks[otherChunk].pos);
                base.bodyChunks[0].pos = vector + vector2 * this.bodyChunkConnections[0].distance * 0.5f;
                base.bodyChunks[1].pos = vector - vector2 * this.bodyChunkConnections[0].distance * 0.5f;
                this.Jump(vector2, 0.8f);
                this.canBite = 40;
                this.mandiblesCharged = 1f;
                this.ReleaseAllGrabChunks();
                this.revivingBuddy = null;
                this.jumpStamina = Mathf.Max(0f, this.jumpStamina - 0.5f);
                flag = true;
                Custom.Log(new string[]
                {
                    "revive fend off"
                });
            }
            if (!flag && !base.safariControlled && (this.spitter || this.mandiblesCharged <= 0.8f || this.canBite <= 0) && (this.spitter || this.jumpStamina > 0.15f) && base.grasps[0] == null && consious && (otherObject as Creature).Template.CreatureRelationship(base.Template).intensity > 0f && (otherObject as Creature).TotalMass > base.TotalMass * 0.2f)
            {
                for (int k = 0; k < this.grabChunks.GetLength(0); k++)
                {
                    for (int l = 0; l < this.grabChunks.GetLength(1); l++)
                    {
                        if (this.grabChunks[k, l] != null && this.grabChunks[k, l].owner == otherObject)
                        {
                            return;
                        }
                    }
                }
                    this.Jump(Custom.DirVec(otherObject.bodyChunks[otherChunk].pos, base.mainBodyChunk.pos + new Vector2(0f, 10f)), 0.1f + 0.4f * this.bounceSoundVol);
                    this.bounceSoundVol = Mathf.Max(0f, this.bounceSoundVol - 0.2f);
                    otherObject.bodyChunks[otherChunk].vel -= Vector2.ClampMagnitude(Custom.DirVec(otherObject.bodyChunks[otherChunk].pos, base.mainBodyChunk.pos + new Vector2(0f, 10f)) * 8f * base.TotalMass / otherObject.bodyChunks[otherChunk].mass, 15f);
                    BodyChunk bodyChunk3 = base.bodyChunks[0];
                    bodyChunk3.pos.y = bodyChunk3.pos.y + 20f;
                    BodyChunk bodyChunk4 = base.bodyChunks[1];
                    bodyChunk4.pos.y = bodyChunk4.pos.y + 10f;
                    this.jumpStamina = Mathf.Max(0f, this.jumpStamina - 0.15f);
                
                if ((otherObject as Creature).Consious && (otherObject as Creature).Template.CreatureRelationship(base.Template).type == CreatureTemplate.Relationship.Type.Eats)
                {
                    this.AI.stayAway = true;
                }
            }
        }
    }
    
}

