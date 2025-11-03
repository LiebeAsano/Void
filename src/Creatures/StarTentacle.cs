using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoidTemplate;

    public class StarTentacle : Tentacle
    {

        public LWMimicstarfish star
        {
            get
            {
                return this.owner as LWMimicstarfish;
            }
        }

        public void SwitchTask(StarTentacle.Task newTask)
        {
            if (newTask != StarTentacle.Task.Hunt)
            {
                this.huntCreature = null;
            }
            if (newTask != StarTentacle.Task.ExamineSound)
            {
                this.checkSound = null;
                this.examineSoundPos = default(Vector2?);
            }
            if (newTask != StarTentacle.Task.Grabbing && this.grabChunk != null)
            {
                this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Creature, this.grabChunk.pos);
                this.grabChunk = null;
            }
            if (newTask == StarTentacle.Task.Locomotion && this.task != StarTentacle.Task.Locomotion)
            {
                List<IntVector2> list = null;
                this.UpdateClimbGrabPos(ref list);
            }
            if (newTask == StarTentacle.Task.ExamineSound)
            {
                this.soundCheckTimer = 0;
                this.soundCheckCounter = 0;
            }
            this.task = newTask;
        }

        public StarTentacle(LWMimicstarfish star, BodyChunk chunk, float length, int tentacleNumber, Vector2 tentacleDir) : base(star, chunk, length)
        {
            this.tentacleNumber = tentacleNumber;
            this.tentacleDir = tentacleDir;
            this.tProps = new Tentacle.TentacleProps(false, true, false, 0.5f, 0f, 0f, 0f, 0f, 3.2f, 10f, 0.25f, 5f, 15, 60, 12, 20);
            if (ModManager.MMF)
            {
                this.tChunks = new Tentacle.TentacleChunk[Math.Max(3, (int)(length / 40f))];
            }
            else
            {
                this.tChunks = new Tentacle.TentacleChunk[(int)(length / 40f)];
            }
            for (int i = 0; i < this.tChunks.Length; i++)
            {
                this.tChunks[i] = new Tentacle.TentacleChunk(this, i, (float)(i + 1) / (float)this.tChunks.Length, 3f);
            }
            this.chunksStickSounds = new int[this.tChunks.Length];
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);
            this.SwitchTask(StarTentacle.Task.Locomotion);
        }

        public override void Update()
        {
            base.Update();
            if (this.grabChunk != null && (this.grabChunk.owner.room == null || this.grabChunk.owner.room != this.star.room))
            {
                this.stun = 10;
                this.grabChunk = null;
            }
            if (this.star.dead)
            {
                this.neededForLocomotion = true;
                this.grabChunk = null;
                this.limp = true;
            }
            if (this.stun > 0)
            {
                this.stun--;
                this.grabChunk = null;
            }
            if (Mathf.Pow(Random.value, 0.35f) > (this.star.State as LWMimicstarfish.StarState).tentacleHealth[this.tentacleNumber])
            {
                this.stun = Math.Max(this.stun, (int)Mathf.Lerp(-4f, 14f, Mathf.Pow(Random.value, 0.5f + 20f * Mathf.Max(0f, (this.star.State as LWMimicstarfish.StarState).tentacleHealth[this.tentacleNumber]))));
            }
            if (this.grabChunk != null)
            {
                float num = Vector2.Distance(base.Tip.pos, this.grabChunk.pos);
                float num2 = (base.Tip.rad + this.grabChunk.rad) / 4f;
                Vector2 a = Custom.DirVec(base.Tip.pos, this.grabChunk.pos);
                float num3 = this.grabChunk.mass / (this.grabChunk.mass + 0.01f);
                float d = 1f;
                base.Tip.pos += a * (num - num2) * num3 * d;
                base.Tip.vel += a * (num - num2) * num3 * d;
                this.grabChunk.pos -= a * (num - num2) * (1f - num3) * d;
                this.grabChunk.vel -= a * (num - num2) * (1f - num3) * d;
                if (this.grabChunk.owner is Player && Random.value < Mathf.Lerp(0f, 1f / (15f), (this.grabChunk.owner as Player).GraspWiggle))
                {
                    this.stun = Math.Max(this.stun, Random.Range(1, 10));
                    this.grabChunk = null;
                }
            }
            this.limp = (!this.star.Consious || this.stun > 0);
            for (int i = 0; i < this.tChunks.Length; i++)
            {
                this.tChunks[i].vel *= 0.9f;
                if (this.limp)
                {
                    Tentacle.TentacleChunk tentacleChunk = this.tChunks[i];
                    tentacleChunk.vel.y = tentacleChunk.vel.y - 0.5f;
                }
                if (this.stun > 0 && !this.star.dead)
                {
                    this.tChunks[i].vel += Custom.RNV() * 10f;
                }
            }
            if (this.limp)
            {
                for (int j = 0; j < this.tChunks.Length; j++)
                {
                    Tentacle.TentacleChunk tentacleChunk2 = this.tChunks[j];
                    tentacleChunk2.vel.y = tentacleChunk2.vel.y - 0.7f;
                }
                return;
            }
            this.atGrabDest = false;
            if (this.backtrackFrom > -1)
            {
                this.secondaryGrabBackTrackCounter++;
                if (!this.lastBackTrack)
                {
                    this.secondaryGrabBackTrackCounter += 20;
                }
            }
            this.lastBackTrack = (this.backtrackFrom > -1);
            Vector2 vector = this.star.mainBodyChunk.pos;
            for (int k = 1; k < this.star.bodyChunks.Length; k++)
            {
                vector += this.star.bodyChunks[k].pos;
            }
            vector /= (float)this.star.bodyChunks.Length;
            this.awayFromBodyRotation = Custom.AimFromOneVectorToAnother(vector, this.connectedChunk.pos);
            this.chunksGripping = 0f;
            if (!this.neededForLocomotion)
            {
                bool flag = !this.star.safariControlled || (this.star.inputWithDiagonals != null && this.star.inputWithDiagonals.Value.pckp);
                if (this.task != StarTentacle.Task.Grabbing && flag)
                {
                    this.LookForCreaturesToHunt();
                    if (this.huntCreature == null && this.checkSound == null)
                    {
                        this.LookForSoundsToExamine();
                    }
                }
            }
            else if (this.task != StarTentacle.Task.Locomotion)
            {
                this.SwitchTask(StarTentacle.Task.Locomotion);
            }
            if (this.task == StarTentacle.Task.Hunt && (this.huntCreature == null || this.huntCreature.deleteMeNextFrame))
            {
                this.SwitchTask(StarTentacle.Task.Locomotion);
            }
            else if (this.task != StarTentacle.Task.Hunt && this.huntCreature != null)
            {
                this.huntCreature = null;
            }
            if (this.task == StarTentacle.Task.ExamineSound && (this.checkSound == null || this.checkSound.slatedForDeletion))
            {
                this.SwitchTask(StarTentacle.Task.Locomotion);
            }
            else if (this.task != StarTentacle.Task.ExamineSound && this.checkSound != null)
            {
                this.checkSound = null;
            }
            if (this.task == StarTentacle.Task.Grabbing && (this.grabChunk == null || this.grabChunk.owner.room != this.room || (ModManager.MMF && !this.star.Consious)))
            {
                this.SwitchTask(StarTentacle.Task.Locomotion);
            }
            else if (this.task != StarTentacle.Task.Grabbing && this.grabChunk != null)
            {
                this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Creature, this.grabChunk.pos);
                this.grabChunk = null;
            }
            if (this.task == StarTentacle.Task.Locomotion)
            {
                this.Climb(ref this.scratchPath);
            }
            else if (this.task == StarTentacle.Task.Hunt)
            {
                this.Hunt(ref this.scratchPath);
            }
            else if (this.task == StarTentacle.Task.ExamineSound)
            {
                this.ExamineSound(ref this.scratchPath);
            }
            else if (this.task == StarTentacle.Task.Grabbing)
            {
                base.MoveGrabDest(vector + Custom.DirVec(vector, this.grabChunk.pos) * 20f, ref this.scratchPath);
                Vector2 p = vector;
                bool flag2 = this.room.VisualContact(this.grabChunk.pos, vector);
                for (int l = this.tChunks.Length - 1; l >= 0; l--)
                {
                    Vector2 p2 = base.FloatBase;
                    if (l > 0)
                    {
                        p2 = this.tChunks[l - 1].pos;
                        if (!flag2 && !this.room.VisualContact(this.grabChunk.pos, this.tChunks[l - 1].pos))
                        {
                            p = this.tChunks[l].pos;
                            flag2 = true;
                        }
                    }
                    this.tChunks[l].vel += Custom.DirVec(this.tChunks[l].pos, p2) * 1.2f;
                    if (this.tChunks[l].phase > -1f || this.room.GetTile(this.tChunks[l].pos).Solid)
                    {
                        this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Creature, this.grabChunk.pos);
                        this.grabChunk = null;
                        this.SwitchTask(StarTentacle.Task.Locomotion);
                        break;
                    }
                }
                if (this.task == StarTentacle.Task.Grabbing)
                {
                    this.grabChunk.vel += (Vector2)Vector3.Slerp(Custom.DirVec(this.grabChunk.pos, p), Custom.DirVec(base.Tip.pos, this.tChunks[this.tChunks.Length - 2].pos), 0.5f) * Custom.LerpMap((float)this.grabPath.Count, 3f, 18f, 0.65f, 0.25f) * (0.45f) / this.grabChunk.mass;
                }
            }
            for (int m = 0; m < this.tChunks.Length; m++)
            {
                float num4 = (float)m / (float)(this.tChunks.Length - 1);
                if (num4 < 0.2f)
                {
                    this.tChunks[m].vel += Custom.DegToVec(this.awayFromBodyRotation) * Mathf.InverseLerp(0.2f, 0f, num4) * 5f;
                }
                for (int n = m + 1; n < this.tChunks.Length; n++)
                {
                    base.PushChunksApart(m, n);
                }
            }
            this.Touch();
        }

        public void Touch()
        {
            bool flag = false;
            bool flag2 = !this.star.safariControlled || (this.star.inputWithDiagonals != null && this.star.inputWithDiagonals.Value.pckp);
            for (int i = 0; i < this.room.abstractRoom.creatures.Count; i++)
            {
                if (this.room.abstractRoom.creatures[i].realizedCreature != null && !this.room.abstractRoom.creatures[i].realizedCreature.inShortcut && this.room.abstractRoom.creatures[i].realizedCreature != this.star && !this.room.abstractRoom.creatures[i].tentacleImmune && flag2)
                {
                    Creature realizedCreature = this.room.abstractRoom.creatures[i].realizedCreature;
                    for (int j = 0; j < this.tChunks.Length; j++)
                    {
                        int k = 0;
                        while (k < realizedCreature.bodyChunks.Length)
                        {
                            if (Custom.DistLess(this.tChunks[j].pos, realizedCreature.bodyChunks[k].pos, this.tChunks[j].rad + realizedCreature.bodyChunks[k].rad))
                            {
                                if (this.star.eyesClosed < 1 || Random.value < 0.05f)
                                {
                                    this.star.AI.tracker.SeeCreature(realizedCreature.abstractCreature);
                                    if (this.star.graphicsModule != null)
                                    {
                                        Tracker.CreatureRepresentation creatureRep = this.star.AI.tracker.RepresentationForObject(realizedCreature, false);
                                        (this.star.graphicsModule as LWMimicGraphics).FeelSomethingWithTentacle(creatureRep, this.tChunks[j].pos);
                                    }
                                }
                                if (realizedCreature.abstractCreature.creatureTemplate.AI && realizedCreature.abstractCreature.abstractAI.RealAI != null && realizedCreature.abstractCreature.abstractAI.RealAI.tracker != null)
                                {
                                    realizedCreature.abstractCreature.abstractAI.RealAI.tracker.SeeCreature(this.star.abstractCreature);
                                }
                                this.CollideWithCreature(j, realizedCreature.bodyChunks[k]);
                                if (!this.neededForLocomotion && realizedCreature.newToRoomInvinsibility < 1 && this.grabChunk == null && j == this.tChunks.Length - 1 && (this.star.digestingCounter < 1) && (this.star.eyesClosed < 1 || Random.value < (/*this.star.SizeClass ? 0.5f :*/ 0.15f)) && (this.task == StarTentacle.Task.Hunt || !this.IsCreatureCaughtEnough(realizedCreature.abstractCreature)))
                                {
                                    flag = true;
                                    if (Vector2.Distance(this.tChunks[j].vel, realizedCreature.bodyChunks[k].vel) >= Mathf.Lerp(1f, 8f, this.sticky))
                                    {
                                        break;
                                    }
                                    bool flag3 = false;
                                    if (this.star.AI.tracker.RepresentationForObject(realizedCreature, false) != null && this.star.AI.DynamicRelationship(this.star.AI.tracker.RepresentationForObject(realizedCreature, false)).type == CreatureTemplate.Relationship.Type.Eats)
                                    {
                                        flag3 = true;
                                    }
                                    int num = 0;
                                    while (num < this.tChunks.Length && flag3)
                                    {
                                        if (this.tChunks[num].phase > -1f || this.room.GetTile(this.tChunks[num].pos).Solid)
                                        {
                                            flag3 = false;
                                        }
                                        num++;
                                    }
                                    if (flag3)
                                    {
                                        this.grabChunk = realizedCreature.bodyChunks[k];
                                        this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Creature, this.tChunks[j].pos, 1f, 1f);
                                        this.SwitchTask(StarTentacle.Task.Grabbing);
                                        return;
                                    }
                                    break;
                                }
                                else
                                {
                                    if (this.neededForLocomotion || (!(this.task == StarTentacle.Task.Locomotion) && !(this.task == StarTentacle.Task.ExamineSound)) || this.IsCreatureCaughtEnough(realizedCreature.abstractCreature))
                                    {
                                        break;
                                    }
                                    Tracker.CreatureRepresentation creatureRepresentation = this.star.AI.tracker.RepresentationForObject(realizedCreature, false);
                                    if (creatureRepresentation == null || !(this.star.AI.DynamicRelationship(creatureRepresentation).type == CreatureTemplate.Relationship.Type.Eats))
                                    {
                                        break;
                                    }
                                    bool flag4 = false;
                                    int num2 = 0;
                                    while (num2 < this.star.tentacles.Length && !flag4)
                                    {
                                        if (this.star.tentacles[num2].huntCreature == creatureRepresentation)
                                        {
                                            flag4 = true;
                                        }
                                        num2++;
                                    }
                                    if (!flag4)
                                    {
                                        this.huntCreature = creatureRepresentation;
                                        if (this.checkSound != null)
                                        {
                                            this.checkSound.Destroy();
                                            this.checkSound = null;
                                        }
                                        this.SwitchTask(StarTentacle.Task.Hunt);
                                        break;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                k++;
                            }
                        }
                    }
                }
            }
            if (flag)
            {
                this.sticky = Mathf.Min(1f, this.sticky + 0.033333335f);
                return;
            }
            this.sticky = Mathf.Max(0f, this.sticky - 0.016666668f);
        }

        public void CollideWithCreature(int tChunk, BodyChunk creatureChunk)
        {
            if (this.backtrackFrom > -1 && this.backtrackFrom <= tChunk)
            {
                return;
            }
            float num = Vector2.Distance(this.tChunks[tChunk].pos, creatureChunk.pos);
            float num2 = (this.tChunks[tChunk].rad + creatureChunk.rad) / 4f;
            Vector2 a = Custom.DirVec(this.tChunks[tChunk].pos, creatureChunk.pos);
            float num3 = creatureChunk.mass / (creatureChunk.mass + 0.01f);
            float d = 0.8f;
            this.tChunks[tChunk].pos += a * (num - num2) * num3 * d;
            this.tChunks[tChunk].vel += a * (num - num2) * num3 * d;
            creatureChunk.pos -= a * (num - num2) * (1f - num3) * d;
            creatureChunk.vel -= a * (num - num2) * (1f - num3) * d;
        }

        public void LookForCreaturesToHunt()
        {
            if (this.neededForLocomotion || this.star.AI.preyTracker.TotalTrackedPrey == 0)
            {
                return;
            }
            Tracker.CreatureRepresentation creatureRepresentation = this.star.AI.preyTracker.GetTrackedPrey(Random.Range(0, this.star.AI.preyTracker.TotalTrackedPrey));
            if (this.star.safariControlled)
            {
                if (this.huntDirection == Vector2.zero)
                {
                    this.huntDirection = Custom.RNV() * 80f;
                }
                if (this.star.inputWithDiagonals != null && this.star.inputWithDiagonals.Value.AnyDirectionalInput)
                {
                    this.huntDirection = new Vector2((float)this.star.inputWithDiagonals.Value.x, (float)this.star.inputWithDiagonals.Value.y) * 80f;
                }
                Creature creature = null;
                float num = float.MaxValue;
                float current = Custom.VecToDeg(this.huntDirection);
                for (int i = 0; i < this.star.room.abstractRoom.creatures.Count; i++)
                {
                    if (this.star.abstractCreature != this.star.room.abstractRoom.creatures[i] && this.star.room.abstractRoom.creatures[i].realizedCreature != null)
                    {
                        float target = Custom.AimFromOneVectorToAnother(this.star.mainBodyChunk.pos, this.star.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos);
                        float num2 = Custom.Dist(this.star.mainBodyChunk.pos, this.star.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos);
                        if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < 22.5f && num2 < num)
                        {
                            num = num2;
                            creature = this.star.room.abstractRoom.creatures[i].realizedCreature;
                        }
                    }
                }
                if (creature != null)
                {
                    creatureRepresentation = this.star.AI.tracker.RepresentationForCreature(creature.abstractCreature, true);
                }
            }
            for (int j = 0; j < this.star.tentacles.Length; j++)
            {
                if (this.star.tentacles[j].huntCreature == creatureRepresentation)
                {
                    return;
                }
            }
            if (this.IsCreatureCaughtEnough(creatureRepresentation.representedCreature))
            {
                return;
            }
            if (creatureRepresentation.BestGuessForPosition().room != this.star.abstractCreature.pos.room)
            {
                return;
            }
            if (Vector2.Distance(this.room.MiddleOfTile(creatureRepresentation.BestGuessForPosition()), base.FloatBase) > this.idealLength + 40f)
            {
                return;
            }
            if (this.checkSound != null)
            {
                this.checkSound.Destroy();
                this.checkSound = null;
            }
            this.huntCreature = creatureRepresentation;
            this.SwitchTask(StarTentacle.Task.Hunt);
        }

        public bool IsCreatureCaughtEnough(AbstractCreature crit)
        {
            int num = 0;
            for (int i = 0; i < this.star.tentacles.Length; i++)
            {
                if (this.star.tentacles[i].grabChunk != null && this.star.tentacles[i].grabChunk.owner is Creature && (this.star.tentacles[i].grabChunk.owner as Creature).abstractCreature == crit)
                {
                    num++;
                }
            }
            return (float)num >= crit.creatureTemplate.bodySize * (2.5f);
        }

        public void LookForSoundsToExamine()
        {

            return;

        }

        public void Hunt(ref List<IntVector2> path)
        {
            if (this.huntCreature.BestGuessForPosition().room != this.star.abstractCreature.pos.room || this.huntCreature.deleteMeNextFrame)
            {
                this.SwitchTask(StarTentacle.Task.Locomotion);
                return;
            }
            if (this.huntCreature.VisualContact)
            {
                base.MoveGrabDest(this.huntCreature.representedCreature.realizedCreature.mainBodyChunk.pos, ref path);
            }
            else
            {
                if (this.huntCreature.BestGuessForPosition().TileDefined)
                {
                    base.MoveGrabDest(this.room.MiddleOfTile(this.huntCreature.BestGuessForPosition()), ref path);
                }
                for (int i = 0; i < this.tChunks.Length; i++)
                {
                    if (this.huntCreature is Tracker.ElaborateCreatureRepresentation)
                    {
                        for (int j = 0; j < (this.huntCreature as Tracker.ElaborateCreatureRepresentation).ghosts.Count; j++)
                        {
                            if (this.room.GetTilePosition(this.tChunks[i].pos) == (this.huntCreature as Tracker.ElaborateCreatureRepresentation).ghosts[j].coord.Tile)
                            {
                                (this.huntCreature as Tracker.ElaborateCreatureRepresentation).ghosts[j].Push();
                            }
                        }
                    }
                }
            }
            if ((float)this.grabPath.Count * 20f > this.idealLength || this.neededForLocomotion)
            {
                float num = float.MaxValue;
                int num2 = -1;
                for (int k = 0; k < this.star.tentacles.Length; k++)
                {
                    if (this.star.tentacles[k].task == StarTentacle.Task.Locomotion && !this.star.tentacles[k].neededForLocomotion && (this.star.tentacles[k].idealLength > this.idealLength || this.neededForLocomotion) && !this.star.tentacles[k].atGrabDest && Mathf.Abs(this.star.tentacles[k].idealLength - (float)this.grabPath.Count * 20f) < num)
                    {
                        num = Mathf.Abs(this.star.tentacles[k].idealLength - (float)this.grabPath.Count * 20f);
                        num2 = k;
                    }
                }
                if (num2 > -1)
                {
                    this.star.tentacles[num2].huntCreature = this.huntCreature;
                    this.star.tentacles[num2].task = StarTentacle.Task.Hunt;
                    this.huntCreature = null;
                    this.UpdateClimbGrabPos(ref path);
                    return;
                }
            }
            if (Vector2.Distance(this.room.MiddleOfTile(this.huntCreature.BestGuessForPosition()), base.FloatBase) > this.idealLength * 1.5f)
            {
                this.huntCreature = null;
                this.UpdateClimbGrabPos(ref path);
                return;
            }
            for (int l = 0; l < this.tChunks.Length; l++)
            {
                if (this.backtrackFrom == -1 || this.backtrackFrom > l)
                {
                    if (base.grabDest != null && this.room.VisualContact(this.tChunks[l].pos, this.floatGrabDest.Value))
                    {
                        this.tChunks[l].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[l].pos, 20f) / 20f * 1.2f;
                    }
                    else
                    {
                        this.tChunks[l].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.segments[this.tChunks[l].currentSegment]) - this.tChunks[l].pos, 20f) / 20f * 1.2f;
                    }
                }
            }
        }

    public void Climb(ref List<IntVector2> path)
    {
        float t = Custom.LerpMap((float)this.star.stuckCounter, 50f, 200f, 0.5f, 0.95f);
        this.idealGrabPos = base.FloatBase + (Vector2)Vector3.Slerp(this.tentacleDir, this.star.moveDirection, t) * this.idealLength * 0.7f;
        Vector2 vector = base.FloatBase + (Vector2)Vector3.Slerp(Vector3.Slerp(this.tentacleDir, this.star.moveDirection, t), Custom.RNV(), Mathf.InverseLerp(20f, 200f, (float)this.foundNoGrabPos)) * this.idealLength * Custom.LerpMap((float)Math.Max(this.foundNoGrabPos, this.star.stuckCounter), 20f, 200f, 0.7f, 1.2f);

        List<IntVector2> list = [];
        SharedPhysics.RayTracedTilesArray(base.FloatBase, vector, list);

        bool flag = false;
        for (int i = 0; i < list.Count - 1; i++)
        {
            if (this.room.GetTile(list[i + 1]).Solid)
            {
                this.ConsiderGrabPos(Custom.RestrictInRect(vector, this.room.TileRect(list[i]).Shrink(1f)), this.idealGrabPos);
                flag = true;
                break;
            }
            if (this.room.GetTile(list[i]).horizontalBeam || this.room.GetTile(list[i]).verticalBeam)
            {
                this.ConsiderGrabPos(this.room.MiddleOfTile(list[i]), this.idealGrabPos);
                flag = true;
            }
        }
        if (flag)
        {
            this.foundNoGrabPos = 0;
        }
        else
        {
            this.foundNoGrabPos++;
        }
        bool flag2 = this.secondaryGrabBackTrackCounter < 200 && this.SecondaryGrabPosScore(this.secondaryGrabPos) > 0f;
        for (int j = 0; j < this.tChunks.Length; j++)
        {
            if (this.backtrackFrom == -1 || this.backtrackFrom > j)
            {
                this.StickToTerrain(this.tChunks[j]);
                if (base.grabDest != null)
                {
                    if (!this.atGrabDest && Custom.DistLess(this.tChunks[j].pos, this.floatGrabDest.Value, 20f))
                    {
                        this.atGrabDest = true;
                    }
                    if (this.tChunks[j].currentSegment <= this.grabPath.Count || !flag2)
                    {
                        this.tChunks[j].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[j].pos, 20f) / 20f * 1.2f;
                    }
                    else if (j > 1 && this.segments.Count > this.grabPath.Count && flag2)
                    {
                        float num = Mathf.InverseLerp((float)this.grabPath.Count, (float)this.segments.Count, (float)this.tChunks[j].currentSegment);
                        Vector2 a = Custom.DirVec(this.tChunks[j - 2].pos, this.tChunks[j].pos) * (1f - num) * 0.6f;
                        a += Custom.DirVec(this.tChunks[j].pos, this.room.MiddleOfTile(base.grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                        a += Custom.DirVec(this.tChunks[j].pos, this.room.MiddleOfTile(this.secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                        a += Custom.DirVec(this.tChunks[j].pos, base.FloatBase) * Mathf.Sin(num * 3.1415927f) * 0.3f;
                        this.tChunks[j].vel += a.normalized * 1.2f;
                        if (j == this.tChunks.Length - 1)
                        {
                            this.tChunks[j].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.secondaryGrabPos) - this.tChunks[j].pos, 20f) / 20f * 4.2f;
                        }
                    }
                }
            }
        }
        if (base.grabDest != null)
        {
            this.ConsiderSecondaryGrabPos(base.grabDest.Value + new IntVector2(Random.Range(-20, 21), Random.Range(-20, 21)));
        }
        if (base.grabDest == null || !this.atGrabDest)
        {
            this.UpdateClimbGrabPos(ref path);
        }
    }

    public void ExamineSound(ref List<IntVector2> path)
    {
        if (!Custom.DistLess(this.checkSound.pos, base.FloatBase, this.idealLength * 1.1f) || this.checkSound.slatedForDeletion)
        {
            this.SwitchTask(StarTentacle.Task.Locomotion);
            return;
        }
        this.soundCheckTimer--;
        if (this.soundCheckTimer < 1 || this.examineSoundPos == null || Custom.DistLess(this.examineSoundPos.Value, base.Tip.pos, 20f))
        {
            if (this.examineSoundPos != null)
            {
                this.soundCheckTimer = Random.Range(40, 180);
                this.soundCheckCounter++;
                if (this.soundCheckCounter > 17)
                {
                    this.checkSound.Destroy();
                    this.SwitchTask(StarTentacle.Task.Locomotion);
                    return;
                }
                this.examineSoundPos = default(Vector2?);
            }

            // Modified RayTracedTilesArray usage
            List<IntVector2> list = new List<IntVector2>();
            SharedPhysics.RayTracedTilesArray(checkSound.pos, this.checkSound.pos + Custom.RNV() * 150f, list);

            int num = list.Count;
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (this.room.GetTile(list[i + 1]).Solid)
                {
                    num = i;
                    break;
                }
            }
            for (int j = list.Count - 1; j > num; j--)
                {
                    list.RemoveAt(j);
                }
                while (list.Count > 0)
                {
                    int num2 = list.Count - 1;
                    if (room.aimap.getTerrainProximity(list[num2]) < 2 || ((this.room.GetTile(list[num2]).horizontalBeam || this.room.GetTile(list[num2]).verticalBeam) && Random.value < 0.05f))
                    {
                        this.examineSoundPos = new Vector2?(Custom.RestrictInRect(this.room.MiddleOfTile(list[num2]) + Custom.DirVec(base.FloatBase, this.room.MiddleOfTile(list[num2])) * 20f, this.room.TileRect(list[num2]).Shrink(1f)));
                        break;
                    }
                    list.RemoveAt(num2);
                }
            }
            if (this.examineSoundPos != null)
            {
                base.MoveGrabDest(this.examineSoundPos.Value, ref path);
            }
            for (int k = 0; k < this.tChunks.Length; k++)
            {
                if (this.backtrackFrom == -1 || this.backtrackFrom > k)
                {
                    if (base.grabDest != null && this.room.VisualContact(this.tChunks[k].pos, this.floatGrabDest.Value))
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[k].pos, 20f) / 20f * 1.2f;
                    }
                    else
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.segments[this.tChunks[k].currentSegment]) - this.tChunks[k].pos, 20f) / 20f * 1.2f;
                    }
                }
            }
        }

        public void StickToTerrain(Tentacle.TentacleChunk chunk)
        {
            if (this.floatGrabDest != null && !Custom.DistLess(chunk.pos, this.floatGrabDest.Value, 200f))
            {
                return;
            }
            int num = (int)Mathf.Sign(chunk.pos.x - this.room.MiddleOfTile(chunk.pos).x);
            Vector2 vector = new Vector2(0f, 0f);
            IntVector2 tilePosition = this.room.GetTilePosition(chunk.pos);
            int i = 0;
            while (i < 8)
            {
                if (this.room.GetTile(tilePosition + new IntVector2(Custom.eightDirectionsDiagonalsLast[i].x * num, Custom.eightDirectionsDiagonalsLast[i].y)).Solid)
                {
                    if (Custom.eightDirectionsDiagonalsLast[i].x != 0)
                    {
                        vector.x = this.room.MiddleOfTile(chunk.pos).x + (float)(Custom.eightDirectionsDiagonalsLast[i].x * num) * (20f - chunk.rad);
                    }
                    if (Custom.eightDirectionsDiagonalsLast[i].y != 0)
                    {
                        vector.y = this.room.MiddleOfTile(chunk.pos).y + (float)Custom.eightDirectionsDiagonalsLast[i].y * (20f - chunk.rad);
                        break;
                    }
                    break;
                }
                else
                {
                    i++;
                }
            }
            if (vector.x == 0f && this.room.GetTile(chunk.pos).verticalBeam)
            {
                vector.x = this.room.MiddleOfTile(chunk.pos).x;
            }
            if (vector.y == 0f && this.room.GetTile(chunk.pos).horizontalBeam)
            {
                vector.y = this.room.MiddleOfTile(chunk.pos).y;
            }
            if (chunk.tentacleIndex > this.tChunks.Length / 2)
            {
                if (vector.x != 0f || vector.y != 0f)
                {
                    if (this.chunksStickSounds[chunk.tentacleIndex] > 10)
                    {
                        this.owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Terrain, chunk.pos, Mathf.InverseLerp((float)(this.tChunks.Length / 2), (float)(this.tChunks.Length - 1), (float)chunk.tentacleIndex), 1f);
                    }
                    if (this.chunksStickSounds[chunk.tentacleIndex] > 0)
                    {
                        this.chunksStickSounds[chunk.tentacleIndex] = 0;
                    }
                    else
                    {
                        this.chunksStickSounds[chunk.tentacleIndex]--;
                    }
                }
                else
                {
                    if (this.chunksStickSounds[chunk.tentacleIndex] < -10)
                    {
                        this.owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Terrain, chunk.pos, Mathf.InverseLerp((float)(this.tChunks.Length / 2), (float)(this.tChunks.Length - 1), (float)chunk.tentacleIndex), 1f);
                    }
                    if (this.chunksStickSounds[chunk.tentacleIndex] < 0)
                    {
                        this.chunksStickSounds[chunk.tentacleIndex] = 0;
                    }
                    else
                    {
                        this.chunksStickSounds[chunk.tentacleIndex]++;
                    }
                }
            }
            if (vector.x != 0f)
            {
                chunk.vel.x = chunk.vel.x + (vector.x - chunk.pos.x) * 0.1f;
                chunk.vel.y = chunk.vel.y * 0.9f;
            }
            if (vector.y != 0f)
            {
                chunk.vel.y = chunk.vel.y + (vector.y - chunk.pos.y) * 0.1f;
                chunk.vel.x = chunk.vel.x * 0.9f;
            }
            if (vector.x != 0f || vector.y != 0f)
            {
                this.chunksGripping += 1f / (float)this.tChunks.Length;
            }
        }

        public void ConsiderGrabPos(Vector2 testPos, Vector2 idealGrabPos)
        {
            if (this.GrabPosScore(testPos, idealGrabPos) > this.GrabPosScore(this.preliminaryGrabDest, idealGrabPos))
            {
                this.preliminaryGrabDest = testPos;
            }
        }

        public float GrabPosScore(Vector2 testPos, Vector2 idealGrabPos)
        {
            float num = 100f / Vector2.Distance(testPos, idealGrabPos);
            if (base.grabDest != null && this.room.GetTilePosition(testPos) == base.grabDest.Value)
            {
                num *= 1.5f;
            }
            for (int i = 0; i < 4; i++)
            {
                if (this.room.GetTile(testPos + Custom.fourDirections[i].ToVector2() * 20f).Solid)
                {
                    num *= 2f;
                    break;
                }
            }
            return num;
        }

        public void ConsiderSecondaryGrabPos(IntVector2 testPos)
        {
            if (this.room.GetTile(testPos).Solid)
            {
                return;
            }
            if (this.SecondaryGrabPosScore(testPos) > this.SecondaryGrabPosScore(this.secondaryGrabPos))
            {
                this.secondaryGrabBackTrackCounter = 0;
                this.secondaryGrabPos = testPos;
            }
        }

        public float SecondaryGrabPosScore(IntVector2 testPos)
        {
            if (base.grabDest == null)
            {
                return 0f;
            }
            if (testPos.FloatDist(base.BasePos) < 7f)
            {
                return 0f;
            }
            float num = this.idealLength - (float)this.grabPath.Count * 20f;
            if (Vector2.Distance(this.room.MiddleOfTile(testPos), this.floatGrabDest.Value) > num)
            {
                return 0f;
            }
            if (!SharedPhysics.RayTraceTilesForTerrain(this.room, base.grabDest.Value, testPos))
            {
                return 0f;
            }
            float num2 = 0f;
            for (int i = 0; i < 8; i++)
            {
                if (this.room.GetTile(testPos + Custom.eightDirections[i]).Solid)
                {
                    num2 += 1f;
                }
            }
            if (this.room.GetTile(testPos).horizontalBeam || this.room.GetTile(testPos).verticalBeam)
            {
                num2 += 1f;
            }
            if (num2 > 0f && testPos == this.secondaryGrabPos)
            {
                num2 += 1f;
            }
            if (num2 == 0f)
            {
                return 0f;
            }
            num2 += testPos.FloatDist(base.BasePos) / 10f;
            return num2 / (1f + Mathf.Abs(num * 0.75f - Vector2.Distance(this.room.MiddleOfTile(testPos), this.floatGrabDest.Value)) + Vector2.Distance(this.room.MiddleOfTile(testPos), this.room.MiddleOfTile(this.segments[this.segments.Count - 1])));
        }

        public float ReleaseScore()
        {
            float num = float.MaxValue;
            for (int i = this.tChunks.Length / 2; i < this.tChunks.Length; i++)
            {
                if (Custom.DistLess(this.tChunks[i].pos, this.idealGrabPos, num))
                {
                    num = Vector2.Distance(this.tChunks[i].pos, this.idealGrabPos);
                }
            }
            return num;
        }

        public void UpdateClimbGrabPos(ref List<IntVector2> path)
        {
            if (this.huntCreature != null)
            {
                return;
            }
            base.MoveGrabDest(this.preliminaryGrabDest, ref path);
        }

        public override IntVector2 GravityDirection()
        {
            if (Random.value >= 0.5f)
            {
                return new IntVector2(0, -1);
            }
            return new IntVector2((base.Tip.pos.x < this.connectedChunk.pos.x) ? -1 : 1, -1);
        }

        public int tentacleNumber;

        public Vector2 tentacleDir;

        public float awayFromBodyRotation;

        public bool atGrabDest;

        public int foundNoGrabPos;

        public float chunksGripping;

        public int stun;

        public int secondaryGrabBackTrackCounter;

        public bool lastBackTrack;

        public IntVector2 secondaryGrabPos;

        public Vector2 preliminaryGrabDest;

        public Vector2 idealGrabPos;

        public bool neededForLocomotion;

        public Tracker.CreatureRepresentation huntCreature;

        public NoiseTracker.TheorizedSource checkSound;

        public int soundCheckCounter;

        public int soundCheckTimer;

        public Vector2? examineSoundPos;

        public BodyChunk grabChunk;

        public float sticky;

        public int[] chunksStickSounds;

        public StarTentacle.Task task;

        public new List<IntVector2> scratchPath;

        public Vector2 huntDirection;

        public class Task : ExtEnum<StarTentacle.Task>
        {
            public Task(string value, bool register = false) : base(value, register)
            {
            }

            public static readonly StarTentacle.Task Locomotion = new StarTentacle.Task("Locomotion", true);

            public static readonly StarTentacle.Task Hunt = new StarTentacle.Task("Hunt", true);

            public static readonly StarTentacle.Task ExamineSound = new StarTentacle.Task("ExamineSound", true);

            public static readonly StarTentacle.Task Grabbing = new StarTentacle.Task("Grabbing", true);
        }
    }

