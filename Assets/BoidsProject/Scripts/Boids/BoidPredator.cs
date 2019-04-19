using BoidsProject.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsProject.Boids
{
	public class BoidPredator : BoidBehaviour
	{
		[Header("Debug")]
		public bool showDebug = false;

		[Header("Settings")]
		public bool enableBehaviour = true;
		public float speed = 10;
		public float maxAvailableThrust = 5;
		public float maxSpeed = 20f;
		public float viewRadius = 8f;
		public float attackRadius = 3f;
		public float captureRadius = 0.25f;
		public float withdrawalStartTime = 4f;
		public float withdrawalEndTime = 8f;

		[Header("Behaviour Weights")]
		[Range(0, 1)] public float separationWeight;
		[Range(0, 1)] public float disruptionWeight;
		[Range(0, 1)] public float eagernessWeight;
		[Range(0, 1)] public float withdrawalWeight;
		[Range(0, 1)] public float independenceWeight;
		[Space()]
		[Header("Relative Weights (read-only)")]
		[Range(0, 1)] public float separationWeightNorm;
		[Range(0, 1)] public float disruptionWeightNorm;
		[Range(0, 1)] public float eagernessWeightNorm;
		[Range(0, 1)] public float withdrawalWeightNorm;
		[Range(0, 1)] public float independenceWeightNorm;

		[Header("Stats")]
		public int killCount = 0;

		private HashSet<Boid> targetGroup = new HashSet<Boid>();
		private HashSet<Boid> fringeGroup = new HashSet<Boid>();

		private Vector3 flockSeparation;
		private Vector3 groupDisruption;
		private Vector3 attackEagerness;
		private Vector3 attemptWithdrawal;
		private Vector3 huntIndependence;

		private Vector3 totalDemand;
		private float timeSinceAttackStarted;

		private float viewRadiusSqr;
		private float attackRadiusSqr;
		private float captureRadiusSqr;

		public override void Init(BoidController controller)
		{
			base.Init(controller);

			viewRadiusSqr = viewRadius * viewRadius;
			attackRadiusSqr = attackRadius * attackRadius;
			captureRadiusSqr = captureRadius * captureRadius;

			killCount = 0;
		}

		private void OnValidate()
		{
			var sum = separationWeight + disruptionWeight + eagernessWeight + withdrawalWeight + independenceWeight;
			separationWeightNorm = separationWeight / sum;
			disruptionWeightNorm = disruptionWeight / sum;
			eagernessWeightNorm = eagernessWeight / sum;
			withdrawalWeightNorm = withdrawalWeight / sum;
			independenceWeightNorm = independenceWeight / sum;
		}

		private void OnDrawGizmosSelected()
		{
			if (!showDebug)
				return;

			//Targets
			Gizmos.color = Color.red;

			///draw average position of target group
			var avgTargetPos = BoidUtility.GetAveragePosition(targetGroup);
			if (avgTargetPos != Vector3.zero)
				Gizmos.DrawSphere(avgTargetPos, 0.33f);

			///draw position of each target
			foreach (var b in targetGroup)
				Gizmos.DrawWireSphere(b.Position, 0.25f);

			//Fringes
			Gizmos.color = Color.yellow;

			///draw average position of fringe group
			var avgFringePos = BoidUtility.GetAveragePosition(fringeGroup);
			if (avgFringePos != Vector3.zero)
				Gizmos.DrawSphere(avgFringePos, 0.33f);

			///draw position of each fringe boid
			foreach (var b in fringeGroup)
				Gizmos.DrawWireSphere(b.Position, 0.25f);

			//Withdrawal
			float currentWithdrawlWeighting = Mathf.InverseLerp(withdrawalStartTime, withdrawalEndTime, timeSinceAttackStarted);
			if (currentWithdrawlWeighting <= 0.5f)
				Gizmos.color = Color.Lerp(Color.green, Color.yellow, currentWithdrawlWeighting * 2);
			else
				Gizmos.color = Color.Lerp(Color.yellow, Color.red, 2 * currentWithdrawlWeighting - 1f);

			Gizmos.DrawCube(Position + Vector3.up * 0.2f, Vector3.one * 0.4f);

			var separation = flockSeparation * separationWeightNorm;
			var disruption = groupDisruption * disruptionWeightNorm;
			var eagerness = attackEagerness * eagernessWeightNorm;
			var withdrawal = attemptWithdrawal * withdrawalWeightNorm;
			var independence = huntIndependence * independenceWeightNorm;

			Gizmos.color = Color.white;
			Gizmos.DrawLine(Position, Position + separation * 5);
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(Position, Position + disruption * 5);
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(Position, Position + eagerness * 5);
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(Position, Position + withdrawal * 5);
			Gizmos.color = Color.black;
			Gizmos.DrawLine(Position, Position + independence * 5);
		}


		private void Update()
		{
			if (!enableBehaviour)
				return;

			/// High-level steps:
			///  1) Separation - separate the target group from flock members on the periphery of the group
			///  1) Disruption - steer towards the center of the group to disrupt cohesion
			///  2) Eagerness - steer towards nearest agent for increased attack opportunity
			///  3) Withdrawal - steer away from the target group if we think we are not going to succeed so we can reassess the flock and try again

			UpdateActiveGroups(controller.boidList);

			float currentWithdrawlWeighting = Mathf.InverseLerp(withdrawalStartTime, withdrawalEndTime, timeSinceAttackStarted);

			flockSeparation = CalculateFlockSeparation(fringeGroup, currentWithdrawlWeighting);
			groupDisruption = CalculateGroupDisruption(targetGroup, currentWithdrawlWeighting);
			attackEagerness = CalculateAttackEagerness(targetGroup, currentWithdrawlWeighting);
			attemptWithdrawal = CalculateAttemptWithdrawal(controller.boidList, currentWithdrawlWeighting);
			huntIndependence = CalculateIndependence(controller.predatorList);

			totalDemand =
				flockSeparation * separationWeight +
				groupDisruption * disruptionWeight +
				attackEagerness * eagernessWeight +
				attemptWithdrawal * withdrawalWeight +
				huntIndependence * independenceWeight +
				CalculateObstacleAvoidance();

			var availableThrust = maxAvailableThrust;
			var demandComps = totalDemand.GetVectorAsDirAndMag();

			if (demandComps.second > maxAvailableThrust)
				demandComps.second = maxAvailableThrust;

			Velocity += demandComps.first * demandComps.second;

			var comps = Velocity.GetVectorAsDirAndMag();
			if (comps.second > maxSpeed)
			{
				Velocity = comps.first * maxSpeed;
			}

			var deltaT = Time.deltaTime;
			Position += Velocity * deltaT * speed;
			if (VelocityNormalized != Vector3.zero)
				Forward = VelocityNormalized;

			timeSinceAttackStarted += deltaT;

			if (timeSinceAttackStarted > withdrawalEndTime)
			{
				timeSinceAttackStarted = 0;
			}


			//Check if we caught our target!
			CheckForTargetCapture();

			SetFlapSpeedMultiplier(maxSpeed);
		}


		private bool CheckForTargetCapture()
		{
			if (targetGroup == null || targetGroup.Count == 0)
				return false;

			var nearestBoid = BoidUtility.GetNearest(targetGroup, Position);
			if ((nearestBoid.Position - Position).sqrMagnitude <= captureRadiusSqr)
			{
				nearestBoid.Kill();
				killCount++;
				return true;
			}

			return false;
		}




		/// <summary>
		/// Assigning the target group and the fringe group
		/// </summary>
		private void UpdateActiveGroups(List<Boid> boidList)
		{
			targetGroup.Clear();
			fringeGroup.Clear();

			if (boidList == null || boidList.Count == 0)
				return;

			//find the nearest boid and form the target group from it and its neighbours
			Boid nearestBoid = BoidUtility.GetNearest(boidList, Position);

			targetGroup.Add(nearestBoid);
			foreach (var boid in nearestBoid.Neighbours)
			{
				targetGroup.Add(boid);
			}

			//form the fringe group from the neighbours of the target group that arent in the target group
			foreach (var boid in targetGroup)
			{
				foreach (var neighbour in boid.Neighbours)
				{
					if (!targetGroup.Contains(neighbour) && !fringeGroup.Contains(neighbour))
					{
						fringeGroup.Add(neighbour);
					}
				}
			}
		}



		/// <summary>
		/// Vector towards the the average position of the fringes.
		/// By directing to the position of the fringe group, we act to cut off
		/// the target group from reconnecting with the main flock.
		/// </summary>
		private Vector3 CalculateFlockSeparation(HashSet<Boid> fringes, float currentWithdrawalWeight)
		{
			if (fringes == null || fringes.Count == 0)
				return Vector3.zero;

			return (BoidUtility.GetAveragePosition(fringes) - Position).normalized * (1f - currentWithdrawalWeight);
		}

		/// <summary>
		/// Vector towards the average position of the targets.
		/// By directing to the center of the group, we can achieve maximum
		/// fragmentation of the group and hopefully futher isolate target boids.
		/// </summary>
		private Vector3 CalculateGroupDisruption(HashSet<Boid> targets, float currentWithdrawalWeight)
		{
			if (targets == null || targets.Count == 0)
				return Vector3.zero;

			return (BoidUtility.GetAveragePosition(targets) - Position).normalized * (1f - currentWithdrawalWeight);
		}

		/// <summary>
		/// Vector towards the nearest target.
		/// This is the actual attack of a particular boid. As we get closer to a boid,
		/// our tendency to attack it increases.
		/// </summary>
		private Vector3 CalculateAttackEagerness(HashSet<Boid> targets, float currentWithdrawalWeight)
		{
			if (targets == null || targets.Count == 0)
				return Vector3.zero;

			var toNearestBoid = BoidUtility.GetNearest(targets, Position).Position - Position;
			var weight = 1f - Mathf.Clamp01(toNearestBoid.sqrMagnitude / attackRadiusSqr); //weight nearer boids most heavily

			return toNearestBoid.normalized * weight * (1f - currentWithdrawalWeight);
		}

		/// <summary>
		/// Vector away from the average heading of the target group.
		/// We use a timer to track how long we have been attempting an attack, and based on this we
		/// decide how strongly we should be withdrawing.
		/// </summary>
		private Vector3 CalculateAttemptWithdrawal(List<Boid> boids, float currentWithdrawalWeight)
		{
			if (boids == null || boids.Count == 0)
				return Vector3.zero;

			return (Position - BoidUtility.GetAveragePosition(boids)).normalized * currentWithdrawalWeight;
		}

		/// <summary>
		/// Vector away from other predators.
		/// This prevents the predator from targeting boids that are also
		/// being targeted by other predators.
		/// </summary>
		private Vector3 CalculateIndependence(List<BoidPredator> predators)
		{
			HashSet<BoidPredator> others = new HashSet<BoidPredator>(predators);
			others.Remove(this);

			if (others.Count > 0)
			{
				var toNearestPred = BoidUtility.GetNearest(others, Position).Position - Position;
				var weight = 1f - Mathf.Clamp01(toNearestPred.sqrMagnitude / viewRadius); //weight nearer predators most heavily
				return -toNearestPred.normalized * weight;
			}

			return Vector3.zero;
		}
	}
}