using BoidsProject.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsProject.Boids
{
	public class Boid : BoidBehaviour
	{
		[Header("Debug")]
		public bool showDebug = false;

		private List<Boid> neighbours = new List<Boid>();
		public List<Boid> Neighbours { get { return neighbours; } }

		private Vector3 followObedience;
		private Vector3 collisionAvoidance;
		private Vector3 velocityAlignment;
		private Vector3 flockCohesion;
		private Vector3 randomVariance;
		private Vector3 predatorEvasion;

		private Vector3 totalDemand;

		private BoidDeathHandler deathHandler = new BoidDeathHandler();

		private void OnDrawGizmosSelected()
		{
			if (!showDebug)
				return;

			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Position, controller.viewRadius);

			Gizmos.color = Color.red;
			var avgPos = Position;
			foreach (var b in neighbours)
			{
				avgPos = Vector3.Lerp(avgPos, b.Position, 0.5f);
				Gizmos.DrawWireSphere(b.Position, controller.collideRadius);
			}

			Gizmos.DrawSphere(avgPos, 0.33f);

			var obedience = followObedience * controller.obedienceWeightNorm;
			var avoidance = collisionAvoidance * controller.avoidanceWeightNorm;
			var alignment = velocityAlignment * controller.alignmentWeightNorm;
			var cohesion = flockCohesion * controller.cohesionWeightNorm;
			var random = randomVariance * controller.randomnessWeightNorm;
			var evasion = predatorEvasion * controller.evasionWeightNorm;

			Gizmos.color = Color.white;
			Gizmos.DrawLine(Position, Position + obedience * 5);
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(Position, Position + avoidance * 5);
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(Position, Position + alignment * 5);
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(Position, Position + cohesion * 5);
			Gizmos.color = Color.black;
			Gizmos.DrawLine(Position, Position + random * 5);
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(Position, Position + evasion * 5);
		}


		private void Update()
		{
			var deltaT = Time.deltaTime;

			if (deathHandler.IsDead)
			{
				Velocity += controller.gravityForce * deltaT;

				if (deathHandler.CheckDeathTimer(deltaT))
				{
					Destroy(gameObject);
					return;
				}
			}
			else
			{
				neighbours = GetNeighbours();

				followObedience = CalculateObedience();
				collisionAvoidance = CalculateAvoidance(neighbours);
				velocityAlignment = CalculateAlignment(neighbours);
				flockCohesion = CalculateCohesion(neighbours);
				randomVariance = CalculateRandomness();
				predatorEvasion = CalculateEvasion(controller.predatorList);

				totalDemand =
					followObedience * controller.obedienceWeight +
					collisionAvoidance * controller.avoidanceWeight +
					velocityAlignment * controller.alignmentWeight +
					flockCohesion * controller.cohesionWeight +
					randomVariance * controller.randomnessWeight +
					predatorEvasion * controller.evasionWeight +
					CalculateObstacleAvoidance();



				var availableThrust = controller.maxAvailableThrust;
				var demandComps = totalDemand.GetVectorAsDirAndMag();

				if (demandComps.second > controller.maxAvailableThrust)
					demandComps.second = controller.maxAvailableThrust;

				Velocity += demandComps.first * demandComps.second;
			}


			var comps = Velocity.GetVectorAsDirAndMag();
			if (comps.second > controller.maxSpeed)
			{
				Velocity = comps.first * controller.maxSpeed;
			}

			Position += Velocity * deltaT * controller.speed;
			if (VelocityNormalized != Vector3.zero)
				Forward = VelocityNormalized;

			//Wing Flap Animation
			SetFlapSpeedMultiplier(controller.maxSpeed);
		}


		/// <summary>
		/// Called by a predator if they catch us.
		/// </summary>
		public void Kill()
		{
			deathHandler.Activate();
			controller.boidList.Remove(this);
		}





		/// <summary>
		/// Acquire Neighbours
		/// </summary>
		private List<Boid> GetNeighbours()
		{
			neighbours.Clear();
			foreach (var boid in controller.boidList)
			{
				if (boid == this) continue;

				Vector3 toBoid = boid.Position - Position;
				float toBoidSqrDist = toBoid.sqrMagnitude;

				if (toBoidSqrDist <= controller.ViewRadiusSqr)
				{
					neighbours.Add(boid);
				}
			}
			return neighbours;
		}

		/// <summary>
		/// Leader Follow Obedience
		/// </summary>
		private Vector3 CalculateObedience()
		{
			return (controller.LeaderPosition - Position).normalized;
		}

		/// <summary>
		/// Random Variance
		/// </summary>
		private Vector3 CalculateRandomness()
		{
			return Random.insideUnitSphere;
		}

		/// <summary>
		/// Collision Avoidance
		/// </summary>
		private Vector3 CalculateAvoidance(List<Boid> neighbours)
		{
			Vector3 adjustment = Vector3.zero;

			var myFuturePosition = Position + Velocity * controller.avoidancePredictTime;
			foreach (var boid in neighbours)
			{
				Vector3 boidFuturePosition = boid.Position + boid.Velocity * controller.avoidancePredictTime;
				Vector3 toBoidFuture = boidFuturePosition - myFuturePosition;
				float toBoidFutureSqrDist = toBoidFuture.sqrMagnitude;
				if (toBoidFutureSqrDist <= controller.CollideRadiusSqr)
				{
					adjustment += -toBoidFuture.normalized;
				}
			}

			return adjustment.normalized;
		}

		/// <summary>
		/// Velocity Alignment
		/// </summary>
		private Vector3 CalculateAlignment(List<Boid> neighbours)
		{
			return BoidUtility.GetAverageHeading(neighbours, Position, controller.ViewRadiusSqr, VelocityNormalized);
		}

		/// <summary>
		/// Flock Cohesion
		/// </summary>
		private Vector3 CalculateCohesion(List<Boid> neighbours)
		{
			return (Vector3.Lerp(BoidUtility.GetAveragePosition(neighbours), Position, 0.5f) - Position).normalized;
		}

		/// <summary>
		/// Predator Evasion
		/// </summary>
		private Vector3 CalculateEvasion(List<BoidPredator> predators)
		{
			Vector3 adjustment = Vector3.zero;

			foreach (var predator in predators)
			{
				Vector3 predatorFuturePosition = predator.Position + predator.Velocity * controller.evasionPredictTime;
				Vector3 predatorApproach = predatorFuturePosition - Position;
				float sqrDist = predatorApproach.sqrMagnitude;
				if (sqrDist <= controller.ViewRadiusSqr)
				{
					adjustment += -predatorApproach.normalized;
				}
			}

			return adjustment.normalized;

		}
	}
}