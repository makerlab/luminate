using System.Runtime.InteropServices;
namespace metaio
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3d
	{
		public float x;
		
		public float y;
		
		public float z;

		public Vector3d(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
		

	}
}

