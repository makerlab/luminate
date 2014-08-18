using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using metaio.common;

namespace metaio
{

public static class MetaioSDKUnity
{
		
	public static metaioDeviceCamera deviceCamera;
		
	// Defines whether the camera is active (as opposed to setImage)
	public static bool usingCamera = true;

	public static MetaioCamera requestedCamera = null;

#region DLL functions
	
#if UNITY_IPHONE && !UNITY_EDITOR
	public const String METAIO_DLL = "__Internal";
#else
	public const String METAIO_DLL = "metaiosdk";
#endif
	
	/// <summary>
	/// Create the metaio SDK instance.
	/// </summary>
	/// <returns>
	/// 0 on sucess, non-zero on failure
	/// </returns>
	/// <param name='signature'>
	/// Application signature.
	/// </param>
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int createMetaioSDKUnity(string signature);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr getVersionNative();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void enableBackgroundProcessing();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void disableBackgroundProcessing();
	
	/// <summary>
	/// Delete the metaio SDK instance.
	/// </summary>
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void deleteMetaioSDKUnity();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void pause();

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void resume();

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	private static extern void setFreezeTrackingInternal(int freeze);
	public static void setFreezeTracking(bool freeze) { setFreezeTrackingInternal(freeze ? 1 : 0); }

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void pauseTracking(int keepTrackingValues);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void resumeTracking();
		
	/// <summary>
	/// Enable/disable metaio SDK callback.
	/// </summary>
	/// <param name='enable'>
	/// true to enable, false to disable
	/// </param>
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void registerCallback(int enable);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getUnityCallbackEventID();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr getUnityCallbackEventValue();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern uint getUnityCallbackEventValueLength();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void removeUnityCallbackEvent();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setScreenRotation(int rotation);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getScreenRotation();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void resizeRenderer(int width, int height);
	
	// This is only here for platforms where GL.IssuePluginEvent seems not working (see below)
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void UnityRenderEvent(int eventID);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int setTrackingConfiguration(string trackingConfiguration, int readFromFile);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int setCameraParametersNative(string cameraFile);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr getCameraParametersNative();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setRendererClippingPlaneLimitsNative(float nearCP, float farCP);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setLLAObjectRenderingLimits(int nearLimit, int farLimit);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern uint getRequiredTextureSize();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern float getCameraPlaneScale();

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void getSensorGravity(float[] values);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void getLocation(double[] values);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void getProjectionMatrix(float[] matrix);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getNumberOfValidCoordinateSystems();
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getNumberOfDefinedCoordinateSystems();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getTrackingValues(int cosID, float[] values);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setTrackingEventCallbackReceivesAllChanges(int enable);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern long getRenderingDuration();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern long getTrackingDuration();

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern float getRenderingFrameRate();
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern float getTrackingFrameRate();

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern float getCameraFrameRate();
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void freeReturnedMemory(IntPtr ptr);

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr listCamerasInternal(out uint length);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void startSensors(int sensors);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void stopSensors(int sensors);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void pauseSensors();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void resumeSensors();
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getRunningSensors();

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	private static extern void setImageInternal(IntPtr buffer, uint width, uint height, int colorFormatInternal, uint originIsUpperLeft, int stride);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	private static extern void setImageInternalFromImage(string filePath, out int outWidth, out int outHeight);

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setManualLocation(float latitude, float longitude, float altitude);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void resetManualLocation();
	
	/// <summary>
	/// Set the cos offset.
	/// </summary>
	/// <param name='coordinateSystemID'>
	/// Coordinate system ID
	/// </param>
	/// <param name='translation'>
	/// Translation (3 floats) or null to reset
	/// </param>
	/// <param name='rotation'>
	/// Rotation as quaternion (4 floats) or null to reset
	/// </param>
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setCosOffset(int coordinateSystemID, float[] translation, float[] rotation);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void requestCameraImage(string filepath, int width, int height);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int startInstantTracking(string trackingConfiguration, string outFile);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void convertLLAToTranslation(double latitude, double longitude, int enableLLALimits, float[] translation);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr createGeometryFromMovie(string movieFilename);
	
	// Only used for movie textures currently
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void deleteMovieGeometry(IntPtr movieGeometry);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern uint getMovieTextureHeight(IntPtr movieGeometry);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern uint getMovieTextureWidth(IntPtr movieGeometry);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern float getMovieTextureDisplayAspect(IntPtr movieGeometry);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void setMovieTextureTargetTextureID(IntPtr movieGeometry, int textureID);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void startMovieTexture(IntPtr movieGeometry, int loop);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void pauseMovieTexture(IntPtr movieGeometry);

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void stopMovieTexture(IntPtr movieGeometry);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void requestVisualSearch(string databaseID, int returnFullTrackingConfig, string visualSearchServer);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr sensorCommandNative(string command, string parameter);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern IntPtr getCoordinateSystemNameNative(int coordinateSystemID);
		
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern int getCoordinateSystemID(string name);

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	private static extern int get3DPointsFrom3DmapInternal(string filePath3DMap, IntPtr out3DFeaturePositions, uint maxVec3);

	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern UInt64 startCameraInternal(IntPtr pbBuf, uint length);
	
	[DllImport(METAIO_DLL, CallingConvention=CallingConvention.Cdecl)]
	public static extern void stopCamera();

#endregion
	
	public static string getVersion()
	{
		IntPtr resultPtr = getVersionNative();
		string result = Marshal.PtrToStringAnsi(resultPtr);
		return result;
	}
	
	/// <summary>
	/// Sets the renderer clipping plane limits.
	/// </summary>
	/// <param name='nearCP'>
	/// Near Clipping plane limit in millimeters
	/// </param>
	/// <param name='farCP'>
	/// Far Clipping plane limit in millimeters
	/// </param>
	public static void setRendererClippingPlaneLimits(float nearCP, float farCP)
	{
		setRendererClippingPlaneLimitsNative(nearCP, farCP);
		// Also set clipping plane limits for the MainCamera (metaioCamera)
		Camera camera = GameObject.Find("MainCamera").camera;
		camera.nearClipPlane = nearCP/1000f;	// in meters
		camera.farClipPlane = farCP/1000f;		// in meters
	}
	
	// Returns number of features on success, or 0 otherwise
	public static int get3DPointsFrom3Dmap(string filePath3DMap, float[] out3DFeaturePositions)
	{
		GCHandle arrayHandle = GCHandle.Alloc(out3DFeaturePositions, GCHandleType.Pinned);
		IntPtr ptr = arrayHandle.AddrOfPinnedObject();
		
		int res = get3DPointsFrom3DmapInternal(filePath3DMap, ptr, (uint)out3DFeaturePositions.Length/3);
		
		arrayHandle.Free();
		
		return res;
	}
	
	public static List<MetaioCamera> getCameraList()
	{
		List<MetaioCamera> ret = new List<MetaioCamera>();

		uint length = 0;
		IntPtr pbList = listCamerasInternal(out length);
		
		try
		{
			if (pbList == IntPtr.Zero || length == 0)
			{
				Debug.LogError("listCamerasInternal failed");
				return ret;
			}
	
			byte[] pbAsBytes = new byte[length];
			Marshal.Copy(pbList, pbAsBytes, 0, (int)length);
			
			metaio.unitycommunication.ListCamerasProtocol pb = metaio.unitycommunication.ListCamerasProtocol.ParseFrom(pbAsBytes);
			
			for (int i = 0; i < pb.CamerasCount; ++i)
			{
				ret.Add(MetaioCamera.FromPB((metaio.unitycommunication.Camera)pb.CamerasList[i]));
			}
		}
		finally
		{
			freeReturnedMemory(pbList);
		}

		return ret;
	}
	
	private static Vector2di parseUInt64ToVector2diUnsigned(UInt64 val)
	{
		checked
		{
			return new Vector2di((int)(val & 0xffffffff), (int)(val >> 32));
		}
	}
	
	public static bool setCameraParameters(string cameraFile)
	{
		int res = setCameraParametersNative(cameraFile);
		metaioCamera.updateCameraProjectionMatrix();
		return res != 0;
	}
	
	public static string getCameraParameters()
	{
		IntPtr resultPtr = getCameraParametersNative();
		string result = Marshal.PtrToStringAnsi(resultPtr);
		return result;
	}

	public static Vector2di startCamera(int index, uint width, uint height, uint downsample)
	{
		return startCamera(new MetaioCamera
		{
			downsample = downsample,
			index = index,
			resolution = new Vector2di((int)width, (int)height)
		});
	}

	public static Vector2di startCamera(MetaioCamera camera)
	{

		metaio.unitycommunication.StartCameraProtocol protocol = metaio.unitycommunication.StartCameraProtocol.CreateBuilder().SetCamera(camera.ToPB()).Build();

		using(MemoryStream stream = new MemoryStream())
        {
			protocol.WriteTo(stream);

			byte[] buf = stream.ToArray();
			GCHandle arrayHandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
			try
			{
				usingCamera = true;
				requestedCamera = camera.Clone();
				Vector2di ret;
				checked
				{
					IntPtr ptr = arrayHandle.AddrOfPinnedObject();
					ret = parseUInt64ToVector2diUnsigned(startCameraInternal(ptr, (uint)buf.LongLength));
				}
				metaioCamera.updateCameraProjectionMatrix();
				return ret;
			}
			finally
			{
				arrayHandle.Free();
			}
		}
	}
		
	/// <summary>
	/// Set an image file as image source
	/// </summary>
	/// <remarks>
	/// This method is used to set the image source from a file for rendering and tracking. It will automatically stop
	/// camera capture if currently running. Call startCamera again to resume capturing from camera.
	/// </remarks>
	/// <param name='filePath'>
	/// Path to the image file
	/// </param>
	/// <returns>
	/// Resolution of the image if loaded successfully, else a null vector
	/// </returns>
	public static Vector2di setImage(string filePath)
	{
		int width = 0;
		int height = 0;
		
		setImageInternalFromImage(filePath, out width, out height);
		
		if (width > 0 && height > 0)
		{
			uint largerDimension = (uint)Math.Max(width, height);
			uint powerOf2 = 1;
			while (powerOf2 < largerDimension)
				powerOf2 *= 2;

			if (deviceCamera != null)
				deviceCamera.prepareForPotentialTextureSizeChange(powerOf2);
		}

		metaioCamera.updateCameraProjectionMatrix();
		usingCamera = false;

		return new Vector2di(width, height);
	}
	
	public static void setImage(byte[] buffer, uint width, uint height, ColorFormat colorFormat, bool originIsUpperLeft)
	{
		// Cannot use default parameters in Unity, forces .NET 3.5 syntax
		setImage(buffer, width, height, colorFormat, originIsUpperLeft, -1);
	}

	public static void setImage(byte[] buffer, uint width, uint height, ColorFormat colorFormat, bool originIsUpperLeft, int stride)
	{
		GCHandle arrayHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		IntPtr ptr = arrayHandle.AddrOfPinnedObject();
		
		uint largerDimension = Math.Max(width, height);
		uint powerOf2 = 1;
		while (powerOf2 < largerDimension)
			powerOf2 *= 2;

		if (deviceCamera != null)
			deviceCamera.prepareForPotentialTextureSizeChange(powerOf2);

		setImageInternal(ptr, width, height, (int)colorFormat, originIsUpperLeft ? 1U : 0U, stride);

		arrayHandle.Free();

		metaioCamera.updateCameraProjectionMatrix();
		usingCamera = false;
	}
	
	/// <summary>
	/// Send sensor command
	/// </summary>
	/// <returns>
	/// Result of the sensor command
	/// </returns>
	/// <param name='command'>
	/// Command to send
	/// </param>
	/// <param name='parameter'>
	/// Parameter to send
	/// </param>
	public static string sensorCommand(string command, string parameter)
	{
		IntPtr resultPtr = sensorCommandNative(command, parameter);
		string result = Marshal.PtrToStringAnsi(resultPtr);
		return result;
	}
		
	public static string getCoordinateSystemName(int coordinateSystemID)
	{
		IntPtr resultPtr = getCoordinateSystemNameNative(coordinateSystemID);
		string result = Marshal.PtrToStringAnsi(resultPtr);
		return result;
	}
	
	/// <summary>
	/// Sets the tracking configuration from resource or a named string.
	/// </summary>
	/// <returns>
	/// non-zero in case of success, else 0
	/// </returns>
	/// <param name='trackingConfig'>
	/// XML file name in the resource, or a named string, e.g. "LLA" or "QRCODE"
	/// </param>
	public static int setTrackingConfigurationFromAssets(string trackingConfig)
    {
        int result = 0;
		
		// first check inside streaming assets
		String assetPath = AssetsManager.getAssetPath(trackingConfig);
			
		if (assetPath != null)
		{
			result = setTrackingConfiguration(assetPath, 1);
		}
		else if (trackingConfig != null)
		{
			Debug.Log("Tracking configuration '" +trackingConfig + "' not found in the streaming assets, loading it as absolute path or string");
			result = setTrackingConfiguration(trackingConfig, 1);
		}
		
		return result;
    }
		
	/// <summary>
	/// Updates the screen orientation of metaio SDK
	/// </summary>
	/// <param name='orientation'>
	/// Screen orientation.
	/// </param>
	public static void updateScreenOrientation(ScreenOrientation orientation)
	{
		switch (orientation)
		{
		case ScreenOrientation.LandscapeLeft:
			setScreenRotation(0);
			break;
		case ScreenOrientation.LandscapeRight:
			setScreenRotation(2);
			break;
		case ScreenOrientation.Portrait:
			setScreenRotation(3);
			break;
		case ScreenOrientation.PortraitUpsideDown:
			setScreenRotation(1);
			break;
		default:
			Debug.LogError("Screen orientation still unknown");
			break;
		}
	}

}	// end class MetaioSDKUnity
}	// end namespace metaio

