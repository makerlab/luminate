using UnityEngine;
using System.Collections;
using metaio;

public class InstantTrackingCallback2 : metaioCallback  {
	override protected void onInstantTrackingEvent(string filepath) {
		Debug.Log("onInstantTrackingEvent: "+filepath);
		GUI.enabled = false;
		if (filepath.Length > 0) {
			int result = MetaioSDKUnity.setTrackingConfiguration(filepath, 1);
			//Debug.Log("onInstantTrackingEvent: instant tracking configuration loaded: "+result);
		}
	}
}