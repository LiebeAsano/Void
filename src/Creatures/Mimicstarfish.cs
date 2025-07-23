// This code was made by ratrat (https://github.com/ratrat44) and is included in Fisobs with his permission.

using RWCustom;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System;
using VoidTemplate.Useful;

namespace VoidTemplate;

public class Mimicstarfish : Creature, PhysicalObject.IHaveAppendages
{
    public new HealthState State
    {
        get
        {
            return base.abstractCreature.state as HealthState;
        }
    }

    public Vector2 MiddleOfBody
    {
        get
        {
            Vector2 a = base.bodyChunks[0].pos * base.bodyChunks[0].mass;
            for (int i = 1; i < base.bodyChunks.Length; i++)
            {
                a += base.bodyChunks[i].pos * base.bodyChunks[i].mass;
            }
            return a / base.TotalMass;
        }
    }

    public float MostDigestedEatObject
    {
        get
        {
            float num = 0f;
            for (int i = 0; i < this.eatObjects.Count; i++)
            {
                num = Mathf.Max(num, this.eatObjects[i].progression);
            }
            return num;
        }
    }

    public Mimicstarfish(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
    {
        this.world = world;


       
        this.effectColor = RainWorld.SaturatedGold;
        this.eyeColor = RainWorld.SaturatedGold;
        this.bodyColor = new Color(0.01f, 0.01f, 0.01f);
        
        this.eatObjects = new List<Mimicstarfish.EatObject>();
        Random.State state = Random.state;
        Random.InitState(abstractCreature.ID.RandomSeed);
        this.graphicsSeed = Random.Range(0, int.MaxValue);
        float num = 10f;

        base.bodyChunks = new BodyChunk[Random.Range(3, 3)];
        List<Vector2> list = new List<Vector2>();
        for (int i = 0; i < base.bodyChunks.Length; i++)
        {
            float num4 = (float)i / (float)(base.bodyChunks.Length - 1);
            float num5 = Mathf.Lerp(num * 0.2f, num * Mathf.Lerp(0.3f, 1f, num4), Mathf.Pow(Random.value, 1f - num4));
            num -= num5;
            base.bodyChunks[0] = new BodyChunk(this, i, new Vector2(0f, 0f), 23f, num5);
            base.bodyChunks[1] = new BodyChunk(this, i, new Vector2(0f, 0f), 0f, num5);
            base.bodyChunks[2] = new BodyChunk(this, i, new Vector2(0f, 0f), 0f, num5);
            list.Add(Custom.RNV() * base.bodyChunks[i].rad);
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
        int num10 = 14;
        float num12;

        this.tentacles = new StarTentacle[Random.Range(5, num10)];
        num12 = Mathf.Lerp(1600f, (float)this.tentacles.Length * (300f), 0.5f);

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
            this.tentacles[num17] = new StarTentacle(this, base.bodyChunks[num17 % base.bodyChunks.Length], 350f, num17, Custom.DegToVec(Mathf.Lerp(0f, 360f, (float)num17 * 2 / (float)this.tentacles.Length)));
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

    public override void InitiateGraphicsModule()
    {
        if (base.graphicsModule == null)
        {
            base.graphicsModule = new MimicGraphics(this);
        }
    }

    public override void NewRoom(Room newRoom)
    {
        base.NewRoom(newRoom);
        for (int i = 0; i < this.tentacles.Length; i++)
        {
            this.tentacles[i].NewRoom(newRoom);
        }
        this.pastPositions = new List<IntVector2>();
    }

    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        Vector2 middleOfBody = this.MiddleOfBody;
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

    public override void Die()
    {
        base.Die();
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.room == null)
        {
            return;
        }
        if (this.room.game.devToolsActive && Input.GetKey("b") && this.room.game.cameras[0].room == this.room)
        {
            base.bodyChunks[0].vel += Custom.DirVec(base.bodyChunks[0].pos, (Vector2)Futile.mousePosition + this.room.game.cameras[0].pos) * 14f;
            this.Stun(12);
        }
        this.unconditionalSupport = Mathf.Max(0f, this.unconditionalSupport - 0.025f);
        if (this.squeeze || this.enteringShortCut != null)
        {
            this.squeezeFac = Mathf.Min(1f, this.squeezeFac + 0.02f);
        }
        else
        {
            this.squeezeFac = Mathf.Max(0f, this.squeezeFac - 0.033333335f);
        }
        if (this.squeezeFac > 0.8f)
        {
            for (int i = 0; i < this.tentacles.Length; i++)
            {
                for (int j = 0; j < this.tentacles[i].tChunks.Length; j++)
                {
                    this.tentacles[i].tChunks[j].pos = Vector2.Lerp(this.tentacles[i].tChunks[j].pos, base.mainBodyChunk.pos, Custom.LerpMap(this.squeezeFac, 0.8f, 1f, 0f, 0.5f));
                }
            }
        }
        for (int k = 0; k < base.bodyChunks.Length; k++)
        {
            base.bodyChunks[k].terrainSqueeze = 1f - this.squeezeFac;
        }
        for (int l = 0; l < this.bodyChunkConnections.Length; l++)
        {
            this.bodyChunkConnections[l].type = (this.squeeze ? PhysicalObject.BodyChunkConnection.Type.Pull : PhysicalObject.BodyChunkConnection.Type.Normal);
        }
        this.squeeze = false;
        this.hangingInTentacle = false;
        int num = 0;
        if (!base.dead && State.health < 2.5f)
        {

            if (Random.value * 0.7f > this.State.health && Random.value < 0.125f)
            {
                this.Stun(Random.Range(1, Random.Range(1, 27 - Custom.IntClamp((int)(20f * this.State.health), 0, 10))));
            }
            if (this.State.health > 0f)
            {
                this.State.health = Mathf.Min(1f, this.State.health + 0.002f);
            }
        }
        for (int m = 0; m < this.tentacles.Length; m++)
        {
            if (ModManager.MSC)
            {
                if ((base.State as Mimicstarfish.StarState).tentacleHealth[m] < 1f)
                {

                    (base.State as Mimicstarfish.StarState).tentacleHealth[m] += 0.001f;

                }

                if ((base.State as Mimicstarfish.StarState).tentacleHealth[m] > 1f)
                {
                    (base.State as Mimicstarfish.StarState).tentacleHealth[m] = 1f;
                }
            }
            this.tentacles[m].Update();
            if (this.tentacles[m].atGrabDest)
            {
                num++;
            }
            this.tentacles[m].retractFac = this.squeezeFac;
        }
        if (this.digestingCounter > 0)
        {
            this.digestingCounter--;
            if (this.digestingCounter > 30)
            {
                this.eyesClosed = Math.Max(10, this.eyesClosed);
            }
            base.stun = Math.Max(10, base.stun);
        }
        if (this.eyesClosed > 0)
        {
            this.eyesClosed--;
        }
        this.Eat(eu);
        if (base.Consious)
        {
            this.Act(num);
            if (this.room.BackgroundNoise > 0.35f)
            {
                this.eyesClosed = Math.Max(this.eyesClosed, 15 + (int)Custom.LerpMap(this.room.BackgroundNoise, 0.35f, 1f, 15f, 100f));
                return;
            }
        }
        else
        {

            this.eyesClosed = Math.Max(this.eyesClosed, 15);
        }
    }

    public void Act(int legsGrabbing)
    {
        AI.Update();
        float num = 0.6f;
        int num2 = 3;
        float num3 = 1.2f;
        float num4 = 0.2f;

        Vector2? vector = null;
        MovementConnection movementConnection = default(MovementConnection);
        if (base.safariControlled)
        {
            stuckPos = null;

            num = 0.25f;


            MovementConnection.MovementType type = MovementConnection.MovementType.Standard;
            if (room.GetTile(base.mainBodyChunk.pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
            {
                type = MovementConnection.MovementType.ShortCut;
            }
            else
            {
                for (int i = 0; i < Custom.fourDirections.Length; i++)
                {
                    if (room.GetTile(base.mainBodyChunk.pos + Custom.fourDirections[i].ToVector2() * 20f).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                    {
                        type = MovementConnection.MovementType.BigCreatureShortCutSqueeze;
                        break;
                    }
                }
            }

            if (inputWithDiagonals.HasValue)
            {
                if (inputWithDiagonals.Value.x != 0 || inputWithDiagonals.Value.y != 0)
                {
                    bool flag = false;
                    for (int j = 0; j < tentacles.Length; j++)
                    {
                        if (tentacles[j].grabChunk != null && tentacles[j].grabChunk.owner is Creature)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!inputWithDiagonals.Value.pckp || flag)
                    {
                        vector = base.mainBodyChunk.pos + new Vector2(inputWithDiagonals.Value.x, inputWithDiagonals.Value.y) * 40f;
                        movementConnection = new MovementConnection(type, room.GetWorldCoordinate(base.mainBodyChunk.pos), room.GetWorldCoordinate(vector.Value), 2);
                    }
                    else
                    {
                        moving = false;
                    }
                }
                else
                {
                    moving = false;
                }

                if ((inputWithDiagonals.Value.thrw && !lastInputWithDiagonals.Value.thrw) || inputWithDiagonals.Value.jmp)
                {
                    for (int k = 0; k < tentacles.Length; k++)
                    {
                        tentacles[k].neededForLocomotion = true;
                        tentacles[k].SwitchTask(StarTentacle.Task.Locomotion);
                    }
                }
            }
            else
            {
                moving = false;
            }

            if (!moving)
            {
                unconditionalSupport = 1f;
                num3 = (isHD ? Mathf.InverseLerp(0f, 3f, legsGrabbing) : ((legsGrabbing <= tentacles.Length / 2) ? (0.5f + Mathf.Lerp(0f, 0.5f, legsGrabbing / (tentacles.Length / 2))) : 1f));
            }
            else if (legsGrabbing < tentacles.Length / 2)
            {
                num3 *= Mathf.Lerp(0.6f, 1f, legsGrabbing / (tentacles.Length / 2));
            }

            if (inputWithDiagonals.Value.jmp)
            {
                unconditionalSupport = 0f;
                for (int l = 0; l < tentacles.Length; l++)
                {
                    tentacles[l].neededForLocomotion = false;
                    tentacles[l].SwitchTask(StarTentacle.Task.Hunt);
                }

                num3 = -1f;
            }
        }

        if (stuckPos == null)
        {
            if (notFollowingPathToCurrentGoalCounter < 200 && AI.pathFinder.GetEffectualDestination != AI.pathFinder.GetDestination)
            {
                notFollowingPathToCurrentGoalCounter++;
            }
            else if (notFollowingPathToCurrentGoalCounter > 0)
            {
                notFollowingPathToCurrentGoalCounter--;
            }

            if (notFollowingPathToCurrentGoalCounter > 100)
            {
                for (int m = 0; m < base.bodyChunks.Length; m++)
                {
                    if (legsGrabbing != 0)
                    {
                        break;
                    }

                    if (base.bodyChunks[m].ContactPoint.x != 0 || base.bodyChunks[m].ContactPoint.y != 0)
                    {
                        legsGrabbing = 1;
                    }
                }
            }

            int num5 = 0;
            if ((Custom.ManhattanDistance(base.abstractCreature.pos, AI.pathFinder.GetEffectualDestination) > num2 || notFollowingPathToCurrentGoalCounter > 100) && legsGrabbing > 0)
            {
                pastPositions.Insert(0, base.abstractCreature.pos.Tile);
                if (pastPositions.Count > 80)
                {
                    pastPositions.RemoveAt(pastPositions.Count - 1);
                }

                for (int n = 40; n < pastPositions.Count; n++)
                {
                    if (Custom.DistLess(base.abstractCreature.pos.Tile, pastPositions[n], 4f))
                    {
                        num5++;
                    }
                }
            }

            if (num5 > 30)
            {
                stuckCounter++;
            }
            else
            {
                stuckCounter -= 2;
            }

            stuckCounter = Custom.IntClamp(stuckCounter, 0, 200);
            if (stuckCounter > 100)
            {
                for (int num6 = 0; num6 < base.bodyChunks.Length; num6++)
                {
                    base.bodyChunks[num6].vel += Custom.RNV() * 3f * UnityEngine.Random.value * Mathf.InverseLerp(100f, 200f, stuckCounter);
                }
            }

            if (base.safariControlled)
            {
                stuckCounter = 0;
            }
        }
        else
        {
            stuckCounter = 0;
        }

        if ((legsGrabbing > tentacles.Length / 2 && moving) || stuckCounter > 100)
        {
            float num7 = float.MinValue;
            int num8 = -1;
            for (int num9 = 0; num9 < tentacles.Length; num9++)
            {
                if (tentacles[num9].atGrabDest && tentacles[num9].huntCreature == null && tentacles[num9].ReleaseScore() > num7)
                {
                    num7 = tentacles[num9].ReleaseScore();
                    num8 = num9;
                }
            }

            if (num8 > -1)
            {
                List<IntVector2> path = null;
                tentacles[num8].UpdateClimbGrabPos(ref path);
            }
        }

        float num10 = 0f;
        float num11 = 0f;
        for (int num12 = 0; num12 < tentacles.Length; num12++)
        {
            float num13 = Mathf.Pow(tentacles[num12].chunksGripping, 0.5f);
            if (tentacles[num12].atGrabDest && tentacles[num12].grabDest.HasValue)
            {
                num11 += Mathf.Pow(Mathf.InverseLerp(Custom.LerpMap(stuckCounter, 0f, 100f, -0.1f, -1f), 0.85f, Vector2.Dot((tentacles[num12].floatGrabDest.Value - base.mainBodyChunk.pos).normalized, moveDirection)), 0.8f) / (float)tentacles.Length;
                num13 = Mathf.Lerp(num13, 1f, 0.75f);
            }

            num10 += num13 / (float)tentacles.Length;
        }

        num11 = Mathf.Pow(num11 * num10, Custom.LerpMap(stuckCounter, 100f, 200f, 0.8f, 0.1f));
        num10 = Mathf.Pow(num10, 0.3f);
        num11 = Mathf.Max(num11, squeezeFac);
        num10 = Mathf.Max(num10, squeezeFac);
        num10 = Mathf.Max(num10, unconditionalSupport);
        num11 = Mathf.Max(num11, unconditionalSupport);
        float num14 = 0f;
        for (int num15 = 0; num15 < tentacles.Length; num15++)
        {
            if (tentacles[num15].neededForLocomotion)
            {
                num14 += 1f / (float)tentacles.Length;
            }
        }

        if (num10 < 1f - num14)
        {
            float num16 = float.MinValue;
            int num17 = UnityEngine.Random.Range(0, tentacles.Length);
            for (int num18 = 0; num18 < tentacles.Length; num18++)
            {
                if (!tentacles[num18].neededForLocomotion)
                {
                    float num19 = 1000f / Mathf.Lerp(tentacles[num18].idealLength * (float)room.aimap.getTerrainProximity(tentacles[num18].Tip.pos), 200f, 0.8f);
                    if (tentacles[num18].task == StarTentacle.Task.Grabbing)
                    {
                        num19 *= 0.01f;
                    }

                    if (tentacles[num18].task == StarTentacle.Task.Hunt)
                    {
                        num19 *= 0.1f;
                    }



                    if (num19 > num16)
                    {
                        num16 = num19;
                        num17 = num18;
                    }
                }
            }

            tentacles[num17].neededForLocomotion = true;
        }
        else if ((double)num10 > 0.85)
        {
            tentacles[UnityEngine.Random.Range(0, tentacles.Length)].neededForLocomotion = false;
        }

        for (int num20 = 0; num20 < base.bodyChunks.Length; num20++)
        {
            base.bodyChunks[num20].vel *= Mathf.Lerp(1f, Mathf.Lerp(0.95f, 0.8f, squeezeFac), num10);
            base.bodyChunks[num20].vel.y += (base.gravity - base.buoyancy * base.bodyChunks[num20].submersion) * num10 * num3;
        }

        MovementConnection movementConnection2 = default(MovementConnection);
        if (!base.safariControlled && Custom.ManhattanDistance(base.abstractCreature.pos, AI.pathFinder.GetEffectualDestination) < num2)
        {
            for (int num21 = 0; num21 < base.bodyChunks.Length; num21++)
            {
                base.bodyChunks[num21].vel += Vector2.ClampMagnitude(room.MiddleOfTile(AI.pathFinder.GetEffectualDestination) - base.bodyChunks[0].pos, 30f) / 30f * num * num11;
            }
        }
        else if (base.safariControlled && vector.HasValue && Custom.ManhattanDistance(base.abstractCreature.pos, Custom.MakeWorldCoordinate(new IntVector2((int)vector.Value.x / 20, (int)vector.Value.y / 20), base.abstractCreature.Room.index)) < num2)
        {
            for (int num22 = 0; num22 < base.bodyChunks.Length; num22++)
            {
                base.bodyChunks[num22].vel += Vector2.ClampMagnitude(room.MiddleOfTile((int)vector.Value.x / 20, (int)vector.Value.y / 20) - base.bodyChunks[0].pos, 30f) / 30f * num * num11;
            }
        }
        else
        {
            for (int num23 = 0; num23 < base.bodyChunks.Length; num23++)
            {
                if (!(movementConnection2 == default(MovementConnection)))
                {
                    break;
                }

                for (int num24 = 0; num24 < 9; num24++)
                {
                    if (!(movementConnection2 == default(MovementConnection)))
                    {
                        break;
                    }

                    movementConnection2 = (AI.pathFinder as StandardPather).FollowPath(room.GetWorldCoordinate(base.bodyChunks[num23].pos + Custom.zeroAndEightDirectionsDiagonalsLast[num24].ToVector2() * 20f), actuallyFollowingThisPath: true);
                }
            }

            if (movementConnection2 == default(MovementConnection))
            {
                movementConnection2 = CheckTentaclesForAccessibleTerrain();
            }
        }

        if (base.safariControlled && (movementConnection2 == default(MovementConnection) || !base.AllowableControlledAIOverride(movementConnection2.type)))
        {
            movementConnection2 = movementConnection;
        }

        moving = movementConnection2 != default(MovementConnection);
        if (ModManager.MMF && movementConnection2 == default(MovementConnection))
        {
            if (!base.safariControlled)
            {
                moveDirection = (moveDirection + new Vector2(0f, 0f - num / 10f)).normalized;
            }
        }
        else if (movementConnection2 != default(MovementConnection))
        {
            if (shortcutDelay < 1)
            {
                squeeze = movementConnection2.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze;
            }

            if (shortcutDelay < 1 && movementConnection2.type == MovementConnection.MovementType.ShortCut)
            {
                enteringShortCut = movementConnection2.StartTile;
                return;
            }

            base.GoThroughFloors = movementConnection2.DestTile.y < movementConnection2.StartTile.y;
            for (int num25 = 0; num25 < base.bodyChunks.Length; num25++)
            {
                base.bodyChunks[num25].vel += Custom.DirVec(base.bodyChunks[0].pos, room.MiddleOfTile(movementConnection2.DestTile)) * num * num11;
            }

            MovementConnection movementConnection3 = movementConnection2;
            Vector2 vector2 = Custom.DirVec(movementConnection3.StartTile.ToVector2(), movementConnection3.DestTile.ToVector2());
            for (int num26 = 0; num26 < 10; num26++)
            {
                movementConnection3 = (AI.pathFinder as StandardPather).FollowPath(movementConnection3.destinationCoord, actuallyFollowingThisPath: false);
                if (movementConnection3 == default(MovementConnection))
                {
                    break;
                }

                vector2 += Custom.DirVec(movementConnection3.StartTile.ToVector2(), movementConnection3.DestTile.ToVector2());
                if (num26 < 2 && movementConnection3.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze && shortcutDelay < 1)
                {
                    squeeze = true;
                }
            }

            moveDirection = (moveDirection + vector2.normalized * num4).normalized;
            if (base.safariControlled && movementConnection != default(MovementConnection) && movementConnection.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze)
            {
                squeeze = true;
            }
        }
        else
        {
            moveDirection = (moveDirection + new Vector2(0f, 0f - num / 10f)).normalized;
        }
    }

    public MovementConnection CheckTentaclesForAccessibleTerrain()
    {
        Vector2 pos = base.mainBodyChunk.pos;
        float num = float.MaxValue;
        Vector2 vector = base.mainBodyChunk.pos;
        if (this.AI.pathFinder.GetDestination.room == base.abstractCreature.pos.room && this.AI.pathFinder.GetDestination.NodeDefined)
        {
            vector = this.room.MiddleOfTile(this.AI.pathFinder.GetDestination);
        }
        for (int i = 0; i < this.tentacles.Length; i++)
        {
            for (int j = 0; j < this.tentacles[i].tChunks.Length; j++)
            {
                if (this.room.aimap.TileAccessibleToCreature(this.tentacles[i].tChunks[j].pos, base.Template) && Custom.DistLess(this.tentacles[i].tChunks[j].pos, vector, num))
                {
                    pos = this.tentacles[i].tChunks[j].pos;
                    num = Vector2.Distance(this.tentacles[i].tChunks[j].pos, vector);
                }
            }
        }
        if (num < 3.4028235E+38f)
        {
            return new MovementConnection(MovementConnection.MovementType.Standard, this.room.GetWorldCoordinate(base.mainBodyChunk.pos), this.room.GetWorldCoordinate(pos), (int)(num / 20f));
        }
        return default(MovementConnection);
    }

    public void Eat(bool eu)
    {
        Vector2 middleOfBody = this.MiddleOfBody;
        for (int i = this.eatObjects.Count - 1; i >= 0; i--)
        {
            if (this.eatObjects[i].progression > 1f)
            {
                if (this.eatObjects[i].chunk.owner is Creature)
                {

                    this.AI.tracker.ForgetCreature((this.eatObjects[i].chunk.owner as Creature).abstractCreature);
                    Player player = this.eatObjects[i].chunk.owner as Player;
                    player?.PermaDie();
                    if (player != null && player.AreVoidViy())
                    {
                        this.Die();
                    }
                }
                this.eatObjects[i].chunk.owner.Destroy();
                this.eatObjects.RemoveAt(i);
            }
            else
            {
                this.eyesClosed = Math.Max(this.eyesClosed, 15);
                if (this.eatObjects[i].chunk.owner.collisionLayer != 0)
                {
                    this.eatObjects[i].chunk.owner.ChangeCollisionLayer(0);
                }
                if (ModManager.MMF && this.eatObjects[i].chunk.owner is Creature)
                {
                    (this.eatObjects[i].chunk.owner as Creature).enteringShortCut = default(IntVector2?);
                }
                float progression = this.eatObjects[i].progression;
                this.eatObjects[i].progression += 0.0125f;
                if (progression <= 0.5f && this.eatObjects[i].progression > 0.5f)
                {
                    if (this.eatObjects[i].chunk.owner is Creature)
                    {
                        (this.eatObjects[i].chunk.owner as Creature).Die();
                    }
                    for (int j = 0; j < this.eatObjects[i].chunk.owner.bodyChunkConnections.Length; j++)
                    {
                        this.eatObjects[i].chunk.owner.bodyChunkConnections[j].type = PhysicalObject.BodyChunkConnection.Type.Pull;
                    }
                }
                float d = this.eatObjects[i].distance * (1f - this.eatObjects[i].progression);
                this.eatObjects[i].chunk.vel *= 0f;
                this.eatObjects[i].chunk.MoveFromOutsideMyUpdate(eu, middleOfBody + Custom.DirVec(middleOfBody, this.eatObjects[i].chunk.pos) * d);
                for (int k = 0; k < this.eatObjects[i].chunk.owner.bodyChunks.Length; k++)
                {
                    this.eatObjects[i].chunk.owner.bodyChunks[k].vel *= 1f - this.eatObjects[i].progression;
                    this.eatObjects[i].chunk.owner.bodyChunks[k].MoveFromOutsideMyUpdate(eu, Vector2.Lerp(this.eatObjects[i].chunk.owner.bodyChunks[k].pos, middleOfBody + Custom.DirVec(middleOfBody, this.eatObjects[i].chunk.owner.bodyChunks[k].pos) * d, this.eatObjects[i].progression));
                }
                if (this.eatObjects[i].chunk.owner.graphicsModule != null && this.eatObjects[i].chunk.owner.graphicsModule.bodyParts != null)
                {
                    for (int l = 0; l < this.eatObjects[i].chunk.owner.graphicsModule.bodyParts.Length; l++)
                    {
                        this.eatObjects[i].chunk.owner.graphicsModule.bodyParts[l].vel *= 1f - this.eatObjects[i].progression;
                        this.eatObjects[i].chunk.owner.graphicsModule.bodyParts[l].pos = Vector2.Lerp(this.eatObjects[i].chunk.owner.graphicsModule.bodyParts[l].pos, middleOfBody, this.eatObjects[i].progression);
                    }
                }
            }
        }
    }

    public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        base.Collide(otherObject, myChunk, otherChunk);
        Tracker.CreatureRepresentation creatureRepresentation = this.AI.tracker.RepresentationForObject(otherObject, false);
        if (creatureRepresentation != null && this.AI.DynamicRelationship(creatureRepresentation).type == CreatureTemplate.Relationship.Type.Eats && this.CheckDaddyConsumption(otherObject))
        {
            bool flag = false;
            if (this.digestingCounter > 0)
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
                this.eatObjects.Add(new Mimicstarfish.EatObject(otherObject.bodyChunks[otherChunk], Vector2.Distance(this.MiddleOfBody, otherObject.bodyChunks[otherChunk].pos)));
                this.room.PlaySound(SoundID.Leviathan_Crush_NPC, base.bodyChunks[myChunk]);
            }
        }
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        this.room.PlaySound((speed < 8f) ? SoundID.Cicada_Light_Terrain_Impact : SoundID.Cicada_Heavy_Terrain_Impact, base.mainBodyChunk);
    }

    public override void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (hitAppendage != null)
        {
            damage /= (2f);
            stunBonus /= (1.2f);
            (base.State as Mimicstarfish.StarState).tentacleHealth[hitAppendage.appendage.appIndex] -= damage;
            this.tentacles[hitAppendage.appendage.appIndex].stun = Math.Max(this.tentacles[hitAppendage.appendage.appIndex].stun, (int)(damage * 48f + stunBonus));
            damage = 0f;
            stunBonus = 0f;
        }

        damage /= ((ModManager.MSC && base.abstractCreature.superSizeMe) ? 4f : 1f);
        base.Violence(source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }

    public override void Stun(int st)
    {
        for (int i = 0; i < this.tentacles.Length; i++)
        {

            this.tentacles[i].neededForLocomotion = true;
            this.tentacles[i].SwitchTask(StarTentacle.Task.Locomotion);

        }

        st /= 3;


        base.Stun(st);
    }

    public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);
        this.shortcutDelay = 80;
        Vector2 a = Custom.IntVector2ToVector2(newRoom.ShorcutEntranceHoleDirection(pos));
        for (int i = 0; i < base.bodyChunks.Length; i++)
        {
            base.bodyChunks[i].pos = newRoom.MiddleOfTile(pos) + Custom.RNV();
            base.bodyChunks[i].lastPos = base.bodyChunks[i].pos;
            base.bodyChunks[i].vel = a * 4f;
        }
        this.squeezeFac = 1f;
        if (base.graphicsModule != null)
        {
            base.graphicsModule.Reset();
        }
        for (int j = 0; j < this.tentacles.Length; j++)
        {
            this.tentacles[j].Reset(this.tentacles[j].connectedChunk.pos);
        }
    }

    public Vector2 AppendagePosition(int appendage, int segment)
    {
        segment--;
        if (segment < 0)
        {
            return this.tentacles[appendage].connectedChunk.pos;
        }
        return this.tentacles[appendage].tChunks[segment].pos;
    }

    public void ApplyForceOnAppendage(PhysicalObject.Appendage.Pos pos, Vector2 momentum)
    {
        if (pos.prevSegment > 0)
        {
            this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment - 1].pos += momentum / 0.04f * (1f - pos.distanceToNext);
            this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment - 1].vel += momentum / 0.04f * (1f - pos.distanceToNext);
        }
        else
        {
            this.tentacles[pos.appendage.appIndex].connectedChunk.pos += momentum / this.tentacles[pos.appendage.appIndex].connectedChunk.mass * (1f - pos.distanceToNext);
            this.tentacles[pos.appendage.appIndex].connectedChunk.vel += momentum / this.tentacles[pos.appendage.appIndex].connectedChunk.mass * (1f - pos.distanceToNext);
        }
        this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment].pos += momentum / 0.04f * pos.distanceToNext;
        this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment].vel += momentum / 0.04f * pos.distanceToNext;
    }

    public bool CheckDaddyConsumption(PhysicalObject otherObject)
    {
        bool result = false;
        if (otherObject != null)
        {
            if (otherObject is Mimicstarfish)
            {

                result = true;


            }
            else
            {
                result = (otherObject.TotalMass < 5f);
            }
        }
        return result;
    }

    public override Color ShortCutColor()
    {
        return this.effectColor;
    }

    public MimicAI AI;

    public bool hangingInTentacle;

    public StarTentacle[] tentacles;

    public Vector2 moveDirection = new Vector2(0f, -1f);

    public bool tentaclesHoldOn;

    public bool moving;

    public List<IntVector2> pastPositions;

    public int stuckCounter;

    public bool squeeze;

    public float squeezeFac;

    public int eyesClosed;

    public Color effectColor;

    public Color eyeColor;

    public Color bodyColor;

    public bool colorClass;

    public int digestingCounter;

    public int graphicsSeed;

    public int notFollowingPathToCurrentGoalCounter;

    public float unconditionalSupport;

    public PlacedObject stuckPos;

    public List<Mimicstarfish.EatObject> eatObjects;

    public bool isHD;

    public World world;

    public class EatObject
    {
        public EatObject(BodyChunk chunk, float distance)
        {
            this.chunk = chunk;
            this.distance = distance;
            this.progression = 0f;
        }

        public BodyChunk chunk;

        public float distance;

        public float progression;
    }
    public override float VisibilityBonus
    {
        get
        {
            if (base.graphicsModule != null)
            {
                return -(base.graphicsModule as MimicGraphics).Camouflaged;
            }
            return 0f;
        }
    }
    public class StarState : HealthState
    {
        public StarState(AbstractCreature creature) : base(creature)
        {
            this.tentacleHealth = new float[13];
            for (int i = 0; i < this.tentacleHealth.Length; i++)
            {
                this.tentacleHealth[i] = 1f;
            }
        }


        public float[] tentacleHealth;
    }

}