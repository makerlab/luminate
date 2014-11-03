using System;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using metaio;


/// <summary>
/// This class provides main behavior for the metaioSDK GameObject
/// </summary>
public class metaioSDK : MonoBehaviour
{
	// Ensure dependency DLLs can be loaded
	// (cf. http://forum.unity3d.com/threads/31083-DllNotFoundException-when-depend-on-another-dll)
	private void adjustPath()
	{
		var envPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
		var pluginsPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "Assets"), "Plugins");
		
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
		// Unfortunately we cannot use Application.dataPath in the loading thread (in which this constructor is called),
		// so we have to construct the path to "XYZ_Data/Plugins" ourself. Changing the PATH later (e.g. in Awake) does
		// not seem to work.

		// Search for "Plugins" folder in subfolders of the current directory (where the executable is)
		string[] subPaths = Directory.GetFileSystemEntries(Environment.CurrentDirectory);
		
		foreach (var subPath in subPaths)
		{
			var fullSubPath = Path.Combine(Environment.CurrentDirectory, subPath);
			// Only look at directories
			if (!Directory.Exists(fullSubPath))
				continue;
			
			// Use GetFullPath to ensure conversion of path separators (slash or backslash) to native
			var potentialPluginsPath = Path.GetFullPath(Path.Combine(fullSubPath, "Plugins"));
			if (Directory.Exists(potentialPluginsPath) && !envPath.Contains(Path.PathSeparator + potentialPluginsPath))
				envPath += Path.PathSeparator + potentialPluginsPath;
		}
#endif
		
		if ((pluginsPath.Length > 0 && !envPath.Contains(Path.PathSeparator + pluginsPath)))
			envPath += Path.PathSeparator + pluginsPath;
		
		Environment.SetEnvironmentVariable(
			"PATH",
			envPath,
			EnvironmentVariableTarget.Process);
	}
	
	public metaioSDK()
	{
		// Must be called before any calls to the metaio SDK DLL
		adjustPath();
	}
	
	
#region Public fields

	// Tracking configuration (path to file or a string)
	[SerializeField]
	public String trackingConfiguration;
	
	// Device camera to start
	[SerializeField]
	public int cameraFacing = MetaioCamera.FACE_UNDEFINED;

	// Whether stereo rendering is enabled (note that there is no property for see-through because that can simply be
	// achieved by disabling the metaioDeviceCamera script)
	[SerializeField]
	private bool _stereoRenderingEnabled = false;
	public bool stereoRenderingEnabled
	{
		get
		{
			return _stereoRenderingEnabled;
		}
		set
		{
			_stereoRenderingEnabled = value;

			MetaioSDKUnity.setStereoRendering(_stereoRenderingEnabled ? 1 : 0);

			metaioTracker[] trackers = (metaioTracker[])FindObjectsOfType(typeof(metaioTracker));
			foreach (metaioTracker tracker in trackers)
			{
				tracker.stereoRenderingEnabled = _stereoRenderingEnabled;
			}
		}
	}

	[SerializeField]
	private bool _seeThroughEnabled = false;
	public bool seeThroughEnabled
	{
		get
		{
			return _seeThroughEnabled;
		}
		set
		{
			_seeThroughEnabled = value;

			if (MetaioSDKUnity.deviceCamera != null)
			{
				MetaioSDKUnity.deviceCamera.seeThroughEnabled = _seeThroughEnabled;
			}

			// Trackers have references to the mono/left/right cameras. So use that class to change see-through settings.
			metaioTracker[] trackers = (metaioTracker[])FindObjectsOfType(typeof(metaioTracker));
			foreach (metaioTracker tracker in trackers)
			{
				tracker.seeThroughEnabled = _seeThroughEnabled;
			}
		}
	}

	// Near clipping plane limit (default 50mm)
	[SerializeField]
	public float nearClippingPlaneLimit = 50f;
	
	// Far clipping plane limit (default 1000Km)
	[SerializeField]
	public float farClippingPlaneLimit = 1e+9f;

#endregion
	
#region Private fields
	/// <summary>
	/// Whether a GUI label should be shown, indicating that the application was not started with "-force-opengl".
	/// </summary> 
	private bool showWrongRendererError = false;

#endregion
	
#region Editor script fields

	public static String[] trackingAssets = {"None", "DUMMY", "GPS", "ORIENTATION", "LLA", "CODE", "QRCODE", "FACE", "StreamingAssets...", "Absolute Path or String...", "Generated"};
	
	[HideInInspector]
	[SerializeField]
	public int trackingAssetIndex;
	
	[HideInInspector]
	[SerializeField]
	public UnityEngine.Object trackingAsset = null;
	
#endregion
	
	void Awake()
	{	
		if (!SystemInfo.graphicsDeviceVersion.ToLowerInvariant().Contains("opengl"))
		{
			Debug.LogError("#######################\n" +
			               "It seems that another renderer than OpenGL is used, but OpenGL is required when using " +
			               "the metaio SDK. Please pass \"-force-opengl\" to the executable to enforce running " +
			               "with OpenGL.\n" +
			               "#######################");
			showWrongRendererError = true;
		}

		AssetsManager.extractAssets(true);	
	}

	void OnGUI()
	{
		if (showWrongRendererError)
		{
			Color backup = GUI.contentColor;
			GUI.contentColor = new Color(1, 0, 0); // red
			GUI.Label(new Rect(0, 0, Screen.width, 100), "Metaio SDK camera stream rendering cannot work without passing \"-force-opengl\" to the executable.");
			GUI.contentColor = backup;
		}
	}
	
	/// <summary>
	/// Parses the application signature from the file StreamingAssets/MetaioSDKLicense.xml.
	/// </summary>
	/// <returns>
	/// Signature as string, or empty string if signature not found or signature file does not exist. Never returns NULL.
	/// </returns>
	public string parseApplicationSignature()
	{
#if UNITY_EDITOR
		string licenseFilePath = Path.Combine(Application.streamingAssetsPath, "MetaioSDKLicense.xml");
#else
		string licenseFilePath = AssetsManager.getAssetPath("MetaioSDKLicense.xml");
#endif
		FileInfo licenseFileInfo = new FileInfo(licenseFilePath);

		if (licenseFileInfo.Exists)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(licenseFilePath);

			// Same structure as for native Windows applications with Metaio SDK (appKeys.xml), except that <AppID>
			// is unused, so we only need <SignatureKey> for Unity
			XmlNodeList rootList = doc.GetElementsByTagName("Keys");
			if (rootList.Count != 1)
			{
				Debug.LogError("MetaioSDKLicense.xml has wrong format");
			}
			else
			{
				string signatureKey = null;

				XmlNode root = rootList[0];

				foreach (XmlNode childNode in root.ChildNodes)
				{
					if (childNode.Name == "SignatureKey")
					{
						signatureKey = childNode.InnerText;
					}
				}
				
				if (string.IsNullOrEmpty(signatureKey))
				{
					// On Android/iOS, you *must* register the application and enter a signature (even for free
					// license), while on Windows/Mac, the SDK runs with free license if no signature given.
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
					Debug.LogError("Missing application signature");
#else
					Debug.Log("Missing application signature");
#endif
				}
				else
				{
					return signatureKey.Trim();
				}
			}
		}
		else
		{
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
					Debug.LogError("No file MetaioSDKLicense.xml found in StreamingAssets");
#else
					Debug.Log("No file MetaioSDKLicense.xml found in StreamingAssets");
#endif
		}

		return string.Empty;
	}
					
	public void writeApplicationSignature(string signatureKey)
	{
		// Code not needed in deployed application
#if UNITY_EDITOR
		string licenseFilePath = Path.Combine(Application.streamingAssetsPath, "MetaioSDKLicense.xml");

		DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);
		if (!dir.Exists)
		{
			try
			{
				dir.Create();
			}
			catch (Exception)
			{
			}
		}

		XmlDocument doc = new XmlDocument();
		doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
		XmlNode root = doc.AppendChild(doc.CreateElement("Keys"));
		XmlNode signatureKeyElement = root.AppendChild(doc.CreateElement("SignatureKey"));
		signatureKeyElement.AppendChild(doc.CreateTextNode(signatureKey));
		try
		{
			doc.Save(licenseFilePath);
		}
		catch (Exception e)
		{
			Debug.LogError("Failed to write MetaioSDKLicense.xml ("+signatureKey+"): " + e);
		}
#endif
	}

	void Start () 
	{
		int result = MetaioSDKUnity.createMetaioSDKUnity(parseApplicationSignature());
		if (result == 0)
			Debug.Log("metaio SDK created successfully");
		else
			Debug.LogError("Failed to create metaio SDK!");
				
		bool mustRestoreAutoRotation = false;
		if (Screen.orientation == ScreenOrientation.Unknown)
		{
			// In this case we know that auto-rotation was active, because else Unity would immediately set a certain
			// default orientation (as defined in player settings).
			mustRestoreAutoRotation = true;

			Debug.Log("Fixing unknown orientation problem");

			switch (Input.deviceOrientation)
			{
				case DeviceOrientation.PortraitUpsideDown:
					Screen.orientation = ScreenOrientation.PortraitUpsideDown;
					break;
				case DeviceOrientation.LandscapeLeft:
					Screen.orientation = ScreenOrientation.LandscapeLeft;
					break;
				case DeviceOrientation.LandscapeRight:
					Screen.orientation = ScreenOrientation.LandscapeRight;
					break;
				case DeviceOrientation.FaceDown:
				case DeviceOrientation.FaceUp:
				case DeviceOrientation.Portrait:
				case DeviceOrientation.Unknown:
				default:
					Screen.orientation = ScreenOrientation.Portrait;
					break;
			}
		}

		MetaioSDKUnity.updateScreenOrientation(Screen.orientation);

		if (mustRestoreAutoRotation)
		{
			Screen.orientation = ScreenOrientation.AutoRotation;
		}

		Debug.Log("Starting the default camera with facing: "+cameraFacing);
		MetaioSDKUnity.startCamera(cameraFacing);
		
		// Load tracking configuration
		if (String.IsNullOrEmpty(trackingConfiguration))
		{
			Debug.Log("No tracking configuration specified");

			result = MetaioSDKUnity.setTrackingConfiguration("", 0);
		}
		else
		{
			result = MetaioSDKUnity.setTrackingConfigurationFromAssets(trackingConfiguration);

			if (result == 0)
				Debug.LogError("Start: failed to load tracking configuration: "+trackingConfiguration);
			else
				Debug.Log("Loaded tracking configuration: "+trackingConfiguration);
		}

		// Set LLA objects' rendering limits
		MetaioSDKUnity.setLLAObjectRenderingLimits(10, 1000);
		
		// Set renderer clipping plane limits
		MetaioSDKUnity.setRendererClippingPlaneLimits(nearClippingPlaneLimit, farClippingPlaneLimit);

		// Apply initial settings for mono/stereo and (non-)see-through mode
		stereoRenderingEnabled = _stereoRenderingEnabled;
		seeThroughEnabled = _seeThroughEnabled;
	}
	
	void OnDisable()
	{
		Debug.Log("OnDisable: deleting metaio sdk...");
		
		// stop camera before deleting the instance
		MetaioSDKUnity.stopCamera();
		
		// delete the instance
		MetaioSDKUnity.deleteMetaioSDKUnity();
	}
	
	void OnApplicationPause(bool pause)
	{
		Debug.Log("OnApplicationPause: "+pause);

		if (pause)
		{
			MetaioSDKUnity.onPauseApplication();
		}
		else
		{
			MetaioSDKUnity.onResumeApplication();
		}
	}
	
}
