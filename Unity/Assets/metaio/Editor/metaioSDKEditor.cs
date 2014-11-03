using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using metaio;

[CustomEditor(typeof(metaioSDK))]
public class metaioSDKEditor : Editor {
	
	// referece to the metaioSDK
	private metaioSDK metaioSDK;
    private String currentTrackingConfiguration;
    private static MapLoader mapLoader = new MapLoader();

    public void OnEnable()
    {
        metaioSDK = (metaioSDK)target;
        currentTrackingConfiguration = metaioSDK.trackingConfiguration;

        if (metaioSDK.trackingConfiguration.EndsWith(".3dmap") || metaioSDK.trackingConfiguration.EndsWith(".creator3dmap"))
        {
            mapLoader.setMapObject(GameObject.Find("Feature Map"));
        }
    }
	
	void OnGUI ()
	{
		GUILayout.Label ("metaio SDK", EditorStyles.boldLabel);
	}
	

	public override void OnInspectorGUI()
	{
		//base.OnInspectorGUI();
			
		// info text
		EditorGUILayout.HelpBox("The metaioSDK compnent will be used to configure the tracking, preview the camera, " +
		 	"tranfrom the main camera and provide a valid SDK license. If you use the Unity build-in configuratio, " +
		 	"please use read the documenation at http://dev.metaio.com/sdk", MessageType.Info);

		try
		{
			// This may fail with Unity free license (Unity plugins for Windows/Mac require Unity PRO license)
			metaioSDK.writeApplicationSignature(EditorGUILayout.TextField("SDK Signature", metaioSDK.parseApplicationSignature()));
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
			metaioSDK.stereoRenderingEnabled = EditorGUILayout.Toggle("Stereo rendering", metaioSDK.stereoRenderingEnabled);

			metaioSDK.seeThroughEnabled = EditorGUILayout.Toggle("See-through", metaioSDK.seeThroughEnabled);
#else
			metaioSDK.stereoRenderingEnabled = EditorGUILayout.ToggleLeft("Stereo rendering", metaioSDK.stereoRenderingEnabled);

			metaioSDK.seeThroughEnabled = EditorGUILayout.ToggleLeft("See-through", metaioSDK.seeThroughEnabled);
#endif
		}
		catch (Exception e)
		{
			Debug.LogWarning("Failed to write Metaio SDK license file (expected failure if you use Unity Free license): "+e.Message);
		}

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Choose the default camera to start:");
		int[] facingValues = {0, 1, 2};
		string[] facingNames = {"UNDEFINED", "BACK", "FRONT"};
		metaioSDK.cameraFacing = EditorGUILayout.IntPopup("Camera facing", metaioSDK.cameraFacing, facingNames, facingValues);

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Renderer clipping plane limits in millimeters:");
		metaioSDK.nearClippingPlaneLimit = EditorGUILayout.FloatField("Near Limit", metaioSDK.nearClippingPlaneLimit);
		metaioSDK.farClippingPlaneLimit = EditorGUILayout.FloatField("Far Limit", metaioSDK.farClippingPlaneLimit);
		
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Tracking configuration:");
        metaioSDK.trackingAssetIndex = EditorGUILayout.Popup("Select source", metaioSDK.trackingAssetIndex, metaioSDK.trackingAssets, EditorStyles.popup);
		
		if (metaioSDK.trackingAssetIndex == 8)
		{
			// select from streaming assets
			metaioSDK.trackingConfiguration="tracking.xml";
			EditorGUILayout.HelpBox("Just drag&drop a *.xml, *.3dmap or *.zip file with tracking data from your project view here", MessageType.Info);
			metaioSDK.trackingAsset = EditorGUILayout.ObjectField( metaioSDK.trackingAsset, typeof(UnityEngine.Object), true);
			
			// set the actual file path
			metaioSDK.trackingConfiguration = AssetDatabase.GetAssetPath(metaioSDK.trackingAsset);
			metaioSDK.trackingConfiguration = metaioSDK.trackingConfiguration.Replace("Assets/StreamingAssets/", "");
			//Debug.Log("Tracking configuration dragged: " + metaioSDK.trackingConfiguration);
		}
		else if (metaioSDK.trackingAssetIndex == 9)
		{
			// specify absolute path
			metaioSDK.trackingConfiguration = EditorGUILayout.TextField("Tracking Configuration", metaioSDK.trackingConfiguration);
		}
		else if (metaioSDK.trackingAssetIndex == 10)
		{
			// generate tracking xml
			metaioSDK.trackingConfiguration="TrackingConfigGenerated.xml";
		}
		else if (metaioSDK.trackingAssetIndex > 0)
		{
			metaioSDK.trackingConfiguration = metaioSDK.trackingAssets[metaioSDK.trackingAssetIndex];
		}
		else
		{
			metaioSDK.trackingConfiguration = "";
			Debug.LogWarning("No tracking configuration selected");
		}
			
		// here we can add more options
        if (GUI.changed)
        {
            // if tracking configuration is a 3D map and it changed visualiaze the map
            if (metaioSDK.trackingConfiguration.EndsWith(".3dmap") || metaioSDK.trackingConfiguration.EndsWith(".creator3dmap"))
            {
                if (!currentTrackingConfiguration.Equals(metaioSDK.trackingConfiguration))
                {
                    mapLoader.loadMap(metaioSDK.trackingConfiguration);
                    EditorApplication.update = createMap;
                }
            }
            else
            {
                mapLoader.clearMap();
            }

            currentTrackingConfiguration = metaioSDK.trackingConfiguration;
            EditorUtility.SetDirty(target);
        }
	}

    void createMap()
    {
        if (mapLoader.createFeatures()) EditorApplication.update = null; 
    }

}
