using System.Collections.Generic;
using UnityEngine;

namespace BoidsProject.Boids
{
	public static class BoidUtility
	{
		public static Vector3 GetAveragePosition<TBoid>(IEnumerable<TBoid> set) where TBoid : BoidBehaviour
		{
			if (set == null)
				return Vector3.zero;

			Vector3 avgPosition = Vector3.zero;
			bool first = true;
			foreach (var boid in set)
			{
				avgPosition = Vector3.Lerp(first ? boid.Position : avgPosition, boid.Position, 0.5f);
				first = false;
			}

			return avgPosition;
		}

		public static Vector3 GetAverageHeading<TBoid>(IEnumerable<TBoid> set, Vector3 position, float considerationRadiusSqr, Vector3 headingSeed = default(Vector3)) where TBoid : BoidBehaviour
		{
			if (set == null)
				return Vector3.zero;

			Vector3 avgHeading = headingSeed;
			foreach (var boid in set)
			{
				var toBoidDistSqr = (boid.Position - position).sqrMagnitude;
				float distanceWeight = 1f - (toBoidDistSqr / considerationRadiusSqr); //we weight nearby boids more heavily
				avgHeading += boid.VelocityNormalized * distanceWeight;
			}

			return avgHeading.normalized;
		}

		public static TBoid GetNearest<TBoid>(IEnumerable<TBoid> set, Vector3 position) where TBoid : BoidBehaviour
		{
			if (set == null)
				return null;

			TBoid currBoid = null;
			float currMinDist = float.MaxValue;
			foreach (var boid in set)
			{
				var sqrDist = (boid.Position - position).sqrMagnitude;
				if (sqrDist < currMinDist)
				{
					currBoid = boid;
					currMinDist = sqrDist;
				}
			}

			return currBoid;
		}
	}
}