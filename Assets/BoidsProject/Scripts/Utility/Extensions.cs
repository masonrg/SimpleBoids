using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BoidsProject.Utility
{
	public static class Extensions
	{
		public static int LoopListIndex<T>(this List<T> list, int index)
		{
			if (index < 0)
			{
				return list.Count - (-index % list.Count);
			}
			else if (index > list.Count - 1)
			{
				return index % list.Count;
			}
			else
			{
				return index;
			}
		}

		public static Pair<Vector3, float> GetVectorAsDirAndMag(this Vector3 v)
		{
			var mag = v.magnitude;
			var dir = mag > 0 ? v / mag : Vector3.zero;
			return new Pair<Vector3, float>(dir, mag);
		}
	}
}