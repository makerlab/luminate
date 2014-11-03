using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using metaio;
using metaio.unitycommunication.util;

public class metaioCallback : MonoBehaviour 
{
	
#region metaio SDK callbacks
	
	/// <summary>
	/// This callback reports when the SDK is ready
	/// </summary>
	virtual protected void onSDKReady()
	{
	}
	
	/// <summary>
	/// This callback reports tracking events
	/// </summary>
	/// <param name="trackingValues">
	/// One or more tracking event structures.
	/// </param>
	virtual protected void onTrackingEvent(List<TrackingValues> trackingValues)
	{
	}
	
	/// <summary>
	/// This callback reports the result of instant tracking, i.e. when
	/// startInstantTracking is called.
	/// </summary>
	/// <param name='filepath'>
	/// Filepath of the newly generated tracking configuration in case of success,
	/// or empty when failed
	/// </param>
	virtual protected void onInstantTrackingEvent(String filepath)
	{
	}
	
	/// <summary>
	/// This callback reports the result of requestCameraImage
	/// </summary>
	/// <param name='filepath'>
	/// Filepath where camera image has been saved, or empty if failed
	/// </param>
	virtual protected void onCameraImageSaved(String filepath)
	{
	}

	/// <summary>
	/// Called when a movie finishes playing.
	/// </summary>
	/// <param name="gameObjectName">
	/// Name of the game object to which the movie texture belongs.
	/// </param>
	virtual protected void onMovieEnd(string gameObjectName)
	{
	}
	
	virtual protected void onVisualSearchResult(VisualSearchResponse[] response, int errorCode)
	{
	}
	
	virtual protected void onVisualSearchStatusChanged(String state)
	{
	}
	
	/// <summary>
	/// This callback reports a debug message from the metaio plugin
	/// </summary>
	/// <param name='log'>
	/// Debug message
	/// </param>
	virtual protected void onLog(String log)
	{
		Debug.Log(log);
	}
	
	/// <summary>
	/// This callback reports warning from the metaio plugin
	/// </summary>
	/// <param name='log'>
	/// Warning message
	/// </param>
	virtual protected void onLogWarning(String log)
	{
		Debug.LogWarning(log);
	}
	
	/// <summary>
	/// This callback reports an error from the metaio plugin
	/// </summary>
	/// <param name='log'>
	/// Error message
	/// </param>
	virtual protected void onLogError(String log)
	{
		Debug.LogError(log);
	}
	
#endregion


#region Handling callback from the plugin
	
	/// <summary>
	/// metaio SDK callbacks' identifiers
	/// </summary>
	public enum EUNITY_CALLBACK_EVENT
	{
		EUCE_NONE =						0,
		EUCE_LOG =						1,
		EUCE_LOG_WARNING =				2,
		EUCE_LOG_ERROR =				3,
		EUCE_SDK_READY =				4,
		EUCE_TRACKING_EVENT =			5,
		EUCE_INSTANT_TRACKING_EVENT =	6,
		EUCE_CAMERA_IMAGE_SAVED =		7,
		EUCE_VISUAL_SEARCH_RESULT =		8,
		EUCE_VISUAL_SEARCH_STATUS =		9,
		EUCE_MOVIE_END =				10,
	};
	
	public void Start()
	{
		// Enable callbacks
		MetaioSDKUnity.registerCallback(1);
	}
	
	public void OnEnable()
	{
		// Enable callbacks
		MetaioSDKUnity.registerCallback(1);
	}
	
	void OnDisable()
	{
		// Disable callbacks
		MetaioSDKUnity.registerCallback(0);
	}
	
	void OnDestroy()
	{
		// Disable callbacks
		MetaioSDKUnity.registerCallback(0);
	}
	
	public void Update()
	{
		EUNITY_CALLBACK_EVENT eventID = (EUNITY_CALLBACK_EVENT)MetaioSDKUnity.getUnityCallbackEventID();
	
		if (eventID != EUNITY_CALLBACK_EVENT.EUCE_NONE)
		{
			uint eventValueLength = 0;
			IntPtr eventValuePtr = MetaioSDKUnity.getUnityCallbackEventValue(out eventValueLength);

//			Debug.Log("Callback event: "+eventID+", "+eventValue);
			
			switch (eventID)
			{
				case EUNITY_CALLBACK_EVENT.EUCE_LOG:
					onLog(Marshal.PtrToStringAnsi(eventValuePtr));
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_LOG_WARNING:
					onLogWarning(Marshal.PtrToStringAnsi(eventValuePtr));
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_LOG_ERROR:
					onLogError(Marshal.PtrToStringAnsi(eventValuePtr));
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_SDK_READY:
					onSDKReady();
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_TRACKING_EVENT:
					byte[] pbAsBytes = new byte[eventValueLength];
					Marshal.Copy(eventValuePtr, pbAsBytes, 0, (int)eventValueLength);
					metaio.unitycommunication.OnTrackingEventProtocol prot = metaio.unitycommunication.OnTrackingEventProtocol.ParseFrom(pbAsBytes);
					List<TrackingValues> listTV = new List<TrackingValues>();
					for (int i = 0; i < prot.TrackingValuesCount; ++i)
					{
						listTV.Add(TrackingValues.FromPB(prot.TrackingValuesList[i]));
					}
					onTrackingEvent(listTV);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_INSTANT_TRACKING_EVENT:
					onInstantTrackingEvent(eventValuePtr.MarshalToStringUTF8());
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_CAMERA_IMAGE_SAVED:
					onCameraImageSaved(eventValuePtr.MarshalToStringUTF8());
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_VISUAL_SEARCH_RESULT:
					parseVisualSearchResponse(eventValuePtr, eventValueLength);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_VISUAL_SEARCH_STATUS:
					onVisualSearchStatusChanged(Marshal.PtrToStringAnsi(eventValuePtr));
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_MOVIE_END:
					IntPtr movieTextureGeometryPtr = new IntPtr(int.Parse(Marshal.PtrToStringAnsi(eventValuePtr)));
					onMovieEnd(metaioMovieTexture.getGameObjectNameForMovieTextureGeometryPtr(movieTextureGeometryPtr));
					break;
			}
			
			// remove the callback event from queue
			MetaioSDKUnity.removeUnityCallbackEvent();
		}
	}
	
	private void parseVisualSearchResponse(IntPtr eventValuePtr, uint eventValueLength)
	{
		byte[] pbAsBytes = new byte[eventValueLength];
		Marshal.Copy(eventValuePtr, pbAsBytes, 0, (int)eventValueLength);
		metaio.unitycommunication.OnVisualSearchResultProtocol pb = metaio.unitycommunication.OnVisualSearchResultProtocol.ParseFrom(pbAsBytes);
		
		VisualSearchResponse[] responses = new VisualSearchResponse[pb.ResponsesCount];
		for (int i = 0; i < pb.ResponsesCount; ++i)
		{
			responses[i] = VisualSearchResponse.FromPB(pb.ResponsesList[i]);
		}

		onVisualSearchResult(responses, pb.ErrorCode);
	}
	
#endregion
	
}
