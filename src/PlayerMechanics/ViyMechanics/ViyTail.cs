using System;
using System.Linq;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics;

internal static class ViyTail
{
	public static void Hook()
	{
		On.PlayerGraphics.ctor += PlayerGraphics_ctor;
	}

	const float tailChunkFirstRadius = 8f;
	const float tailChunkLastRadius = 2f;
	const float tailChunkFirstConnectionRadius = 2f;
	const float tailChunkLastConnectionRadius = 3.5f;
	const float surfaceFriction = 0.85f;
	const float airFriction = 1f;

	private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
	{
		orig(self, ow);
		if (self.player.IsViy())
		{
			//reversing the effect of default tail
			var bodypartslist = self.bodyParts.ToList();
			foreach (var tailsegment in self.tail)
			{
				bodypartslist.Remove(tailsegment);
			}

			self.tail = new TailSegment[6];
			var tail = self.tail;
			for(int i = 0; i < tail.Length; i++)
			{
				//percentage of going through tail
				float progression = ((float)i)/(tail.Length-1);


				tail[i] = new TailSegment(self,
					//tail chunk radius
					rd: Mathf.Lerp(tailChunkFirstRadius,
						tailChunkLastRadius,
						progression),
					//tail connection size radius
					cnRd: Mathf.Lerp(tailChunkFirstConnectionRadius, 
						tailChunkLastConnectionRadius, 
						progression),
					cnSeg: i == 0 ? null : tail[i - 1],
					sfFric: surfaceFriction,
					aFric: airFriction,
					affectPrevious: i == 0 ? 1f : 0.5f,
					pullInPreviousPosition: true
					);
				bodypartslist.Add(tail[i]);
			}

			self.bodyParts = bodypartslist.ToArray();
        }
	}
}
