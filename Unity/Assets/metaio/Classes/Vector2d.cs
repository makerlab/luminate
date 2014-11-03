using System.Runtime.InteropServices;

namespace metaio
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector2d
	{
		public float x;
		
		public float y;

		public Vector2d(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
		

	}
}

