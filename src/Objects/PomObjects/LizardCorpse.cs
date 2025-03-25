using SlugBase.SaveData;
using System;
using UnityEngine;
using static Pom.Pom;
using MoreSlugcats;
using RWCustom;

namespace VoidTemplate.Objects.PomObjects;

internal class LizardCorpse : UpdatableAndDeletable
{
	public static void Register() => Pom.Pom.RegisterFullyManagedObjectType([], typeof(LizardCorpse), name: "Corpse", category: "The Void");
	

	ManagedData ManagedData;
	PlacedObject PlacedObject;
	bool needsSpawning;
	
	public LizardCorpse(Room room, PlacedObject pObj)
	{
		ManagedData = (ManagedData)pObj.data;
		PlacedObject = pObj;
		needsSpawning = room.abstractRoom.firstTimeRealized
			&& room.game.GetStorySession.saveState.cycleNumber == 0;
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		if(needsSpawning && room.aimap is not null)
		{
			var pos = room.GetTilePosition(PlacedObject.pos);
			var abscrit = new AbstractCreature(world: room.world,
				creatureTemplate: StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.BlackLizard),
				realizedCreature: null,
				pos: new WorldCoordinate(room.abstractRoom.index, pos.x, pos.y, -1),
				ID: room.game.GetNewID());
			abscrit.saveCreature = false;
			abscrit.Die();
			room.abstractRoom.AddEntity(abscrit);
			abscrit.RealizeInRoom();
			abscrit.realizedCreature.mainBodyChunk.HardSetPosition(PlacedObject.pos);
			needsSpawning = false;
		}
		
	}

}

