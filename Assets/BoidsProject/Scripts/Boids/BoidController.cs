using BoidsProject.Splines;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsProject.Boids
{
	[System.Serializable]
	public struct BoundaryZone
	{
		public Vector3 Min;
		public Vector3 Max;

		public Vector3 Center
		{
			get { return Vector3.Lerp(Min, Max, 0.5f); }
		}

		public Vector3 RandomVolume
		{
			get
			{
				return new Vector3(
					Mathf.Lerp(Min.x, Max.x, Random.value),
					Mathf.Lerp(Min.y, Max.y, Random.value),
					Mathf.Lerp(Min.z, Max.z, Random.value));
			}
		}
	}

	public class BoidController : MonoBehaviour
	{
		public enum SpawnMode { Origin, OnLeader, Random }

		[Header("References")]
		public GameObject boidPrefab;
		public GameObject predatorPrefab;
		public GameObject leaderPrefab;
		public Transform boidContainer;
		public List<Boid> boidList = new List<Boid>();
		public List<BoidPredator> predatorList = new List<BoidPredator>();

		[Header("General Settings")]
		public BoundaryZone boundary;
		public SpawnMode spawnMode;
		public int boidCount = 10;
		public int predatorCount = 1;
		public Vector3 gravityForce = new Vector3(0, -9.81f, 0);

		[Header("Boid Parameters")]
		public float viewRadius = 5;
		public float collideRadius = 1f;
		public float avoidancePredictTime = 0.5f;
		public float evasionPredictTime = 0.5f;
		public float maxAvailableThrust = 5;
		public float speed = 1;
		public float maxSpeed = 10;
		public float obstacleAwarenessRadius = 8f;
		public LayerMask obstacleLayers;

		[Header("Boid Behaviour Weights")]
		[Range(0, 1)] public float randomnessWeight;
		[Range(0, 1)] public float obedienceWeight;
		[Range(0, 1)] public float avoidanceWeight;
		[Range(0, 1)] public float alignmentWeight;
		[Range(0, 1)] public float cohesionWeight;
		[Range(0, 1)] public float evasionWeight;
		[Space()]
		[Header("Boid Relative Weights (read-only)")]
		[Range(0, 1)] public float randomnessWeightNorm;
		[Range(0, 1)] public float obedienceWeightNorm;
		[Range(0, 1)] public float avoidanceWeightNorm;
		[Range(0, 1)] public float alignmentWeightNorm;
		[Range(0, 1)] public float cohesionWeightNorm;
		[Range(0, 1)] public float evasionWeightNorm;


		public float ViewRadiusSqr { get; private set; }
		public float CollideRadiusSqr { get; private set; }
		public Transform LeaderTransform { get; private set; }
		public Vector3 LeaderPosition { get { return LeaderTransform.position; } }

		private void OnValidate()
		{
			var sum = obedienceWeight + avoidanceWeight + alignmentWeight + cohesionWeight + randomnessWeight + evasionWeight;
			obedienceWeightNorm = obedienceWeight / sum;
			avoidanceWeightNorm = avoidanceWeight / sum;
			alignmentWeightNorm = alignmentWeight / sum;
			cohesionWeightNorm = cohesionWeight / sum;
			randomnessWeightNorm = randomnessWeight / sum;
			evasionWeightNorm = evasionWeight / sum;
		}

		private void Start()
		{
			ViewRadiusSqr = viewRadius * viewRadius;
			CollideRadiusSqr = collideRadius * collideRadius;

			//spawn leader
			var leaderObj = SpawnLeader();
			leaderObj.name = "leader";
			var leader = leaderObj.GetComponent<BoidLeader>();
			leader.Init(this);
			leader.spline = FindObjectOfType<CatmullRomSpline>();
			LeaderTransform = leader.transform;

			//Spawn boids
			boidList.Clear();
			for (int i = 0; i < boidCount; i++)
			{
				var boidObj = SpawnBoid(spawnMode);
				boidObj.name = "boid_" + i;
				var boid = boidObj.GetComponent<Boid>();
				boid.Init(this);
				boidList.Add(boid);
			}

			//Spawn predators
			predatorList.Clear();
			for (int i = 0; i < predatorCount; i++)
			{
				var predatorObj = SpawnPredator(new Vector3(0, 0, boundary.Min.z - 5));
				predatorObj.name = "predator_" + i;
				var predator = predatorObj.GetComponent<BoidPredator>();
				predator.Init(this);
				predatorList.Add(predator);
			}
		}

		private int replacementBoidId = 0;
		private void Update()
		{
			if (predatorList == null || predatorList.Count == 0)
				return;

			//replace dead boids!
			if (boidList.Count < boidCount)
			{
				var boidObj = SpawnBoid(SpawnMode.OnLeader);
				boidObj.name = "boid_respawned_" + replacementBoidId;
				var boid = boidObj.GetComponent<Boid>();
				boid.Init(this);
				boidList.Add(boid);
				replacementBoidId++;
			}
		}




		#region Helpers
		private GameObject SpawnBoid(SpawnMode spawnMode)
		{
			Vector3 spawnPosition = Vector3.zero;
			if (spawnMode == SpawnMode.Origin)
			{
				spawnPosition = boundary.Center;
			}
			else if (spawnMode == SpawnMode.Random)
			{
				spawnPosition = boundary.RandomVolume;
			}
			else if (LeaderTransform != null) //on leader
			{
				spawnPosition = LeaderTransform.position;
			}

			return Instantiate(boidPrefab, spawnPosition, Quaternion.identity, boidContainer);
		}

		private GameObject SpawnPredator(Vector3 position)
		{
			return Instantiate(predatorPrefab, position + Vector3.one * Random.value, Quaternion.identity, boidContainer);
		}

		private GameObject SpawnLeader()
		{
			return Instantiate(leaderPrefab, transform.position, Quaternion.identity, boidContainer);
		}
		#endregion
	}
}