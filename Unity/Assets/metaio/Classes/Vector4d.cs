using System.Runtime.InteropServices;

namespace metaio
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector4d
	{
		public float x;
		
		public float y;
		
		public float z;
		
		public float w;

		public Vector4d(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
		

	}
}

