using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.PlayerMechanics.Karma11Foundation;

namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
	public static void Hook()
	{

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
		ExplosiveResist.Hook();
		ExtendedLungs.Hook();
		Grabability.Hook();
		GraspSave.Hook();
		HealthSpear.Hook();
		HeavyCarry.Hook();
		JumpUnderWater.Hook();
		KarmaFlowerChanges.Initiate();
		LizardResist.Hook();
        MalnourishmentDeath.Hook();
		NoForceSleep.Hook();
		NoVisualMalnourishment.Hook();
		PunishNonPermaDeath.Hook();
		RockHitSomething.Hook();
		SaintArenaKarma.Hook();
		SaintArenaSpears.Hook();
		SaintKarmaImmunity.Hook();
		SpearmasterAntiMechanic.Hook();
		SwallowObjects.Hook();
		ThrowObject.Hook();
		ViyMaul.Hook();
	}
}
