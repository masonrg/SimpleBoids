using BoidsProject.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsProject.Splines
{
	public class CatmullRomSpline : MonoBehaviour
	{
		[System.Serializable]
		public struct ArcLengthInfo
		{
			public float t;
			public float arcLength;

			public ArcLengthInfo(float t, float arcLength)
			{
				this.t = t;
				this.arcLength = arcLength;
			}
		}


		[Header("Editing")]
		public bool editMode = true;
		[Space()]
		[Range(1, 15)] public int lineResolution = 5;
		public Color lineColor = Color.white;
		[Space()]
		[Range(0, 10)] public float controlPointSize = 1f;
		public Color pointColor = Color.white;
		[Space()]
		[Range(0, 1)] public float tracerProgress;
		[Range(0, 10)] public float uniformTracerSize = 0.75f;
		public Color uniformTracerColor = Color.white;
		[Range(0, 10)] public float geodesicTracerSize = 0.75f;
		public Color geodesicTracerColor = Color.white;


		[Header("Parameters")]
		public bool loop = false;
		[Range(100, 1000)] public int arcLengthTableSize = 100;
		public List<Transform> points;

		public int StartIndex { get { return loop ? 0 : 1; } }
		public int EndIndex { get { return loop ? 0 : points.Count - 2; } }
		public int NumPoints { get { return loop ? points.Count : points.Count - 2; } }


		private ArcLengthInfo[] arcInfo;
		private ArcLengthInfo[] ArcInfo
		{
			get
			{
				if (arcInfo == null || arcInfo.Length != arcLengthTableSize)
				{
					GenerateArcLengthInfoTable();
				}
				return arcInfo;
			}
		}

		public float SplineEuclideanLength
		{
			get { return ArcInfo[arcLengthTableSize - 1].arcLength; }
		}

		private void GenerateArcLengthInfoTable()
		{
			float step = 1f / (arcLengthTableSize - 1);

			arcInfo = new ArcLengthInfo[arcLengthTableSize];

			float distance = 0;
			Vector3 position = GetUniformPosition(0f);
			for (int i = 0; i < arcLengthTableSize; i++)
			{
				float t = Mathf.Clamp01(i * step);
				var nextPosition = GetUniformPosition(t);
				distance += Vector3.Distance(nextPosition, position);
				position = nextPosition;
				arcInfo[i] = new ArcLengthInfo(t, distance);
			}
		}


		private void OnValidate()
		{
		}


		#region Editing
		private void OnDrawGizmos()
		{
			if (!editMode || points == null)
				return;

			//Draw control points
			Gizmos.color = pointColor;
			for (int i = 0; i < points.Count; i++)
			{
				Gizmos.DrawSphere(points[i].position, controlPointSize);
			}

			//Draw lines
			Gizmos.color = lineColor;
			for (int i = 0; i < points.Count; i++)
			{
				if (!IsValidIndex(i))
					continue;

				DrawCatmullRomSpline(i);
			}

			//Uniform Tracer
			Gizmos.color = uniformTracerColor;
			Gizmos.DrawSphere(GetUniformPosition(tracerProgress), uniformTracerSize);

			GenerateArcLengthInfoTable();

			//Geodesic Tracer
			Gizmos.color = geodesicTracerColor;
			Gizmos.DrawSphere(GetGeodesicPositionByPercentage(tracerProgress), geodesicTracerSize);
		}

		void DrawCatmullRomSpline(int index)
		{
			Vector3[] P = GetSplinePoints(index);

			Vector3 lastPos = P[1];
			var segmentLen = 1f / lineResolution;

			for (int i = 1; i <= lineResolution; i++)
			{
				float t = i * segmentLen;
				Vector3 newPos = ComputeCatmullRom(t, P[0], P[1], P[2], P[3]);

				Gizmos.DrawLine(lastPos, newPos);
				lastPos = newPos;
			}
		}
		#endregion

		#region	Interpolation 
		public Vector3 GetUniformPosition(float t)
		{
			float t0 = t * (NumPoints - StartIndex);
			int index = StartIndex + (int)t0;
			float lerp = t0 - (int)t0;

			Vector3[] P = GetSplinePoints(index);

			var step = 1f / (NumPoints - StartIndex);
			var min = step * (index - StartIndex);
			var max = step * (index - StartIndex + 1);

			return ComputeCatmullRom(lerp, P[0], P[1], P[2], P[3]);
		}

		public Vector3 GetGeodesicPositionByPercentage(float percent)
		{
			return GetGeodesicPositionByDistance(SplineEuclideanLength * percent);
		}

		public Vector3 GetGeodesicPositionByDistance(float distance)
		{
			return GetUniformPosition(TFromDistance(distance));
		}



		private Vector3 ComputeCatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			/// Naming:
			///		p1 => the start node
			///		p2 => the end node
			///		p0 => the node before the start
			///		p3 => the node after the end

			var tt = t * t; //the square term
			var ttt = tt * t; //the cubic term

			var f0 = (-0.5f * ttt) + tt - (0.5f * t);
			var f1 = (1.5f * ttt) + (-2.5f * tt) + 1.0f;
			var f2 = (-1.5f * ttt) + (2.0f * tt) + (0.5f * t);
			var f3 = (0.5f * ttt) - (0.5f * tt);

			return p0 * f0 + p1 * f1 + p2 * f2 + p3 * f3;
		}
		#endregion



		private float TFromDistance(float distance)
		{
			int index = 0;
			for (int i = 0; i < ArcInfo.Length - 1; i++)
			{
				if (distance <= ArcInfo[i + 1].arcLength)
				{
					index = i;
					break;
				}
			}

			//Debug.Log(string.Format("AI0: {0} | AI1: {1} | d: {2}", ArcInfo[index].arcLength, ArcInfo[index + 1].arcLength, distance));
			float lerp = Mathf.InverseLerp(ArcInfo[index].arcLength, ArcInfo[index + 1].arcLength, distance);
			return Mathf.Lerp(ArcInfo[index].t, ArcInfo[index + 1].t, lerp);
		}




		private Vector3[] GetSplinePoints(int index)
		{
			return new Vector3[]
			{
			points[points.LoopListIndex(index - 1)].position,
			points[points.LoopListIndex(index + 0)].position,
			points[points.LoopListIndex(index + 1)].position,
			points[points.LoopListIndex(index + 2)].position
			};
		}

		private bool IsValidIndex(int i)
		{
			return loop || (i > 0 && i < points.Count - 2);
		}
	}
}