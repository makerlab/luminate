using System;
using metaio.common;

namespace metaio
{
	// Called MetaioCamera to avoid naming clash with Unity's internal Camera class (compiler warning)
	public class MetaioCamera
	{
		public enum Facing
		{
			UNDEFINED = 0,
			FRONT = 1,
			BACK = 2
		}
		
		public const uint FLIP_NONE = 0;
		public const uint FLIP_VERTICAL = 1;
		public const uint FLIP_HORIZONTAL = 1<<1;
		public const uint FLIP_BOTH = FLIP_VERTICAL | FLIP_HORIZONTAL;
		
		public int index = -1;
		
		public string friendlyName = string.Empty;
		
		public Vector2di resolution = new Vector2di(320, 240);
		
		public Vector2d fps = new Vector2d(0.0f, 0.0f);
		
		public uint downsample = 1;
		
		public bool yuvPipeline = false;
		
		public Facing facing = Facing.UNDEFINED;
		
		public uint flip = FLIP_NONE;

		public MetaioCamera Clone()
		{
			return new MetaioCamera {
				downsample = this.downsample,
				facing = this.facing,
				flip = this.flip,
				fps = this.fps,
				friendlyName = this.friendlyName,
				index = this.index,
				resolution = new Vector2di(this.resolution.x, this.resolution.y),
				yuvPipeline = this.yuvPipeline
			};
		}

		public static MetaioCamera FromPB(metaio.unitycommunication.Camera cam)
		{
			MetaioCamera ret = new MetaioCamera();
			ret.downsample = cam.Downsample;
			ret.facing = (Facing)cam.Facing;
			ret.flip = (uint)cam.Flip;
			ret.fps = new Vector2d(cam.Fps.X, cam.Fps.Y);
			ret.friendlyName = cam.FriendlyName;
			ret.index = cam.Index;
			ret.resolution = new Vector2di(cam.Resolution.X, cam.Resolution.Y);
			ret.yuvPipeline = cam.YuvPipeline;
			return ret;
		}
		
		public metaio.unitycommunication.Camera ToPB()
		{
			return metaio.unitycommunication.Camera.CreateBuilder()
				.SetDownsample(downsample)
				.SetFacing((metaio.unitycommunication.CameraFacing)(int)facing)
				.SetFlip((metaio.unitycommunication.CameraFlip)(int)flip)
				.SetFps(metaio.unitycommunication.Vector2d.CreateBuilder().SetX(fps.x).SetY(fps.y).Build())
				.SetFriendlyName(friendlyName)
				.SetIndex(index)
				.SetResolution(metaio.unitycommunication.Vector2di.CreateBuilder().SetX(resolution.x).SetY(resolution.y).Build())
				.SetYuvPipeline(yuvPipeline)
				.Build();
		}
		
		public bool validateParameters()
		{
			return (index >= 0 && resolution.x > 0 && resolution.y > 0 && fps.x >= 0.0f && fps.y >= 0.0f && downsample > 0);
		}
	}
}

