using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate;


public class DartAI : BigSpiderAI
{
    public DartAI(AbstractCreature creature, World world)
        : base(creature, world)
    {
        
        arenaMode = world.game.IsArenaSession;
        bug = creature.realizedCreature as BigSpider;
        bug.AI = this;
        AddModule(new StandardPather(this, world, creature));
        base.pathFinder.stepsPerFrame = 15;
        base.pathFinder.accessibilityStepsPerFrame = 15;
        AddModule(new Tracker(this, 10, 10, 1500, 0.5f, 5, 5, 10));
        AddModule(new ThreatTracker(this, 3));
        AddModule(new PreyTracker(this, 5, 2f, 10f, 70f, 0.5f));
        AddModule(new RainTracker(this));
        AddModule(new DenFinder(this, creature));
        AddModule(new StuckTracker(this, trackPastPositions: true, trackNotFollowingCurrentGoal: false));
        AddModule(new NoiseTracker(this, base.tracker));
        AddModule(new UtilityComparer(this));
        AddModule(new RelationshipTracker(this, base.tracker));
        
            spitModule = new DartSpitModule(this);
            AddModule(spitModule); 
            spitModule.ammo = 2;
        FloatTweener.FloatTween smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.5f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.005f));
        base.utilityComparer.AddComparedModule(base.threatTracker, smoother, 1f, 1.1f);
        smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.2f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.01f));
        base.utilityComparer.AddComparedModule(base.preyTracker, smoother, 0.65f, 1.1f);
        base.utilityComparer.AddComparedModule(base.rainTracker, null, 1f, 1.1f);
        base.utilityComparer.AddComparedModule(base.stuckTracker, null, 0.4f, 1.1f);
        lightThreats = new List<LightThreat>();
        behavior = Behavior.Idle;
        previdlePositions = new List<WorldCoordinate>();
    }
    public class DartSpitModule : SpiderSpitModule
    {
        public DartSpitModule(BigSpiderAI AI) : base(AI)
        {
            this.bugAI = AI;
            this.targetChunk = UnityEngine.Random.Range(0, 100);
            this.targetTrail = new List<Vector2>();
        }
        public override void Update()
        {
            base.Update();
            if (this.taggedCreature != null && (this.taggedCreature.dynamicRelationship.state as BigSpiderAI.SpiderTrackState).tagged < 1)
            {
                Custom.Log(new string[]
                {
                        "untag target"
                });
                this.taggedCreature = null;
            }
            if (this.ammo < 2)
            {
                this.ammoRegen += 1f / (this.fastAmmoRegen ? 80f : 160f);
                if (this.ammoRegen > 1f)
                {
                    this.ammo++;
                    this.ammoRegen -= 1f;
                    if (this.ammo > 1)
                    {
                        this.ammoRegen = 0f;
                        this.fastAmmoRegen = false;
                        this.bugAI.stayAway = false;
                    }
                }
            }
        }
    }
}
