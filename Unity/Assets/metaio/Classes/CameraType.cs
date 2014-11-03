namespace metaio
{
	public enum CameraType
	{
		Tracking = 1<<0,
		RenderingMono = 1<<1,
		RenderingStereoLeft = 1<<2,
		RenderingStereoRight = 1<<3,
		All = 0xff
	}
}
