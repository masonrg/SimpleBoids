
namespace BoidsProject.Utility
{
	[System.Serializable]
	public struct Pair<T1, T2>
	{
		public T1 first;
		public T2 second;
		public Pair(T1 first, T2 second)
		{
			this.first = first;
			this.second = second;
		}
		public bool IsNull
		{
			get { return (first.Equals(default(T1)) && second.Equals(default(T2))); }
		}
	}
}