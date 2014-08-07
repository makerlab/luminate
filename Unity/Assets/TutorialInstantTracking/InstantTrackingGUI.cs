using UnityEngine;
using System.Collections;
using metaio;

public class InstantTrackingGUI : MonoBehaviour {
	private float SizeFactor;
	public GUIStyle buttonTextStyle;
	void Update() {
		SizeFactor = GUIUtilities.SizeFactor;
	}
	void OnGUI() {
		if(GUIUtilities.ButtonWithText(new Rect(
			Screen.width - 200*SizeFactor,
			0,
			200*SizeFactor,
			100*SizeFactor),"Track",null,buttonTextStyle) ) {
			MetaioSDKUnity.startInstantTracking("INSTANT_3D", "");
			GUI.enabled = false;
		}
	}
}
