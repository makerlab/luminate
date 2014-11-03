using System.Runtime.InteropServices;

namespace metaio
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector2di
	{
		public int x;
		
		public int y;

		public Vector2di(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		

	}
}

