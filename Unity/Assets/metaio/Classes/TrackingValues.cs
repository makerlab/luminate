using System;

namespace metaio
{
	// Same integer values as in native SDK
	public enum TrackingState
	{
		Unknown = 0,
		NotTracking = 1,
		Tracking = 2,
		Lost = 3,
		Found = 4,
		Extrapolated = 5,
		Initialized = 6,
	 	Registered = 7
	}
	
	public static class TrackingStateExtensions
	{
		public static bool isTrackingState(this TrackingState state)
		{
			return state == TrackingState.Tracking || state == TrackingState.Found || state == TrackingState.Extrapolated;
		}
	}
	
	public class TrackingValues
	{
		public TrackingState state;
		
		public Vector3d translation;
		
		public Vector4d rotation;
		
		public LLACoordinate llaCoordinate;
		
		public float quality;
		
		public double timeElapsed;
		
		public int coordinateSystemID;
		
		public string cosName;
		
		public string additionalValues;
		
		public static TrackingValues FromPB(metaio.unitycommunication.TrackingValues tv)
		{
			TrackingValues ret = new TrackingValues();
			ret.state = (TrackingState)tv.State;
			ret.translation = new Vector3d(tv.Translation.X, tv.Translation.Y, tv.Translation.Z);
			ret.rotation = new Vector4d(tv.Rotation.X, tv.Rotation.Y, tv.Rotation.Z, tv.Rotation.W);
			ret.llaCoordinate = new LLACoordinate() {
				latitude = tv.LlaCoordinate.Latitude,
				longitude = tv.LlaCoordinate.Longitude,
				altitude = tv.LlaCoordinate.Altitude,
				accuracy = tv.LlaCoordinate.Accuracy,
				timestamp = tv.LlaCoordinate.Timestamp
			};
			ret.quality = tv.Quality;
			ret.timeElapsed = tv.TimeElapsed;
			ret.coordinateSystemID = tv.CoordinateSystemID;
			ret.cosName = tv.CosName;
			ret.additionalValues = tv.AdditionalValues;
			return ret;
		}
	}
}

