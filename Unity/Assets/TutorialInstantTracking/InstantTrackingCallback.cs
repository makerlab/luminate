using UnityEngine;
using System.Collections;
using metaio;

public class InstantTrackingCallback : metaioCallback 
{
	override protected void onInstantTrackingEvent(string filepath)
	{
		Debug.Log("onInstantTrackingEvent: "+filepath);
		
		GUI.enabled = false;
		
		// if succeeded, set new tracking configuration
		if (filepath.Length > 0)
		{
			int result = MetaioSDKUnity.setTrackingConfiguration(filepath, 1);
			Debug.Log("onInstantTrackingEvent: instant tracking configuration loaded: "+result);
		}
		
	}
}
