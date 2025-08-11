using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.PlayerMechanics.Karma11Foundation;
using VoidTemplate.PlayerMechanics.ViyMechanics;

namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
	public static void Hook()
	{ 
		BarnacleResist.Hook();
		BasiliskResist.Hook();
		BitByPlayer.Hook();
		CanBeSwallowed.Hook();
		CanIPickThisUp.Hook();
        CanMaul.Hook();
		CentipedeResist.Hook();
		Climbing.Hook();
		ColdImmunityPatch.Hook();
		Crawl.Hook();
		CreatureGrab.Hook();
		CreatureReleaseGrasp.Hook();
		MovementUpdate.Hook();
		DontBiteMimic.Hook();
		DontEatVoid.Hook();
		DreamManager.RegisterMaps();
        DropCarriedObject.Hook();
        DreamCustom.Hook();
		DreamManager.Hook();
		EdibleChanges.Hook();
		ElectricSpearResist.Hook();
		ExplosiveResist.Hook();
		ExtendedLungs.Hook();
		SlugStats.Hook();
		FrogResist.Hook();
		Grabability.Hook();
		GrabUpdate.Hook();
		GraspSave.Hook();
		HarmfulSteam.Hook();
		HealthSpear.Hook();
		HeavyCarry.Hook();
        ImmuneToFallDamage.Hook();
        JellyResist.Hook();
		JumpUnderWater.Hook();
		KarmaFlowerChanges.Initiate();
		LizardResist.Hook();
		LocustResist.Hook();
        MalnourishmentDeath.Hook();
		NoForceSleep.Hook();
		NoVisualMalnourishment.Hook();
		PilgrimPasage.Hook();
		PlayerGrabbed.Hook();
		RockHitSomething.Hook();
		RottenDangleResist.Hook();
		RottenSeedCob.Hook();
		CrawlJump.Hook();
		SaintArenaKarma.Hook();
		SaintArenaSpears.Hook();
		SaintKarmaImmunity.Hook();
        Sandstorm.Hook();
        SlugcatGrab.Hook();
		Spasm.Hook();
		SpiderResist.Hook();
		SwallowObjects.Hook();
		TardigradeResist.Hook();
		ThrowObject.Hook();
	}
}
