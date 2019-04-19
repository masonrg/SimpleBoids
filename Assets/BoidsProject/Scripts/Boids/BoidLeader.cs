using BoidsProject.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsProject.Boids
{
	public class BoidLeader : BoidBehaviour
	{
		public CatmullRomSpline spline;

		public bool enableMovement;
		public float speedMeterPerSec = 1;

		private float distanceTravelled;

		private void Update()
		{
			if (!enableMovement)
				return;

			distanceTravelled = WrapDistanceOverSpline(distanceTravelled + speedMeterPerSec * Time.deltaTime);

			Position = spline.GetGeodesicPositionByDistance(distanceTravelled);

			//face the direction of motion
			var forwardPos = spline.GetGeodesicPositionByDistance(WrapDistanceOverSpline(distanceTravelled + 0.01f));
			Forward = forwardPos - transform.position;
		}


		private float WrapDistanceOverSpline(float distance)
		{
			if (distance > spline.SplineEuclideanLength)
				return distance - spline.SplineEuclideanLength;
			else if (distance < 0)
				return spline.SplineEuclideanLength - Mathf.Abs(distance);
			else
				return distance;
		}
	}
}