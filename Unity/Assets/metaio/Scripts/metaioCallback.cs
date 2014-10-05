using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using metaio;

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
			IntPtr eventValuePtr = MetaioSDKUnity.getUnityCallbackEventValue();
			String eventValue = Marshal.PtrToStringAnsi(eventValuePtr);

//			Debug.Log("Callback event: "+eventID+", "+eventValue);
			
			switch (eventID)
			{
				case EUNITY_CALLBACK_EVENT.EUCE_LOG:
					onLog(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_LOG_WARNING:
					onLogWarning(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_LOG_ERROR:
					onLogError(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_SDK_READY:
					onSDKReady();
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_TRACKING_EVENT:
					uint length = MetaioSDKUnity.getUnityCallbackEventValueLength();	
					byte[] pbAsBytes = new byte[length];
					Marshal.Copy(eventValuePtr, pbAsBytes, 0, (int)length);
					metaio.unitycommunication.OnTrackingEventProtocol prot = metaio.unitycommunication.OnTrackingEventProtocol.ParseFrom(pbAsBytes);
					List<TrackingValues> listTV = new List<TrackingValues>();
					for (int i = 0; i < prot.TrackingValuesCount; ++i)
					{
						listTV.Add(TrackingValues.FromPB(prot.TrackingValuesList[i]));
					}
					onTrackingEvent(listTV);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_INSTANT_TRACKING_EVENT:
					onInstantTrackingEvent(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_CAMERA_IMAGE_SAVED:
					onCameraImageSaved(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_VISUAL_SEARCH_RESULT:
					parseVisualSearchResponse(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_VISUAL_SEARCH_STATUS:
					onVisualSearchStatusChanged(eventValue);
					break;
				case EUNITY_CALLBACK_EVENT.EUCE_MOVIE_END:
					IntPtr movieTextureGeometryPtr = new IntPtr(int.Parse(eventValue));
					onMovieEnd(metaioMovieTexture.getGameObjectNameForMovieTextureGeometryPtr(movieTextureGeometryPtr));
					break;
			}
			
			// remove the callback event from queue
			MetaioSDKUnity.removeUnityCallbackEvent();
		}
	}
	
	private void parseVisualSearchResponse(String eventValue)
	{
//		Debug.Log ("parseVisualSearchResponse: "+eventValue);
		
		string[] tokens = eventValue.Split("|"[0]);
		
//		Debug.Log ("parseVisualSearchResponse: tokens count "+tokens.Length);
		
		if (tokens.Length < 2)
			return;
		
		int errorCode = Int32.Parse(tokens[0]);
		int responseCount = Int32.Parse(tokens[1]);
		
		VisualSearchResponse[] response = new VisualSearchResponse[responseCount];
		
//		Debug.Log ("parseVisualSearchResponse: "+responseCount);
		
		int j = 2;
		for (int i=0; i<responseCount; i++)
		{
			response[i].TrackingConfigurationName = tokens[j++];
			response[i].TrackingConfiguration = tokens[j++];			
		}
		
		onVisualSearchResult(response, errorCode);
	}
	
#endregion
	
}
