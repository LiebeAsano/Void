using RWCustom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Noise;
using System;
using Random = UnityEngine.Random;

namespace VoidTemplate;

    
    public class mimicAI : ArtificialIntelligence, IUseARelationshipTracker, IAINoiseReaction
    {
        public class Behavior : ExtEnum<Behavior>
        {
            public static Behavior Idle = new Behavior("Idle", register: true);

            public static Behavior Hunt = new Behavior("Hunt", register: true);

            public static Behavior EscapeRain = new Behavior("EscapeRain", register: true);

            public static Behavior ExamineSound = new Behavior("ExamineSound", register: true);

            public static readonly Behavior Lurk = new Behavior("Lurk", true);

            public Behavior(string value, bool register = false)
                : base(value, register)
            {
            }
        }

        public DebugDestinationVisualizer debugDestinationVisualizer;

        public WorldCoordinate reactTarget;

        public int reactNoiseTime;

        public Behavior behavior;

        public int newIdlePosCounter;

        public float runSpeed;

        public mimicAI.LurkTracker lurkTracker;

        public Mimicstarfish star => creature.realizedCreature as Mimicstarfish;

        public mimicAI(AbstractCreature creature, World world)
            : base(creature, world)
        {
            star.AI = this;
            AddModule(new StandardPather(this, world, creature));
            (base.pathFinder as StandardPather).heuristicCostFac = 1f;
            (base.pathFinder as StandardPather).heuristicDestFac = 1f;
           
                AddModule(new Tracker(this, 10, 10, -1, 0.25f, 50, 1, 1));
            

           
           

            AddModule(new RainTracker(this));
            AddModule(new DenFinder(this, creature));
            
                AddModule(new PreyTracker(this, 7, 0f, 60f, 100f, 0.75f));
                base.preyTracker.giveUpOnUnreachablePrey = -1;
            

            AddModule(new RelationshipTracker(this, base.tracker));
            AddModule(new UtilityComparer(this));
            base.utilityComparer.AddComparedModule(base.preyTracker, null, 1f, 1f);
            base.utilityComparer.AddComparedModule(base.noiseTracker, null, 0.1f, 1f);
            base.utilityComparer.AddComparedModule(base.rainTracker, null, 1f, 1f);
            this.lurkTracker = new mimicAI.LurkTracker(this, this.star);
            base.AddModule(this.lurkTracker);
            base.utilityComparer.AddComparedModule(this.lurkTracker, null, Mathf.Lerp(0.4f, 0.3f, creature.personality.energy), 1f);
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);
        }

        public override void Update()
        {
          

            if (ModManager.MSC && star.LickedByPlayer != null)
            {
                base.tracker.SeeCreature(star.LickedByPlayer.abstractCreature);
            }

            behavior = Behavior.Idle;
            reactNoiseTime--;
            AIModule aIModule = base.utilityComparer.HighestUtilityModule();
            if (base.utilityComparer.HighestUtility() > 0.01f && aIModule != null)
            {
                if (aIModule is PreyTracker)
                {
                    behavior = Behavior.Hunt;
                }

                if (aIModule is NoiseTracker)
                {
                    behavior = Behavior.ExamineSound;
                }

                if (aIModule is RainTracker)
                {
                    behavior = Behavior.EscapeRain;
                }

            }

            if ((base.noiseTracker != null && base.noiseTracker.Utility() > 0f))
            {
                behavior = Behavior.ExamineSound;
            }

            if (star.safariControlled && star.inputWithDiagonals.HasValue && star.inputWithDiagonals.Value.pckp)
            {
                behavior = Behavior.Hunt;
            }

            if (star.safariControlled && behavior == Behavior.Hunt && (!star.inputWithDiagonals.HasValue || !star.inputWithDiagonals.Value.pckp))
            {
                behavior = Behavior.Idle;
            }

            if (behavior == Behavior.Idle)
            {
                bool flag = true;

                
                newIdlePosCounter--;
                if (newIdlePosCounter < 1 || !base.pathFinder.CoordinateReachableAndGetbackable(base.pathFinder.GetDestination))
                {
                    WorldCoordinate worldCoordinate = new WorldCoordinate(creature.pos.room, Random.Range(0, star.room.TileWidth), Random.Range(0, star.room.TileHeight), -1);
                    if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate))
                    {
                        creature.abstractAI.SetDestination(worldCoordinate);
                       
                            newIdlePosCounter = Random.Range(300, 2000);
                        
                    }
                }
                else if (base.pathFinder.GetDestination.room == creature.pos.room && base.pathFinder.GetDestination.TileDefined && star.room.aimap.getTerrainProximity(base.pathFinder.GetDestination) < 6)
                {
                    flag = (this.newIdlePosCounter < 1);
                    WorldCoordinate worldCoordinate2 = base.pathFinder.GetDestination + Custom.fourDirections[Random.Range(0, 4)];
                    if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate2) && star.room.aimap.getTerrainProximity(worldCoordinate2) > star.room.aimap.getTerrainProximity(base.pathFinder.GetDestination))
                    {
                        creature.abstractAI.SetDestination(worldCoordinate2);
                    }
                }

                if ( base.pathFinder.GetDestination.room == creature.pos.room && base.pathFinder.GetDestination.TileDefined && base.pathFinder.GetDestination.Tile.FloatDist(creature.pos.Tile) < 7f && star.room.VisualContact(creature.pos.Tile, base.pathFinder.GetDestination.Tile))
                {
                    flag = (this.newIdlePosCounter < 1);
                    WorldCoordinate worldCoordinate3 = base.pathFinder.GetDestination + new IntVector2(Random.Range(-20, 21), Random.Range(-20, 21));
                    if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate3) && worldCoordinate3.Tile.FloatDist(creature.pos.Tile) >= 7f)
                    {
                        creature.abstractAI.SetDestination(worldCoordinate3);
                    }
                }
                if (flag)
                {
                   
                        this.runSpeed = Mathf.Lerp(this.runSpeed, 0.25f, 0.5f);
                   
                }
                else
                {
                    this.runSpeed = Mathf.Lerp(this.runSpeed, 0f, 0.5f);
                }
            }
            else if (this.behavior == Behavior.Lurk)
            {
                this.runSpeed = Mathf.Lerp(this.runSpeed, 0f, 0f);
                if (Custom.ManhattanDistance(this.creature.pos, this.creature.abstractAI.destination) > 5 && this.lurkTracker.LurkPosScore(this.creature.pos) * 1.2f < this.lurkTracker.LurkPosScore(this.lurkTracker.lurkPosition))
                {
                    this.runSpeed = Mathf.Lerp(this.runSpeed, 0.5f, 0.5f);
                    this.creature.abstractAI.SetDestination(this.lurkTracker.lurkPosition);
                }
                else if (base.VisualContact(this.star.room.MiddleOfTile(this.lurkTracker.lookPosition), 3.4028235E+38f))
                {
                    this.creature.abstractAI.SetDestination(this.lurkTracker.lurkPosition);
                    this.runSpeed = Mathf.Lerp(this.runSpeed, 0f, 0.5f);
                }
                else
                {
                    this.creature.abstractAI.SetDestination(Custom.MakeWorldCoordinate(this.lurkTracker.lurkPosition.Tile + Custom.fourDirections[UnityEngine.Random.Range(0, 4)], this.lurkTracker.lurkPosition.room));
                    this.runSpeed = Mathf.Lerp(this.runSpeed, 0.7f, 0.2f);
                }
            }
            else if (behavior == Behavior.ExamineSound)
            {
                if (ModManager.MSC && reactNoiseTime > 0)
                {
                    creature.abstractAI.SetDestination(reactTarget);
                }
                else
                {
                    creature.abstractAI.SetDestination(base.noiseTracker.ExaminePos);
                }
            }
            else if (behavior == Behavior.Hunt)
            {
                if (base.preyTracker.MostAttractivePrey != null)
                {
                    
                    WorldCoordinate worldCoordinate4 = base.preyTracker.MostAttractivePrey.BestGuessForPosition();
                   

                    if (!worldCoordinate4.TileDefined || worldCoordinate4.room != creature.pos.room)
                    {
                        creature.abstractAI.SetDestination(worldCoordinate4);
                    }
                    else
                    {
                        bool flag = false;
                        for (int i = 0; i < 5; i++)
                        {
                            if (flag)
                            {
                                break;
                            }

                            if (base.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate4 + Custom.fourDirectionsAndZero[i]))
                            {
                                creature.abstractAI.SetDestination(worldCoordinate4 + Custom.fourDirectionsAndZero[i]);
                                flag = true;
                            }
                        }

                        for (int j = 0; j < star.tentacles.Length; j++)
                        {
                            if (flag)
                            {
                                break;
                            }

                            if (star.tentacles[j].huntCreature != base.preyTracker.MostAttractivePrey || !star.room.aimap.TileAccessibleToCreature(star.tentacles[j].grabPath[0], creature.creatureTemplate))
                            {
                                continue;
                            }

                            for (int k = 0; k < star.tentacles[j].grabPath.Count - 1; k++)
                            {
                                if (flag)
                                {
                                    break;
                                }

                                bool flag2 = false;
                                for (int l = 0; l < 5; l++)
                                {
                                    if (flag2)
                                    {
                                        break;
                                    }

                                    if (base.pathFinder.CoordinateReachableAndGetbackable(star.room.GetWorldCoordinate(star.tentacles[j].grabPath[k + 1] + Custom.fourDirectionsAndZero[l])))
                                    {
                                        flag2 = true;
                                    }
                                }

                                if (!flag2)
                                {
                                    creature.abstractAI.SetDestination(star.room.GetWorldCoordinate(star.tentacles[j].grabPath[k]));
                                    flag = true;
                                }
                            }
                        }

                        if (!flag)
                        {
                            creature.abstractAI.SetDestination(base.preyTracker.MostAttractivePrey.BestGuessForPosition());
                        }
                    }
                }
                this.runSpeed = Mathf.Lerp(this.runSpeed, Mathf.Max(0.8f, 0.3f), 0.1f);
            }
            else if (behavior == Behavior.EscapeRain && base.denFinder.GetDenPosition().HasValue)
            {
                creature.abstractAI.SetDestination(base.denFinder.GetDenPosition().Value);
                this.runSpeed = Mathf.Lerp(this.runSpeed, 1f, 0.1f);
            }

            if (base.tracker.CreaturesCount > 0)
            {
                Tracker.CreatureRepresentation rep = base.tracker.GetRep(Random.Range(0, base.tracker.CreaturesCount));
                if (rep.LowestGenerationAvailable > 100)
                {
                    rep.Destroy();
                }
            }

            base.Update();
        }

        public override void CreatureSpotted(bool firstSpot, Tracker.CreatureRepresentation otherCreature)
        {
            base.CreatureSpotted(firstSpot, otherCreature);
        }

        public override float VisualScore(Vector2 lookAtPoint, float targetSpeed)
        {
            return base.VisualScore(lookAtPoint, targetSpeed);
        }

        public override Tracker.CreatureRepresentation CreateTrackerRepresentationForCreature(AbstractCreature otherCreature)
        {
            if (otherCreature.creatureTemplate.smallCreature)
            {
                return new Tracker.SimpleCreatureRepresentation(base.tracker, otherCreature, 0f, forgetWhenNotVisible: false);
            }

            return new Tracker.ElaborateCreatureRepresentation(base.tracker, otherCreature, 1f, 3);
        }

        AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            if (relationship.type == CreatureTemplate.Relationship.Type.Eats)
            {
                return base.preyTracker;
            }

            return null;
        }

        RelationshipTracker.TrackedCreatureState IUseARelationshipTracker.CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            return null;
        }

        CreatureTemplate.Relationship IUseARelationshipTracker.UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
        {
            if (ModManager.MMF && !star.CheckDaddyConsumption(dRelation.trackerRep.representedCreature.realizedCreature))
            {
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
            }

            return StaticRelationship(dRelation.trackerRep.representedCreature);
        }

        public override PathCost TravelPreference(MovementConnection connection, PathCost cost)
        {
            if (star.stuckPos != null)
            {
                if (connection.destinationCoord.room != star.room.abstractRoom.index)
                {
                    return new PathCost(0f, PathCost.Legality.Unallowed);
                }

                if (!Custom.DistLess(star.room.MiddleOfTile(connection.destinationCoord), star.stuckPos.pos, (star.stuckPos.data as PlacedObject.ResizableObjectData).Rad + 20f))
                {
                    return new PathCost(0f, PathCost.Legality.Unallowed);
                }
            }

            return cost;
        }

        public override bool WantToStayInDenUntilEndOfCycle()
        {
            if (!(behavior == Behavior.EscapeRain))
            {
                return creature.world.rainCycle.TimeUntilRain < 40;
            }

            return true;
        }

        public void ReactToNoise(NoiseTracker.TheorizedSource source, InGameNoise noise)
        {
            if (star.graphicsModule != null)
            {
                (star.graphicsModule as MimicGraphics).ReactToNoise(source, noise);
            }

            
                star.Deafen((int)Custom.LerpMap(noise.strength, 2000f, 8000f, 10f, 200f));
            
        }
        public class LurkTracker : AIModule
        {

            public LurkTracker(ArtificialIntelligence AI, Mimicstarfish star) : base(AI)
            {
                this.star = star;
                this.lurkPosition = star.abstractCreature.pos;
            }

            public override void Update()
            {
                if (this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom != null && this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.readyForAI)
                {
                    IntVector2 intVector = new IntVector2(UnityEngine.Random.Range(0, this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.TileWidth), UnityEngine.Random.Range(0, this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.TileHeight));
                    if (this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.aimap.getAItile(intVector).visibility > this.bestVisLook && this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.VisualContact(this.lurkPosition, this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.ToWorldCoordinate(intVector)))
                    {
                        this.lookPosition = intVector;
                        this.bestVisLook = this.star.room.game.world.GetAbstractRoom(this.lurkPosition).realizedRoom.aimap.getAItile(intVector).visibility;
                    }
                }
                if (Custom.ManhattanDistance(this.star.abstractCreature.pos, this.lurkPosition) > 10 || !this.AI.pathFinder.CoordinateReachable(this.lurkPosition) || !this.AI.pathFinder.CoordinatePossibleToGetBackFrom(this.lurkPosition))
                {
                    WorldCoordinate worldCoordinate = this.star.room.GetWorldCoordinate(new IntVector2(this.star.abstractCreature.pos.Tile.x + UnityEngine.Random.Range(-15, 16), this.star.abstractCreature.pos.Tile.y + UnityEngine.Random.Range(-15, 16)));
                    if (this.LurkPosScore(worldCoordinate) > this.LurkPosScore(this.lurkPosition))
                    {
                        this.lurkPosition = worldCoordinate;
                        this.lookPosition = worldCoordinate.Tile;
                        this.bestVisLook = 0;
                    }
                }
                if (UnityEngine.Random.value < 0.00083333335f)
                {
                    this.lurkPosition = this.star.room.GetWorldCoordinate(new IntVector2(UnityEngine.Random.Range(0, this.star.room.TileWidth), UnityEngine.Random.Range(0, this.star.room.TileHeight)));
                }
            }

            public float LurkPosScore(WorldCoordinate testLurkPos)
            {
                if (!this.star.room.aimap.TileAccessibleToCreature(testLurkPos.Tile, this.star.Template))
                {
                    return -100000f;
                }
                if (this.star.room.GetTile(testLurkPos).Terrain == Room.Tile.TerrainType.Slope)
                {
                    return -100000f;
                }
                if (testLurkPos.room != this.star.abstractCreature.pos.room)
                {
                    return -100000f;
                }
                if (!this.AI.pathFinder.CoordinateReachable(testLurkPos) || !this.AI.pathFinder.CoordinatePossibleToGetBackFrom(testLurkPos))
                {
                    return -100000f;
                }
                float num = 0f;
                
                num = Mathf.Clamp((float)this.star.room.aimap.getAItile(testLurkPos).floorAltitude, 1f, 5f) / (float)this.star.room.aimap.getTerrainProximity(testLurkPos);
                
                int visibility = this.star.room.aimap.getAItile(testLurkPos).visibility;
                num -= (float)visibility / 1000f;
                for (int i = 0; i < 8; i++)
                {
                    if (this.star.room.VisualContact(testLurkPos.Tile, testLurkPos.Tile + Custom.eightDirections[i] * 10))
                    {
                        num += (float)this.star.room.aimap.getAItile(testLurkPos.Tile + Custom.eightDirections[i] * 10).visibility / 8000f;
                    }
                }
                if (this.star.room.aimap.getAItile(testLurkPos).narrowSpace)
                {
                    num -= 10000f;
                }
                for (int j = 0; j < this.AI.tracker.CreaturesCount; j++)
                {
                    if (this.AI.tracker.GetRep(j).BestGuessForPosition().room == testLurkPos.room && !this.AI.tracker.GetRep(j).representedCreature.creatureTemplate.smallCreature && this.AI.tracker.GetRep(j).dynamicRelationship.currentRelationship.type != CreatureTemplate.Relationship.Type.Eats && this.AI.tracker.GetRep(j).BestGuessForPosition().Tile.FloatDist(testLurkPos.Tile) < 20f && this.AI.tracker.GetRep(j).representedCreature.creatureTemplate.bodySize >= this.star.Template.bodySize * 0.8f)
                    {
                        num += this.AI.tracker.GetRep(j).BestGuessForPosition().Tile.FloatDist(testLurkPos.Tile) / 10f;
                    }
                }
                return num;
            }

            public override float Utility()
            {
                
                    return 0.5f;
                
            }

            private Mimicstarfish star;

            public WorldCoordinate lurkPosition;

            public IntVector2 lookPosition;

            public int bestVisLook;
        }
    }

