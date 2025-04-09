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
		СanMaul.Hook();
		CentipedeResist.Hook();
		Climbing.Hook();
		ColdImmunityPatch.Hook();
		DontBiteMimic.Hook();
		DontEatVoid.Hook();
		DreamManager.RegisterMaps();
		DreamCustom.Hook();
		DreamManager.Hook();
		EdibleChanges.Hook();
		ElectricSpearResist.Hook();
		ExplosiveResist.Hook();
		ExtendedLungs.Hook();
		SlugStats.Hook();
		FrogResist.Hook();
		Grabability.Hook();
		GraspSave.Hook();
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
		PunishNonPermaDeath.Hook();
		RockHitSomething.Hook();
		CrawlJump.Hook();
		SaintArenaKarma.Hook();
		SaintArenaSpears.Hook();
		SaintKarmaImmunity.Hook();
		SpiderResist.Hook();
		SwallowObjects.Hook();
		TardigradeResist.Hook();
		ThrowObject.Hook();
		VoidViolence.Hook();
	}
}
