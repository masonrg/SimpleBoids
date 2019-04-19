using UnityEngine;

namespace BoidsProject.Splines
{
	[RequireComponent(typeof(CatmullRomSpline))]
	public class CatmullRomSplineTester : MonoBehaviour
	{
		public bool runTest;
		public float speedMeterPerSec = 1;

		private float progress;

		private void Update()
		{
			if (!runTest)
				return;

			var spline = GetComponent<CatmullRomSpline>();

			progress += (speedMeterPerSec / spline.SplineEuclideanLength) * Time.deltaTime;
			progress -= (int)progress; //wrap-around


			spline.tracerProgress = progress;
		}
	}
}