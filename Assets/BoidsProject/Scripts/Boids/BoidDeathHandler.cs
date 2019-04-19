
namespace BoidsProject.Boids
{
	public class BoidDeathHandler
	{
		public bool IsDead { get; private set; }

		private const float duration = 5;
		private float timeSinceDeath;

		public void Activate()
		{
			IsDead = true;
			timeSinceDeath = 0f;
		}

		public bool CheckDeathTimer(float deltaTime)
		{
			timeSinceDeath += deltaTime;
			return timeSinceDeath > duration;
		}
	}
}