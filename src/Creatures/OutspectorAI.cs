using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Random = UnityEngine.Random;

namespace VoidTemplate.Creatures
{
    public class OutspectorAI : ArtificialIntelligence, IUseItemTracker, IUseARelationshipTracker, ILookingAtCreatures
    {
        public class Behavior : ExtEnum<Behavior>
        {
            public static readonly Behavior Idle = new Behavior("Idle", register: true);

            public static readonly Behavior Hunt = new Behavior("Hunt", register: true);

            public static readonly Behavior EscapeRain = new Behavior("EscapeRain", register: true);

            public static readonly Behavior InspectArea = new Behavior("InspectArea", register: true);

            public Behavior(string value, bool register = false)
                : base(value, register)
            {
            }
        }

        public DebugDestinationVisualizer debugDestinationVisualizer;

        public Behavior behavior;

        public int newIdlePosCounter;

        public WorldCoordinate reactTarget;

        public Vector2 goalPos;

        private List<Vector2> AllHeadGoals;

        private Vector2[] AIHeadGoals;

        private int HeadIdler;

        public Tracker.CreatureRepresentation focusCreature;

        private CreatureLooker creatureLooker;

        private PhysicalObject FirstTimeAttentionGrabber;

        public float AttentionGrabberTimer;

        public bool controlledAnger;

        public Outspector myOutspector => creature.realizedCreature as Outspector;

        public OutspectorAI(AbstractCreature creature, World world)
            : base(creature, world)
        {
            myOutspector.AI = this;
            AddModule(new StandardPather(this, world, creature));
            (base.pathFinder as StandardPather).heuristicCostFac = 1f;
            (base.pathFinder as StandardPather).heuristicDestFac = 1f;
            base.pathFinder.visualize = false;
            AddModule(new Tracker(this, 10, 10, -1, 0.25f, 190, 1, 10));
            base.tracker.visualize = false;
            AddModule(new RainTracker(this));
            AddModule(new DenFinder(this, creature));
            AddModule(new PreyTracker(this, 4, 0.6f, 60f, 300f, 0.75f));
            AddModule(new ThreatTracker(this, 3));
            AddModule(new ItemTracker(this, 10, 15, 600, 4000, stopTrackingCarried: true));
            base.preyTracker.giveUpOnUnreachablePrey = -1;
            AddModule(new RelationshipTracker(this, base.tracker));
            AddModule(new UtilityComparer(this));
            base.utilityComparer.AddComparedModule(base.threatTracker, null, 1f, 1.1f);
            base.utilityComparer.AddComparedModule(base.preyTracker, null, 4f, 1f);
            base.utilityComparer.AddComparedModule(base.rainTracker, null, 10f, 1f);
            behavior = Behavior.Idle;
            goalPos = myOutspector.mainBodyChunk.pos;
            newIdlePosCounter = 1;
            AllHeadGoals = new List<Vector2>();
            AIHeadGoals = new Vector2[Outspector.headCount()];
            creatureLooker = new CreatureLooker(this, base.tracker, myOutspector, 0.2f, 20);
        }

        AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            return base.tracker;
        }

        RelationshipTracker.TrackedCreatureState IUseARelationshipTracker.CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            if (rel.trackerRep is Tracker.ElaborateCreatureRepresentation)
            {
                if (rel.trackerRep.representedCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
                    if (rel.trackerRep.representedCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.Inspector)
                    {
                        rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Attacks;
                    }

                if (rel.trackerRep.representedCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.Inspector)
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Ignores;

                }

                else
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                    rel.currentRelationship.intensity = 1f;
                    base.preyTracker.AddPrey(rel.trackerRep);

                }

                if (rel.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Overseer)
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Ignores;

                }
                else

                if (rel.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplateType.Outspector)
                {
                    rel.currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                    rel.currentRelationship.intensity = 2f;
                    base.preyTracker.AddPrey(rel.trackerRep);

                }

                if (!rel.trackerRep.representedCreature.realizedCreature.dead)
                {
                    GrabAttentionWithObject(rel.trackerRep.representedCreature.realizedCreature);
                }

                rel.currentRelationship.intensity = 1f;
                FirstTimeAttentionGrabber = rel.trackerRep.representedCreature.realizedCreature;
            }

            return rel.state;
        }

        CreatureTemplate.Relationship IUseARelationshipTracker.UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship currentRelationship = dRelation.currentRelationship;
            if (dRelation.trackerRep is Tracker.SimpleCreatureRepresentation)
            {
                return currentRelationship;
            }

            if (base.preyTracker.MostAttractivePrey != null && base.preyTracker.MostAttractivePrey.representedCreature == dRelation.trackerRep.representedCreature && currentRelationship.type == CreatureTemplate.Relationship.Type.Uncomfortable)
            {
                currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                currentRelationship.intensity = 1f;
            }

            if (currentRelationship.type == CreatureTemplate.Relationship.Type.Uncomfortable || dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplateType.Outspector)
            {
                if (dRelation.trackerRep.VisualContact)
                {
                    if (dRelation.trackerRep.representedCreature.realizedCreature != null && !dRelation.trackerRep.representedCreature.realizedCreature.dead)
                    {
                        Creature realizedCreature = dRelation.trackerRep.representedCreature.realizedCreature;
                        if (realizedCreature.grasps != null && realizedCreature.grasps.Length != 0)
                        {
                            for (int i = 0; i < realizedCreature.grasps.Length; i++)
                            {
                                if (realizedCreature.grasps[i] == null || realizedCreature.grasps[i].grabbed is not SSOracleSwarmer)
                                {
                                    continue;
                                }

                                if ((realizedCreature.grasps[i].grabbed as SSOracleSwarmer).bites < 3)
                                {
                                    currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                                    currentRelationship.intensity = 1f;
                                    base.preyTracker.AddPrey(dRelation.trackerRep);
                                    continue;
                                }

                                currentRelationship.intensity = 1f;
                                if (!myOutspector.safariControlled)
                                {
                                    myOutspector.anger += 0.09f;
                                    OrderAHeadToGrabObject(realizedCreature.grasps[i].grabbed);
                                }
                            }
                        }

                        if (!myOutspector.safariControlled && dRelation.trackerRep.representedCreature.creatureTemplate.type != CreatureTemplateType.Outspector && myOutspector.DangerousThrowLocations.Count > 0 && Random.value < 0.1f)
                        {
                            foreach (Vector2 dangerousThrowLocation in myOutspector.DangerousThrowLocations)
                            {
                                float num = Vector2.Distance(dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.pos, dangerousThrowLocation);
                                if (dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.vel.magnitude > 10f * Mathf.InverseLerp(150f, 600f, num) && num < 230f && Vector2.Distance(dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.pos + dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.vel, dangerousThrowLocation) < num && num < Vector2.Distance(myOutspector.mainBodyChunk.pos, dangerousThrowLocation))
                                {
                                    OrderAHeadToGrabObject(dRelation.trackerRep.representedCreature.realizedCreature);
                                }
                            }
                        }

                        currentRelationship.intensity = 1f;
                    }

                    if (currentRelationship.intensity < 0.5f && Random.value < 0.02f)
                    {
                        currentRelationship.intensity = 1f;
                    }

                    if (Vector2.Distance(myOutspector.mainBodyChunk.pos, dRelation.trackerRep.lastSeenCoord.Tile.ToVector2() * 20f) < 100f && currentRelationship.intensity + 0.09f < 1f)
                    {
                        currentRelationship.intensity += 0.09f;
                    }
                }

                if (currentRelationship.intensity > 0f && dRelation.trackerRep.VisualContact)
                {
                    currentRelationship.intensity -= 0.006f;
                }

                if (currentRelationship.intensity > 0f && !dRelation.trackerRep.VisualContact)
                {
                    currentRelationship.intensity -= 0.01f;
                }

                if (currentRelationship.intensity < 0f)
                {
                    currentRelationship.intensity = 0f;
                }
            }
            else if (currentRelationship.type == CreatureTemplate.Relationship.Type.Attacks && dRelation.trackerRep.VisualContact)
            {
                if (myOutspector.activeEye != -1 && !myOutspector.safariControlled && Vector2.Distance(myOutspector.heads[myOutspector.activeEye].Tip.pos, dRelation.trackerRep.lastSeenCoord.Tile.ToVector2() * 20f) < 500f)
                {
                    myOutspector.anger += 0.19f;
                    if (myOutspector.anger > 1f)
                    {
                        if (myOutspector.anger > 2f)
                        {
                            myOutspector.anger = 2f;
                        }

                        currentRelationship.intensity += 0.08f;
                        if (currentRelationship.intensity > 1f)
                        {
                            currentRelationship.intensity = 1f;
                            behavior = Behavior.EscapeRain;
                            newIdlePosCounter = Random.Range(300, 400);
                        }
                    }
                }
                else if (!myOutspector.safariControlled)
                {
                    myOutspector.anger -= 0.001f;
                    if (myOutspector.anger < 0f)
                    {
                        myOutspector.anger = 0f;
                    }
                }
            }
            else if (currentRelationship.type == CreatureTemplate.Relationship.Type.Eats && dRelation.trackerRep.VisualContact)
            {
                if (dRelation.trackerRep.representedCreature.realizedCreature.dead && myOutspector.anger <= 0f)
                {
                    base.preyTracker.ForgetPrey(dRelation.trackerRep.representedCreature);
                    currentRelationship.type = CreatureTemplate.Relationship.Type.Uncomfortable;
                    currentRelationship.intensity = 1f;
                }

                if (!myOutspector.abstractCreature.controlled)
                {
                    myOutspector.anger += 0.19f;
                }

                if (myOutspector.anger > 1f)
                {
                    myOutspector.anger = 1f;
                }

                currentRelationship.intensity += 0.05f;
                if (currentRelationship.intensity >= 1f)
                {
                    Creature realizedCreature2 = dRelation.trackerRep.representedCreature.realizedCreature;
                    if (realizedCreature2.abstractCreature.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.DaddyLongLegs)
                    {
                        if (currentRelationship.intensity >= 1f)
                        {
                            int num2 = -1;
                            for (int j = 0; j < Outspector.headCount(); j++)
                            {
                                if (!myOutspector.HeadsCrippled(j) && myOutspector.headGrabChunk[j] != null && myOutspector.headGrabChunk[j].owner == realizedCreature2)
                                {
                                    num2 = j;
                                    break;
                                }
                            }

                            if (num2 == -1)
                            {
                                if (!myOutspector.safariControlled)
                                {
                                    OrderAHeadToGrabObject(realizedCreature2);
                                }
                            }
                            else
                            {
                                if (myOutspector.heads[num2].Tip.vel.magnitude < 1f || myOutspector.heads[num2].Tip.vel.magnitude > 4f)
                                {
                                    myOutspector.heads[num2].Tip.vel *= 1.2f;
                                    myOutspector.heads[num2].Tip.vel += new Vector2(Random.Range(-18, 18), Random.Range(-18, 18));
                                    realizedCreature2.firstChunk.pos = myOutspector.heads[num2].Tip.pos;
                                    realizedCreature2.firstChunk.vel = myOutspector.heads[num2].Tip.vel;
                                }

                                float target = Custom.VecToDeg(Custom.DirVec(myOutspector.mainBodyChunk.pos, realizedCreature2.firstChunk.pos));
                                bool flag = false;
                                float num3 = 2000f;
                                for (int k = 0; k < myOutspector.DangerousThrowLocations.Count; k++)
                                {
                                    Vector2 vector = Vector2.Lerp(realizedCreature2.firstChunk.pos, myOutspector.DangerousThrowLocations[k], 0.8f);
                                    if (Random.value < 0.85f && Vector2.Distance(myOutspector.mainBodyChunk.pos, myOutspector.DangerousThrowLocations[k]) < num3 && myOutspector.room.RayTraceTilesForTerrain((int)(realizedCreature2.firstChunk.pos.x / 20f), (int)(realizedCreature2.firstChunk.pos.y / 20f), (int)(vector.x / 20f), (int)(vector.y / 20f)))
                                    {
                                        num3 = Vector2.Distance(myOutspector.mainBodyChunk.pos, myOutspector.DangerousThrowLocations[k]);
                                        flag = true;
                                        target = Custom.VecToDeg(Custom.DirVec(realizedCreature2.firstChunk.pos, myOutspector.DangerousThrowLocations[k]));
                                    }
                                }

                                float num4 = 35f;
                                if (flag)
                                {
                                    num4 = 10f;
                                }

                                if (!flag && !myOutspector.room.RayTraceTilesForTerrain((int)(realizedCreature2.firstChunk.pos.x / 20f), (int)(realizedCreature2.firstChunk.pos.y / 20f), (int)(realizedCreature2.firstChunk.pos.x + realizedCreature2.firstChunk.vel.x * 3f / 20f), (int)(realizedCreature2.firstChunk.pos.y + realizedCreature2.firstChunk.vel.y * 3f / 20f)) && (realizedCreature2.firstChunk.vel.magnitude > 50f || (Random.value < 0.5f && Mathf.DeltaAngle(Custom.VecToDeg(realizedCreature2.firstChunk.vel), target) < num4 && realizedCreature2.firstChunk.vel.magnitude > 30f)))
                                {
                                    currentRelationship.intensity = 0f;
                                    myOutspector.headWantToGrabChunk[num2] = null;
                                    myOutspector.headGrabChunk[num2] = null;
                                    myOutspector.room.PlaySound(SoundID.Vulture_Peck, myOutspector.heads[num2].Tip.pos);
                                }
                                else if (Mathf.DeltaAngle(Custom.VecToDeg(realizedCreature2.firstChunk.vel), target) < num4 && realizedCreature2.firstChunk.vel.magnitude > 20f)
                                {
                                    currentRelationship.intensity = 0f;
                                    myOutspector.headWantToGrabChunk[num2] = null;
                                    myOutspector.headGrabChunk[num2] = null;
                                    myOutspector.room.PlaySound(SoundID.Vulture_Peck, myOutspector.heads[num2].Tip.pos);
                                }
                            }
                        }
                    }
                    else
                    {
                        currentRelationship.intensity += 0.03f;
                        if (currentRelationship.intensity > 1f)
                        {
                            currentRelationship.intensity = 1f;
                        }
                    }
                }
            }

            return currentRelationship;
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);
        }

        public override void Update()
        {
            base.Update();
            if (myOutspector.LickedByPlayer != null)
            {
                base.tracker.SeeCreature(myOutspector.LickedByPlayer.abstractCreature);
            }

            if (myOutspector.safariControlled)
            {
                if (controlledAnger)
                {
                    myOutspector.anger = Mathf.Lerp(myOutspector.anger, 1f, 0.12f);
                }
                else
                {
                    myOutspector.anger = Mathf.Lerp(myOutspector.anger, 0f, 0.12f);
                }

                if (myOutspector.inputWithDiagonals.HasValue && myOutspector.inputWithDiagonals.Value.jmp && !myOutspector.lastInputWithDiagonals.Value.jmp)
                {
                    controlledAnger = !controlledAnger;
                }
            }
            else
            {
                myOutspector.anger -= 0.06f;
            }

            if (myOutspector.anger > 1f)
            {
                myOutspector.anger = 1f;
            }

            if (myOutspector.anger < 0f)
            {
                myOutspector.anger = 0f;
            }

            creatureLooker.Update();
            if (AttentionGrabberTimer > 0f)
            {
                AttentionGrabberTimer -= 1f;
                if (AttentionGrabberTimer == 0f)
                {
                    for (int i = 0; i < Outspector.headCount(); i++)
                    {
                        if (!myOutspector.HeadWeaponized(i))
                        {
                            myOutspector.headCuriosityFocus[i] = FirstTimeAttentionGrabber;
                        }
                    }

                    FirstTimeAttentionGrabber = null;
                }
            }

            if (AllHeadGoals.Count == 0)
            {
                PopulateHeadSpecialGoals(20);
                PopulateInspectionModeHeadGoals(20);
                for (int j = 0; j < Outspector.headCount(); j++)
                {
                    HeadFindNewMoreInterestingGoal(j);
                }
            }
            else if (behavior != Behavior.InspectArea)
            {
                PopulateHeadGoals(10);
            }

            if (HeadIdler <= 0)
            {
                HeadFindNewMoreInterestingGoal(Random.Range(0, Outspector.headCount()));
                HeadIdler = Random.Range(200, 500);
            }

            HeadIdler--;
            if (behavior != Behavior.Idle)
            {
                HeadIdler--;
            }

            AIModule aIModule = base.utilityComparer.HighestUtilityModule();
            if (base.utilityComparer.HighestUtility() > 0.3f && aIModule != null && aIModule is RainTracker)
            {
                behavior = Behavior.EscapeRain;
            }
            else if (base.utilityComparer.HighestUtility() > 0.1f && aIModule != null && aIModule is PreyTracker)
            {
                behavior = Behavior.Hunt;
            }
            else if (behavior != Behavior.Idle && behavior != Behavior.InspectArea)
            {
                behavior = Behavior.Idle;
                newIdlePosCounter = 1;
            }

            if (behavior == Behavior.EscapeRain || myOutspector.AllHeadsCrippled())
            {
                if (base.denFinder.GetDenPosition().HasValue)
                {
                    creature.abstractAI.SetDestination(base.denFinder.GetDenPosition().Value);
                }

                return;
            }

            if (behavior == Behavior.Hunt)
            {
                WorldCoordinate lastSeenCoord = base.preyTracker.MostAttractivePrey.lastSeenCoord;
                if (base.pathFinder.CoordinateReachableAndGetbackable(lastSeenCoord))
                {
                    creature.abstractAI.SetDestination(lastSeenCoord);
                }

                return;
            }

            if (behavior == Behavior.Idle)
            {
                if (this.newIdlePosCounter < 1 && this.myOutspector.room.aimap.getTerrainProximity(this.myOutspector.abstractCreature.pos) > 4 && this.myOutspector.abstractCreature.abstractAI.destination.room == this.myOutspector.abstractCreature.pos.room && Vector2.Distance(this.myOutspector.abstractCreature.abstractAI.destination.Tile.ToVector2(), this.myOutspector.abstractCreature.pos.Tile.ToVector2()) < 80f)
                {
                    Debug.Log("Outspector triggered inspection mode");
                    AllHeadGoals.Clear();
                    PopulateInspectionModeHeadGoals(50);
                    PopulateHeadSpecialGoals(15);
                    behavior = Behavior.InspectArea;
                    creature.abstractAI.SetDestination(myOutspector.abstractCreature.abstractAI.destination);
                    newIdlePosCounter = Random.Range(1400, 2500);
                }
                else
                {
                    newIdlePosCounter--;
                    if (newIdlePosCounter < 1 || !base.pathFinder.CoordinateReachableAndGetbackable(base.pathFinder.GetDestination))
                    {
                        WorldCoordinate worldCoordinate = new WorldCoordinate(creature.pos.room, Random.Range(0, myOutspector.room.TileWidth), Random.Range(0, myOutspector.room.TileHeight), -1);
                        if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate))
                        {
                            creature.abstractAI.SetDestination(worldCoordinate);
                            newIdlePosCounter = Random.Range(300, 2000);
                        }
                    }
                    else if (base.pathFinder.GetDestination.room == this.creature.pos.room && base.pathFinder.GetDestination.TileDefined && this.myOutspector.room.aimap.getTerrainProximity(base.pathFinder.GetDestination) < 1)
                    {
                        WorldCoordinate worldCoordinate2 = base.pathFinder.GetDestination + Custom.fourDirections[UnityEngine.Random.Range(0, 4)];
                        if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate2) && this.myOutspector.room.aimap.getTerrainProximity(worldCoordinate2) > this.myOutspector.room.aimap.getTerrainProximity(base.pathFinder.GetDestination))
                        {
                            this.creature.abstractAI.SetDestination(worldCoordinate2);
                        }

                        if (base.pathFinder.GetDestination.room == creature.pos.room && base.pathFinder.GetDestination.TileDefined && base.pathFinder.GetDestination.Tile.FloatDist(creature.pos.Tile) < 3f && myOutspector.room.VisualContact(creature.pos.Tile, base.pathFinder.GetDestination.Tile))
                        {
                            WorldCoordinate worldCoordinate3 = base.pathFinder.GetDestination + new IntVector2(Random.Range(-20, 21), Random.Range(-20, 21));
                            if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate3) && worldCoordinate3.Tile.FloatDist(creature.pos.Tile) >= 7f)
                            {
                                creature.abstractAI.SetDestination(worldCoordinate3);
                                return;
                            }
                        }

                        if (newIdlePosCounter > 10)
                        {
                            foreach (ZapCoil zapCoil in myOutspector.room.zapCoils)
                            {
                                Vector2 pos = base.pathFinder.GetDestination.Tile.ToVector2() * 20f;
                                IntRect rect = zapCoil.rect;
                                rect.left -= 5;
                                rect.right += 5;
                                rect.bottom -= 5;
                                rect.top += 5;
                                if (Custom.InsideRect(myOutspector.room.GetTilePosition(pos), rect))
                                {
                                    newIdlePosCounter = 10;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (behavior == Behavior.InspectArea)
                {
                    newIdlePosCounter--;
                    if (newIdlePosCounter < 1)
                    {
                        Debug.Log("Outspector left inspection mode");
                        behavior = Behavior.Idle;
                        AllHeadGoals.Clear();
                        PopulateHeadGoals(50);
                        PopulateHeadSpecialGoals(10);
                        creature.abstractAI.SetDestination(myOutspector.abstractCreature.abstractAI.destination);
                        newIdlePosCounter = 1;
                    }
                }
            }
        }

        public override void CreatureSpotted(bool firstSpot, Tracker.CreatureRepresentation otherCreature)
        {
            creatureLooker.ReevaluateLookObject(otherCreature, 4f);
            base.CreatureSpotted(firstSpot, otherCreature);
        }

        public override float VisualScore(Vector2 lookAtPoint, float targetSpeed)
        {
            base.VisualScore(lookAtPoint, targetSpeed);
            if (myOutspector.activeEye == -1)
            {
                return 0f;
            }

            if ((myOutspector.State as Outspector.OutspectorState).headHealth[myOutspector.activeEye] > 0f)
            {
                Tentacle tentacle = myOutspector.heads[myOutspector.activeEye];
                float current = Custom.Angle(tentacle.tChunks[tentacle.tChunks.Length - 2].pos, tentacle.tChunks[tentacle.tChunks.Length - 1].pos);
                float target = Custom.Angle(tentacle.tChunks[tentacle.tChunks.Length - 1].pos, lookAtPoint);
                return Mathf.InverseLerp(50f, 30f, Mathf.DeltaAngle(current, target));
            }

            return 0f;
        }

        public override Tracker.CreatureRepresentation CreateTrackerRepresentationForCreature(AbstractCreature otherCreature)
        {
            if (otherCreature.creatureTemplate.smallCreature)
            {
                return new Tracker.SimpleCreatureRepresentation(base.tracker, otherCreature, 0f, forgetWhenNotVisible: false);
            }

            return new Tracker.ElaborateCreatureRepresentation(base.tracker, otherCreature, 1f, 3);
        }

        public override bool WantToStayInDenUntilEndOfCycle()
        {
            if (!(behavior == Behavior.EscapeRain))
            {
                return creature.world.rainCycle.TimeUntilRain < 40;
            }

            return true;
        }

        private float HeadGoalInterest(int headIndex, Vector2 Goal)
        {
            Vector2 pos = myOutspector.heads[headIndex].Tip.pos;
            pos = Vector2.Lerp(pos, myOutspector.mainBodyChunk.pos, Random.Range(0.4f, 0.9f));
            pos += myOutspector.mainBodyChunk.vel * 10f;
            float a = Mathf.InverseLerp(800f, 60f, Vector2.Distance(pos, Goal));
            a = Mathf.Lerp(a, 0f, Mathf.InverseLerp(0f, 100f, Vector2.Distance(myOutspector.mainBodyChunk.pos, Goal)));
            if (creatureLooker.lookCreature != null)
            {
                Vector2 b = creatureLooker.lookCreature.BestGuessForPosition().Tile.ToVector2() * 20f;
                float num = Mathf.InverseLerp(800f, 10f, Vector2.Distance(Goal, b));
                if (creatureLooker.lookCreature.VisualContact)
                {
                    num *= 1.7f;
                }

                a = Mathf.Lerp(a, num, (500f - (float)creatureLooker.lookCreature.TicksSinceSeen) / 500f);
            }

            for (int i = 0; i < Outspector.headCount(); i++)
            {
                if (Goal == HeadGoal(i))
                {
                    a = float.MinValue;
                }
            }

            return a;
        }

        public void HeadFindNewMoreInterestingGoal(int headIndex)
        {
            if (AllHeadGoals.Count == 0)
            {
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 vector = AllHeadGoals[Random.Range(0, AllHeadGoals.Count)];
                if (creatureLooker.lookCreature != null && creatureLooker.lookCreature.VisualContact && creatureLooker.lookCreature.representedCreature.realizedCreature != null && creatureLooker.lookCreature.dynamicRelationship != null && creatureLooker.lookCreature.dynamicRelationship.currentRelationship.intensity > 0.6f && (Vector2.Distance(creatureLooker.lookCreature.representedCreature.realizedCreature.firstChunk.pos, myOutspector.heads[headIndex].Tip.pos) < 60f || Vector2.Distance(creatureLooker.lookCreature.representedCreature.realizedCreature.firstChunk.pos, myOutspector.firstChunk.pos) < 128f))
                {
                    myOutspector.headCuriosityFocus[headIndex] = creatureLooker.lookCreature.representedCreature.realizedCreature;
                }
                else if (HeadGoalInterest(headIndex, HeadGoal(headIndex)) < HeadGoalInterest(headIndex, vector))
                {
                    myOutspector.headCuriosityFocus[headIndex] = null;
                    AIHeadGoals[headIndex] = vector;
                    break;
                }
            }

            AIHeadGoals[headIndex] = HeadGoal(headIndex);
        }

        public Vector2 HeadGoal(int headIndex)
        {
            return AIHeadGoals[headIndex];
        }

        public float CreatureInterestBonus(Tracker.CreatureRepresentation crit, float score)
        {
            return score;
        }

        public Tracker.CreatureRepresentation ForcedLookCreature()
        {
            return focusCreature;
        }

        public void LookAtNothing()
        {
        }

        public void PopulateHeadGoals(int posamount)
        {
            if (AllHeadGoals.Count >= 500)
            {
                return;
            }

            for (int i = 0; i < posamount; i++)
            {
                WorldCoordinate worldCoordinate = new WorldCoordinate(myOutspector.room.abstractRoom.index, Random.Range(0, myOutspector.room.TileWidth), Random.Range(0, myOutspector.room.TileHeight), -1);
                if (!myOutspector.room.GetTile(worldCoordinate.Tile).Solid)
                {
                    AllHeadGoals.Add(worldCoordinate.Tile.ToVector2() * 20f);
                }
            }
        }

        public void PopulateHeadSpecialGoals(int posamount)
        {
            foreach (ZapCoil zapCoil in myOutspector.room.zapCoils)
            {
                for (int i = 0; i < posamount; i++)
                {
                    WorldCoordinate worldCoordinate = new WorldCoordinate(myOutspector.room.abstractRoom.index, Random.Range(zapCoil.rect.left - 30, zapCoil.rect.right + 30), Random.Range(zapCoil.rect.bottom - 30, zapCoil.rect.top + 30), -1);
                    if (myOutspector.room.IsPositionInsideBoundries(worldCoordinate.Tile) && !myOutspector.room.GetTile(worldCoordinate.Tile).Solid)
                    {
                        AllHeadGoals.Add(worldCoordinate.Tile.ToVector2() * 20f);
                    }
                }
            }

            foreach (PlacedObject placedObject in myOutspector.room.roomSettings.placedObjects)
            {
                if (placedObject.type == PlacedObject.Type.CoralCircuit)
                {
                    for (int j = 0; j < posamount; j++)
                    {
                        WorldCoordinate worldCoordinate2 = new WorldCoordinate(myOutspector.room.abstractRoom.index, (int)Random.Range(placedObject.pos.x - 60f, placedObject.pos.x + 60f), (int)Random.Range(placedObject.pos.y - 60f, placedObject.pos.y + 60f), -1);
                        if (myOutspector.room.IsPositionInsideBoundries(worldCoordinate2.Tile) && !myOutspector.room.GetTile(worldCoordinate2.Tile).Solid)
                        {
                            AllHeadGoals.Add(worldCoordinate2.Tile.ToVector2() * 20f);
                        }
                    }
                }
                else
                {
                    if (!(placedObject.type == PlacedObject.Type.CoralStem))
                    {
                        continue;
                    }

                    for (int k = 0; k < posamount; k++)
                    {
                        WorldCoordinate worldCoordinate3 = new WorldCoordinate(myOutspector.room.abstractRoom.index, (int)Random.Range(placedObject.pos.x - 80f, placedObject.pos.x + 80f), (int)Random.Range(placedObject.pos.y - 80f, placedObject.pos.y + 80f), -1);
                        if (myOutspector.room.IsPositionInsideBoundries(worldCoordinate3.Tile) && !myOutspector.room.GetTile(worldCoordinate3.Tile).Solid)
                        {
                            AllHeadGoals.Add(worldCoordinate3.Tile.ToVector2() * 20f);
                        }
                    }
                }
            }
        }

        public void PopulateInspectionModeHeadGoals(int posamount)
        {
            for (int i = 0; i < posamount; i++)
            {
                WorldCoordinate pos = myOutspector.abstractCreature.pos;
                pos.x += Random.Range(-20, 20);
                pos.y += Random.Range(-20, 20);
                if (myOutspector.room.IsPositionInsideBoundries(pos.Tile) && !myOutspector.room.GetTile(pos.Tile).Solid)
                {
                    AllHeadGoals.Add(pos.Tile.ToVector2() * 20f);
                }
            }
        }

        public void GrabAttentionWithObject(PhysicalObject inputObject)
        {
            if (myOutspector.activeEye != -1)
            {
                myOutspector.headCuriosityFocus[myOutspector.activeEye] = inputObject;
                AttentionGrabberTimer = Outspector.attentionDelayMax;
            }
        }

        public bool TrackItem(AbstractPhysicalObject obj)
        {
            if (obj.realizedObject != null)
            {
                return obj.realizedObject is Weapon;
            }

            return false;
        }

        public void SeeThrownWeapon(PhysicalObject obj, Creature thrower)
        {
        }

        public void OrderAHeadToGrabObject(PhysicalObject Object)
        {
            int i = 0;
            int num = 0;
            for (; i < Outspector.headCount(); i++)
            {
                if (!myOutspector.HeadsCrippled(i) && !myOutspector.HeadWeaponized(i) && Vector2.Distance(myOutspector.heads[i].Tip.pos, Object.firstChunk.pos) < Vector2.Distance(myOutspector.heads[num].Tip.pos, Object.firstChunk.pos))
                {
                    num = i;
                }
            }

            if (myOutspector.headWantToGrabChunk[num] == null && myOutspector.headGrabChunk[num] == null)
            {
                myOutspector.headWantToGrabChunk[num] = Object.firstChunk;
            }
        }
    }
}
