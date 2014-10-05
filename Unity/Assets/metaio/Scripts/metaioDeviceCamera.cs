using UnityEngine;
using System.Collections;
using System;
using metaio;

[RequireComponent(typeof(Camera))]
public class metaioDeviceCamera : MonoBehaviour 
{
	// camera plane on which camera image is rendered
	GameObject cameraPlane;
	
	// true when camera texture has been created
	bool textureCreated;
	
	// camera texture id
	int textureID;
	
	// currently used screen orientation
	ScreenOrientation screenOrientation;
	
	// currently used screen (viewport) size
	Vector2 screenSize;
	
	// Defines the current dimensions of the camera texture so that we can check if the texture must be recreated, in
	// case a larger camera resolution (or setImage input frame) is requested
	private uint currentTextureSize = 0;
	
	private Texture2D texture;
	
	void Awake()
	{
		MetaioSDKUnity.deviceCamera = this;
	}
	
	// Use this for initialization
	void Start () 
	{
	
		Camera cam = GetComponent(typeof(Camera)) as Camera;
		cam.orthographic = true;
		
		// initialize camera plane object
		cameraPlane = transform.FindChild("CameraPlane").gameObject;
		cameraPlane.renderer.material.shader = Shader.Find("metaio/UnlitTexture");
		cameraPlane.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
		
		screenOrientation = ScreenOrientation.Unknown;
		screenSize.x = 0;
		screenSize.y = 0;
		
	}

	// Update is called once per frame
	void Update() 
	{
		if (updateScreenOrientation())
		{
			
			// also update size/orientation of metaio SDK and camera projection
			// matrix
			MetaioSDKUnity.resizeRenderer(Screen.width, Screen.height);
			MetaioSDKUnity.updateScreenOrientation(screenOrientation);
			metaioCamera.updateCameraProjectionMatrix();	
		}
		
		// Try to create the texture (must call every frame in case required size changes)
		textureCreated = createTexture(0);
		
		if (textureCreated)
		{
			updateCameraPlaneScale();
		}
		
		// TODO: GL.IssuePluginEvent should be used on all platforms,
		// but in Unity 3.5.x, the callbacks in the native plugin are
		// never called. Check if they work in Unity 4.x.
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
		GL.IssuePluginEvent(textureID);
#elif UNITY_ANDROID || UNITY_IPHONE
		MetaioSDKUnity.UnityRenderEvent(textureID);
#endif
		
	}
	
	/// <summary>
	/// Applys screen orientation to camera preview plane
	/// </summary>
	/// <returns>
	/// true when screen orientation is changed and applied to the camera plane.
	/// </returns>
	private bool updateScreenOrientation()
	{
		bool screenSizeChanged =!(screenSize.x == Screen.width && screenSize.y == Screen.height);

		ScreenOrientation currentScreenOrientation = Screen.orientation;
		
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
		currentScreenOrientation = ScreenOrientation.LandscapeLeft;
#endif
		
		bool screenOrientationChanged = !(screenOrientation == currentScreenOrientation);
		
		// if screen orientation or size has not changed, return false
		if (!(screenSizeChanged || screenOrientationChanged))
		{
			return false;
		}
		
		screenOrientation = currentScreenOrientation;
		
		// Find real viewport size
		GameObject sceneCam = GameObject.Find("SceneCamera");
		if (sceneCam != null)
		{
			screenSize.x = sceneCam.camera.pixelRect.width;
			screenSize.y = sceneCam.camera.pixelRect.height;
		}
		else
		{
			// If that object wasn't found just assume we're fullscreen
			screenSize.x = Screen.width;
			screenSize.y = Screen.height;
		}
		
		// update orthographic size
		Camera cam = GetComponent(typeof(Camera)) as Camera;
		cam.orthographicSize = getOrthographicSize(screenOrientation);
		Debug.Log("Camera orthographic size: "+cam.orthographicSize);
		
		// update camera plane rotation
		cameraPlane.transform.localRotation = Quaternion.AngleAxis(270.0f, Vector3.right);
		
		Debug.Log("Screen orientation: "+screenOrientation);
		
		
		switch (screenOrientation)
		{
		case ScreenOrientation.Portrait:
			cameraPlane.transform.localRotation *= Quaternion.AngleAxis(90.0f, Vector3.up);
			break;
		case ScreenOrientation.LandscapeRight:
            cameraPlane.transform.localRotation *= Quaternion.AngleAxis(180.0f, Vector3.up);
        	break;
		case ScreenOrientation.PortraitUpsideDown:
            cameraPlane.transform.localRotation *= Quaternion.AngleAxis(270.0f, Vector3.up);
			break;
        }
		
		return true;
	}
	
	/// <summary>
	/// Determine size of the orthographic camera based on screen orientation.
	/// </summary>
	/// <returns>
	/// The orthographic camera size.
	/// </returns>
	/// <param name='orientation'>
	/// Screen orientation
	/// </param>
	private static float getOrthographicSize(ScreenOrientation orientation)
	{
		if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
			return 1.0f;
		
		if (Screen.width < Screen.height)
			return ((float)Screen.width)/((float)Screen.height);
		else
			return ((float)Screen.height)/((float)Screen.width);
	}
	
	public void prepareForPotentialTextureSizeChange(uint changingToTextureSize)
	{
		createTexture(changingToTextureSize);
	}
	
	/// <summary>
	/// Create camera texture and set it to the camera plane
	/// </summary>
	/// <returns>
	/// true when texture is created, else false
	/// </returns>
	public bool createTexture(uint enforceSize)
	{
		uint requiredSize;
		if(enforceSize > 0)
			requiredSize = enforceSize;
		else
			requiredSize = MetaioSDKUnity.getRequiredTextureSize();
		
		if (requiredSize <= 0)
			return false;
		
		// Reuse old texture if required size didn't change
		if (textureCreated && currentTextureSize == requiredSize)
			return true;
		
		// Create the texture that will hold camera frames
		
		// Android, iOS and OSX
		TextureFormat textureFormat = TextureFormat.RGBA32;
		
		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
		{
			// Windows
			textureFormat = TextureFormat.RGB24;
		}
		
		Debug.Log("Creating texture with size " + requiredSize + " and format " + textureFormat);
		
		texture = new Texture2D((int)requiredSize, (int)requiredSize, textureFormat, false);		
		
		if (texture == null)
			return false;
		
		currentTextureSize = requiredSize;

		cameraPlane.renderer.material.mainTexture = texture;
		textureID = texture.GetNativeTextureID();

		Debug.Log("Texture ID: "+textureID);
		
		return true;
	}
	
	public void updateCameraPlaneScale()
	{
		// determine scale of the camera plane
		float scale = MetaioSDKUnity.getCameraPlaneScale();
	
		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
		{
			// Windows
			cameraPlane.transform.localScale = new Vector3(-scale, scale, -scale);
		}
		else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
		{
			// OSX
			cameraPlane.transform.localScale = new Vector3(-scale, scale, scale);
		}
		else if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
		{
			// Android and iOS
			cameraPlane.transform.localScale = new Vector3(-scale, scale, scale);
		}
		else
		{
			Debug.LogError("Unsupported platform: "+Application.platform);
		}
	}
}
