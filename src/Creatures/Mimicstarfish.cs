// This code was made by ratrat (https://github.com/ratrat44) and is included in Fisobs with his permission.

using RWCustom;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace VoidTemplate;



    internal sealed class Mimicstarfish : DaddyLongLegs
    {
        public new HealthState State
        {
            get
            {
                return base.abstractCreature.state as HealthState;
            }
        }
        public Vector2 MiddleOfStar
        {
            get
            {
                Vector2 a = base.mainBodyChunk.pos;
                for (int i = 1; i < base.bodyChunks.Length; i++)
                {
                    a += base.bodyChunks[0].pos ;
                }
                return a / base.TotalMass;
            }
        }
        internal Mimicstarfish(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            this.world = world;
            
                effectColor = Custom.HSL2RGB(Custom.WrappedRandomVariation(1f, .48f, .15f), .8f, Custom.ClampedRandomVariation(.86f, .72f, .43f));
                eyeColor = new Color(.8f, 1f, 1f);
            
            if (base.abstractCreature.IsVoided())
            {
                this.effectColor = RainWorld.SaturatedGold;
                this.eyeColor = this.effectColor;
            }
            this.eatObjects = new List<DaddyLongLegs.EatObject>();
            Random.State state = Random.state;
            Random.InitState(abstractCreature.ID.RandomSeed);
            this.graphicsSeed = Random.Range(0, int.MaxValue);
            float num = this.SizeClass ? 12f : 8f;
            //int num2 = 8;
            //int num3 = 7;
            if (ModManager.MSC && abstractCreature.superSizeMe)
            {
                //num2 = 16;
                //num3 = 11;
                num = 18f;
            }
            else if (this.HDmode)
            {
                //num2 = 6;
                //num3 = 6;
                num = 4f;
            }
            base.bodyChunks = new BodyChunk[Random.Range(3, 3)];
            List<Vector2> list = new List<Vector2>();
            for (int i = 0; i < base.bodyChunks.Length; i++)
            {
                float num4 = (float)i / (float)(base.bodyChunks.Length - 1);
                float num5 = Mathf.Lerp(num * 0.2f, num * Mathf.Lerp(0.3f, 1f, num4), Mathf.Pow(Random.value, 1f - num4));
                num -= num5;
                base.bodyChunks[0] = new BodyChunk(this, i, new Vector2(0f, 0f),  23f, (this.HDmode && i < 2) ? 20f : num5); 
                base.bodyChunks[1] = new BodyChunk(this, i, new Vector2(4f, 0f), 0f, (this.HDmode && i < 2) ? 20f : num5);
                base.bodyChunks[2] = new BodyChunk(this, i, new Vector2(4f, 0f), 0f, (this.HDmode && i < 2) ? 20f : num5);
                list.Add(Custom.RNV() * base.bodyChunks[0].rad);
                list.Add(Custom.RNV() * base.bodyChunks[1].rad);
            }
            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < base.bodyChunks.Length; k++)
                {
                    for (int l = 0; l < base.bodyChunks.Length; l++)
                    {
                        if (k != l && Vector2.Distance(list[k], list[l]) < (base.bodyChunks[k].rad + base.bodyChunks[l].rad) * 0.85f)
                        {
                            List<Vector2> list2 = list;
                            int num6 = l;
                            list2[num6] -= Custom.DirVec(list[l], list[k]) * ((base.bodyChunks[k].rad + base.bodyChunks[l].rad) * 0.85f - Vector2.Distance(list[k], list[l]));
                        }
                    }
                }
                for (int m = 0; m < base.bodyChunks.Length; m++)
                {
                    List<Vector2> list2 = list;
                    int num6 = m;
                    list2[num6] *= 0.9f;
                }
            }
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[base.bodyChunks.Length * (base.bodyChunks.Length - 1) / 2];
            int num7 = 0;
            for (int n = 0; n < base.bodyChunks.Length; n++)
            {
                for (int num8 = n + 1; num8 < base.bodyChunks.Length; num8++)
                {
                    this.bodyChunkConnections[num7] = new PhysicalObject.BodyChunkConnection(base.bodyChunks[n], base.bodyChunks[num8], Vector2.Distance(list[n], list[num8]), PhysicalObject.BodyChunkConnection.Type.Normal, 1f, -1f);
                    num7++;
                }
            }
            //int num9 = 13;
            //int num10 = 10;
            float num11 = 400f;
            
            float num12;
            
                this.tentacles = new DaddyTentacle[Random.Range(5, 9)];
                num12 = Mathf.Lerp(this.SizeClass ? 3000f : 1600f, (float)this.tentacles.Length * (this.SizeClass ? num11 : 300f), 0.5f);
            
            List<float> list3 = new List<float>();
            for (int num13 = 0; num13 < this.tentacles.Length; num13++)
            {
                list3.Add(num12 / (float)this.tentacles.Length);
            }
            for (int num14 = 0; num14 < 5 * this.tentacles.Length; num14++)
            {
                int num15 = Random.Range(0, this.tentacles.Length);
                float num16 = list3[num15] * Random.value * 0.3f;
                if (list3[num15] - num16 > 100f)
                {
                    List<float> list4 = list3;
                    int num6 = Random.Range(0, this.tentacles.Length);
                    list4[num6] += num16;
                    list4 = list3;
                    num6 = num15;
                    list4[num6] -= num16;
                }
            }
            this.appendages = new List<PhysicalObject.Appendage>();
            for (int num17 = 0; num17 < this.tentacles.Length; num17++)
            {
                this.tentacles[num17] = new DaddyTentacle(this, base.bodyChunks[num17 % base.bodyChunks.Length], 350f, num17, Custom.DegToVec(Mathf.Lerp(0f, 360f, (float)num17 / (float)this.tentacles.Length)));
                this.appendages.Add(new PhysicalObject.Appendage(this, num17, this.tentacles[num17].tChunks.Length + 1));
            }
            Random.state = state;
            base.airFriction = 0.999f;
            base.gravity = 0.85f;
            this.bounce = 0.1f;
            this.surfaceFriction = 0.4f;
            this.collisionLayer = 1;
            base.waterFriction = 0.9f;
            base.buoyancy = 0.85f;
           





        }
    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        Vector2 middleOfBody = this.MiddleOfStar;
        for (int i = 0; i < this.tentacles.Length; i++)
        {
            IntVector2 tilePosition = this.room.GetTilePosition(this.tentacles[i].connectedChunk.pos);
            Vector2 a = Custom.DirVec(middleOfBody, this.tentacles[i].connectedChunk.pos);
            IntVector2 tilePosition2 = this.room.GetTilePosition(this.tentacles[i].connectedChunk.pos + a * this.tentacles[i].idealLength);
            List<IntVector2> list = new List<IntVector2>();
            this.room.RayTraceTilesList(tilePosition.x, tilePosition.y, tilePosition2.x, tilePosition2.y, ref list);
            for (int j = 1; j < list.Count; j++)
            {
                if (this.room.GetTile(list[j]).Solid)
                {
                    list.RemoveRange(j, list.Count - j);
                    break;
                }
            }
            this.tentacles[i].segments = list;
            for (int k = 0; k < this.tentacles[i].tChunks.Length; k++)
            {
                this.tentacles[i].tChunks[k].Reset();
            }
            this.tentacles[i].MoveGrabDest(this.room.MiddleOfTile(list[list.Count - 1]), ref list);
        }
        this.unconditionalSupport = 1f;
    }
    public override void Stun(int st)
        {
            for (int i = 0; i < this.tentacles.Length; i++)
            {
                if (Random.value < 0.5f || !this.SizeClass)
                {
                    this.tentacles[i].neededForLocomotion = true;
                    this.tentacles[i].SwitchTask(DaddyTentacle.Task.Locomotion);
                }
            }
            
                
                    st = 0;
                
                
            
            base.Stun(st);
        }
       public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            
            Tracker.CreatureRepresentation creatureRepresentation = this.AI.tracker.RepresentationForObject(otherObject, false);
            if (creatureRepresentation != null && this.AI.DynamicRelationship(creatureRepresentation).type == CreatureTemplate.Relationship.Type.Eats && this.CheckDaddyConsumption(otherObject))
            {
                bool flag = false;
                if (!this.SizeClass && this.digestingCounter > 0)
                {
                    return;
                }
                int num = 0;
                while (num < this.tentacles.Length && !flag)
                {
                    if (this.tentacles[num].grabChunk != null && this.tentacles[num].grabChunk.owner == otherObject)
                    {
                        flag = true;
                    }
                    num++;
                }
                int num2 = 0;
                while (num2 < this.eatObjects.Count && flag)
                {
                    if (this.eatObjects[num2].chunk.owner == otherObject)
                    {
                        flag = false;
                    }
                    num2++;
                }
                if (flag && (!base.safariControlled || (base.safariControlled && this.inputWithDiagonals != null && this.inputWithDiagonals.Value.pckp)))
                {
                    if (base.graphicsModule != null)
                    {
                        if (otherObject is IDrawable)
                        {
                            base.graphicsModule.AddObjectToInternalContainer(otherObject as IDrawable, 0);
                        }
                        else if (otherObject.graphicsModule != null)
                        {
                            base.graphicsModule.AddObjectToInternalContainer(otherObject.graphicsModule, 0);
                        }
                    }
                    this.eatObjects.Add(new DaddyLongLegs.EatObject(otherObject.bodyChunks[otherChunk], Vector2.Distance(this.MiddleOfBody, otherObject.bodyChunks[otherChunk].pos)));
                    this.room.PlaySound(SoundID.Leviathan_Crush_NPC, base.bodyChunks[myChunk]);
                }
            }
        base.Collide(otherObject, myChunk, otherChunk);
    }
        public override void InitiateGraphicsModule()
        {
            if (graphicsModule is not MimicstarfishGraphics)
            {
               graphicsModule = new MimicstarfishGraphics(this);
            }
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            
            if (!base.dead && State.health < 2.5f)
		{
			
			if (Random.value * 0.7f > this.State.health && Random.value < 0.125f)
			{
				this.Stun(Random.Range(1, Random.Range(1, 27 - Custom.IntClamp((int)(20f * this.State.health), 0, 10))));
			}
                if (this.State.health > 0f)
                {
                    this.State.health = Mathf.Min(1f, this.State.health + 0.001f);
                }
            }

            //int num = 0;
            for (int m = 0; m < this.tentacles.Length; m++)
            {
                if (ModManager.MSC)
                {
                    if ((base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] < 1f)
                    {
                        
                            (base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] += 0.001f;
                       
                        
                    }
                    if ((base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] > 1f)
                    {
                        (base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] = 1f;
                    }
                }
                
            }
        }
        
    }

