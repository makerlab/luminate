using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Xml;
	
[CustomEditor(typeof(metaioTracker))]
public class metaioTrackerEditor : Editor
{
	private metaioTracker tracker;
 	private metaioSDK metaioSDK;
	
    public void OnEnable()
    {
        tracker = (metaioTracker)target;
		 
		metaioSDK = (metaioSDK) GameObject.Find("metaioSDK").GetComponent("metaioSDK");
		if(!metaioSDK)
			Debug.LogError("Could not find the metaioSDK, with the metaioSDK script attached");
    }
	
	void OnGUI ()
	{
		GUILayout.Label ("metaio SDK", EditorStyles.boldLabel);
	}
	

	public override void OnInspectorGUI()
	{
		// help text
		EditorGUILayout.HelpBox("Specify coordinate system ID to which this tracker should be attached", MessageType.Info);
		
		// Coordinate system ID
		tracker.cosID = EditorGUILayout.IntField("Coordinate Sytem ID", tracker.cosID);
		
		// help text
		EditorGUILayout.HelpBox("Specify if the camera should be transform or the objects", MessageType.Info);
		GUIContent cameraLabel = new GUIContent("Transform camera", "Tranform the camera or the object? The default is true." +
			"If you need to tranform multiple objects, the camera should be fixed (e.g. set to false)");
		tracker.transformCamera = EditorGUILayout.Toggle(cameraLabel, tracker.transformCamera);
		
        if (metaioSDK.trackingAssetIndex == 10)
        {
            // help text
            EditorGUILayout.HelpBox("Here you can specify the appropriate tracking target. Please make sure the images have " +
                "been placed in the StreamingAssets folder", MessageType.Info);
            // tracking pattern
            tracker.texture = (Texture2D)EditorGUILayout.ObjectField("Tracking Pattern", tracker.texture, typeof(Texture2D), false);
            tracker.trackingImage = AssetDatabase.GetAssetPath(tracker.texture);
            
            // was Application.streamingAssetsPath, but caused problems on Mac 
            if (tracker.trackingImage.StartsWith("Assets") && !tracker.trackingImage.StartsWith("Assets/StreamingAssets"))
            {
                Debug.LogWarning("Selected image is not in the StreamingAsset folder");
                return;
            }

            saveTrackingXML();
        }
		
		EditorGUILayout.HelpBox("Specify geo location (X=Latitude and Y=Longitude) for this tracker. Setting 0 for both elements means no geo location.", MessageType.Info);
		tracker.geoLocation = EditorGUILayout.Vector2Field("Geo Location", tracker.geoLocation);
		
		EditorGUILayout.HelpBox("Enable or disable LLA limits for this tracker", MessageType.Info);
		GUIContent llaLimlitsLabel = new GUIContent("Enable LLA Limits");
		tracker.enableLLALimits = EditorGUILayout.Toggle(llaLimlitsLabel, tracker.enableLLALimits);
		
		tracker.ApplyModifications();

		// shoud we simulate the camera?
		// todo: connect to tracker.simulatePose
		
		if (GUI.changed) 
			EditorUtility.SetDirty(target);
	}
	
	
	
	public void addPattern(XmlWriter writer, string pattern, string name)
	{
		// clean up the path
		pattern = pattern.Replace("Assets/StreamingAssets/", "");
		Debug.Log("new pattern" +pattern);
		
		writer.WriteStartElement("SensorCOS");
		writer.WriteElementString("SensorCosID", name);
		writer.WriteStartElement("Parameters");
		writer.WriteElementString("ReferenceImage", pattern);
		writer.WriteElementString("SimilarityThreshold", "0.7");
		writer.WriteEndElement();
		writer.WriteEndElement();
	}
	
	public void saveTrackingXML()
	{
		Debug.Log("Saving xml");
		string filename = "TrackingConfigGenerated.xml";
		string filepath = Application.dataPath + @"/StreamingAssets/"+filename;  
		
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.NewLineOnAttributes = true;
		xmlWriterSettings.Indent = true;
	
		XmlWriter writer = XmlWriter.Create(filepath, xmlWriterSettings);
		writer.WriteStartDocument();
		writer.WriteStartElement("TrackingData");
		
		// get all the single patterns
		metaioTracker[] patterns = metaioSDK.GetComponentsInChildren<metaioTracker>();

		if (patterns.Length == 0)
			Debug.LogWarning("No metaioTracker objects found, make sure you have assigned at least one as child of " +
			                 "the metaioSDK object. The currently generated tracking configuration will fail to load.");

		//Sensors
		writer.WriteStartElement("Sensors");
			writer.WriteStartElement("Sensor");
			writer.WriteAttributeString("Type", "FeatureBasedSensorSource");
			writer.WriteAttributeString("Subtype", "Fast");
				writer.WriteElementString("SensorID", "FeatureTracking1");
				// parameters
				writer.WriteStartElement("Parameters");
				writer.WriteEndElement();
		
				// add all the patterns
				foreach ( metaioTracker pattern in patterns) {
					string name = "Patch_"+pattern.name+"_"+pattern.trackingImage;
					addPattern(writer, pattern.trackingImage, name );
				}
		
			writer.WriteEndElement();
		writer.WriteEndElement();
		
		//Exp
		//writer.WriteStartElement("Connections");
		//writer.WriteEndElement();

		writer.WriteEndElement();
		writer.WriteEndDocument();
		writer.Close();
			
		AssetsManager.setAssetPath(filename, filepath);
		metaioSDK.trackingConfiguration = filename;
			
	}
}
