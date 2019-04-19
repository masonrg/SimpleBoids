using UnityEngine;

namespace BoidsProject.Boids
{
	/// <summary>
	/// Base class for boids - just general stuff.
	/// </summary>
	public abstract class BoidBehaviour : MonoBehaviour
	{
		[Header("Obstacle Avoidance")]
		public bool enableObstacleAvoidance;

		protected BoidController controller;

		protected Animator animator;

		public Vector3 Position
		{
			get { return transform.position; }
			protected set { transform.position = value; }
		}

		public Vector3 Forward
		{
			get { return transform.forward; }
			protected set { transform.forward = value; }
		}

		private Vector3 velocity;
		public Vector3 Velocity
		{
			get { return velocity; }
			protected set { VelocityNormalized = velocity.normalized; velocity = value; }
		}

		public Vector3 VelocityNormalized
		{
			get; protected set;
		}


		public virtual void Init(BoidController controller)
		{
			this.controller = controller;
			this.animator = GetComponent<Animator>();
		}

		protected Vector3 CalculateObstacleAvoidance()
		{
			//look ahead and see if there is an obstacle
			bool obstacleNearby = Physics.CheckSphere(Position, controller.obstacleAwarenessRadius, controller.obstacleLayers);
			if (obstacleNearby)
			{
				//steer away from the collision point
				var cols = Physics.OverlapSphere(Position, controller.obstacleAwarenessRadius, controller.obstacleLayers);
				if (cols != null)
				{
					//avoid the first collider that we find that is in front of us
					foreach (var col in cols)
					{
						var toClosestPoint = col.ClosestPoint(Position) - Position;

						//only avoid it if the obstacle is in front of us.
						if (Vector3.Dot(toClosestPoint, VelocityNormalized) > 0)
						{
							var weight = 1f - Mathf.Clamp01(toClosestPoint.sqrMagnitude / (controller.obstacleAwarenessRadius * controller.obstacleAwarenessRadius));

							return (-toClosestPoint.normalized * weight + VelocityNormalized * (1f - weight));
						}
					}
				}
			}

			return Vector3.zero;
		}

		protected void SetFlapSpeedMultiplier(float maxSpeed)
		{
			var speedMult = Mathf.InverseLerp(0f, maxSpeed, Velocity.magnitude);
			var diveMult = Mathf.Clamp(Vector3.Angle(Vector3.down, VelocityNormalized), 0f, 90f) / 90f;
			animator.SetFloat("FlapSpeedMult", speedMult * diveMult);
		}
	}
}