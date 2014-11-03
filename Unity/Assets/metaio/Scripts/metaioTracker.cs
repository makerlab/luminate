using UnityEngine;
using System;
using metaio;

public class metaioTracker : MonoBehaviour 
{
#region Public fields
	// per default, we transform the camera instead of the objects
	// Note: if you need to transform multiple objects, the camera should be fixed (e.g. set to false)
	public bool transformCamera = true;

	// link to the main camera
	private Camera cameraToPositionMono;
	private Camera cameraToPositionLeft;
	private Camera cameraToPositionRight;

	// COS ID
	[SerializeField]
	public int cosID = 1;	
	
	// the texture, needed for editor
	[SerializeField]
	public Texture2D texture;

	[SerializeField]
	public String trackingImage;
	
	[SerializeField]
	public Vector2 geoLocation;
	
	[SerializeField]
	public Boolean enableLLALimits;
	
	// true if we want to use a preconfigure pose
	public bool simulatePose = false;
	
#endregion	

	private bool _stereoRenderingEnabled;
	internal bool stereoRenderingEnabled
	{
		get
		{
			return _stereoRenderingEnabled;
		}
		set
		{
			// Note that seeThroughEnabled setter calls this setter because the flags must be combined
			_stereoRenderingEnabled = value;

			if (cameraToPositionMono != null)
			{
				cameraToPositionMono.enabled = !_stereoRenderingEnabled;
			}
			if (cameraToPositionLeft != null)
			{
				cameraToPositionLeft.enabled = _stereoRenderingEnabled;
			}
			if (cameraToPositionRight != null)
			{
				cameraToPositionRight.enabled = _stereoRenderingEnabled;
			}
		}
	}

	private bool _seeThroughEnabled;
	internal bool seeThroughEnabled
	{
		get
		{
			return _seeThroughEnabled;
		}
		set
		{
			_seeThroughEnabled = value;

			// Enable/disable cameras based on stereo/see-through mode (no need to duplicate code)
			stereoRenderingEnabled = _stereoRenderingEnabled;
		}
	}

	// buffer to hold temporary cartesian translations
	private float[] translation;
	
	// Holds temprary tracking values
	private float[] trackingValues;	
	
	// true if childs' rendering is enabled, else false
	private bool childsEnabled;
		
	void Awake() 
	{
		trackingValues = new float[7];
		translation = new float[3];
		
		cameraToPositionMono = null;
		cameraToPositionLeft = null;
		cameraToPositionRight = null;
		
		if (Camera.main.GetComponent<metaioCamera>() != null)
		{
			cameraToPositionMono = Camera.main;
		}
		
		metaioCamera[] cameras = (metaioCamera[])FindObjectsOfType(typeof(metaioCamera));
		foreach (metaioCamera camera in cameras)
		{
			Camera unityCamera = camera.gameObject.GetComponent<Camera>();
			if (camera.cameraType == CameraType.RenderingMono)
			{
				if (cameraToPositionMono != null && cameraToPositionMono != unityCamera)
				{
					Debug.LogWarning("Two cameras have the metaioCamera script attached and are marked as mono camera. Choosing main camera.");
				}
				else
				{
					cameraToPositionMono = unityCamera;
				}
			}
			else if (camera.cameraType == CameraType.RenderingStereoLeft)
			{
				if (cameraToPositionLeft != null)
				{
					Debug.LogWarning("Two cameras have the metaioCamera script attached and are marked as left camera. Choosing first one.");
				}
				else
				{
					cameraToPositionLeft = unityCamera;
				}
			}
			else if (camera.cameraType == CameraType.RenderingStereoRight)
			{
				if (cameraToPositionRight != null)
				{
					Debug.LogWarning("Two cameras have the metaioCamera script attached and are marked as right camera. Choosing first one.");
				}
				else
				{
					cameraToPositionRight = unityCamera;
				}
			}
		}

		if (transformCamera && cameraToPositionMono == null)
		{
			Debug.LogError("Metaio plugin didn't find camera to position. Please use the metaioSDK prefab, or create a camera with the 'metaioCamera' script attached.");
		}

		if (cameraToPositionLeft == null || cameraToPositionRight == null)
		{
			Debug.LogWarning("No left/right camera for Metaio SDK stereo rendering found - this feature won't be available. Please make sure you imported the latest metaioSDK.unitypackage.");
		}

		childsEnabled = true;
	}

	void Start()
	{
		// Now that camera references are found, apply settings (stereoRenderingEnabled setter takes care of both
		// stereo mode and see-through)
		stereoRenderingEnabled = _stereoRenderingEnabled;
	}

	// Update is called once per frame
	void Update()
	{
		if(simulatePose)
		{
			// use this predefined pose
			Quaternion q = new Quaternion(-0.2f, -0.8f, 0.4f, 0.4f);
			Vector3 p = new Vector3(14.7f, 12.8f, 203.0f);

			setCameraOrTrackerPosition(p,q);

			return;
		}
		
		int isTracking = MetaioSDKUnity.getTrackingValues(cosID, trackingValues);
		// Debug.Log("cosID " + cosID + ", isTracking: " + isTracking);
		
		if (isTracking > 0)
		{
			// Metaio SDK: RHS with X=right (on marker) Y=up (on marker, i.e. back) Z=up (away from marker)
			// Unity: LHS with X=right Y=up Z=back

			Quaternion q;
			q.x = -trackingValues[3];
			q.y = -trackingValues[4];
			q.z = trackingValues[5];
			q.w = trackingValues[6];
			Quaternion mul = new Quaternion(1, 0, 0, -1);
			q *= mul;

			//translation
			Vector3 p;
			p.x = trackingValues[0];
			p.y = trackingValues[1];
			p.z = -trackingValues[2];
			
//			Debug.Log("Cartesian translation: "+p);
			
			// Apply geo-location if specified
			if (geoLocation.x != 0f && geoLocation.y != 0f)
			{
				// convert LLA to cartesian translation
				MetaioSDKUnity.convertLLAToTranslation(geoLocation.x, geoLocation.y, enableLLALimits?1:0, translation);
//				Debug.Log("LLA translation: "+translation[0]+", "+translation[1]+", "+translation[2]);
			
				// Augment LLA cartesian translation
				Vector3 tLLA;
				tLLA.x = translation[1];
				tLLA.y = translation[2];
				tLLA.z = -translation[0];
				p = p+q*tLLA;
			}
			
//			Debug.Log("Final translation: "+p);
			setCameraOrTrackerPosition(p,q);

			// show childs
			enableRenderingChilds(true);
		}
		else
		{
			// hide because target not tracked
			enableRenderingChilds(false);
		}
	}

	private void calcHandEyeCalibrationInverse(Matrix4x4 trackingMatrix, CameraType cameraType, out Matrix4x4 outInverted)
	{
		Vector3 translation;
		Quaternion rotation;
		MetaioSDKUnity.getHandEyeCalibration(out translation, out rotation, cameraType);

		Matrix4x4 rotationMatrix = new Matrix4x4();
		NormalizeQuaternion(ref rotation);
		rotationMatrix.SetTRS(Vector3.zero, 
		                      rotation,
		                      new Vector3(1.0f, 1.0f, 1.0f));

		Matrix4x4 translationMatrix = new Matrix4x4();
		translationMatrix.SetTRS(translation, 
		                         new Quaternion(0.0f, 0.0f, 0.0f, 1.0f),
		                         new Vector3(1.0f, 1.0f, 1.0f));
		
		Matrix4x4 composed = translationMatrix * rotationMatrix;

		composed *= trackingMatrix;

		outInverted = composed.inverse;
	}
	
	private void setCameraOrTrackerPosition(Vector3 p, Quaternion q)
	{
		// todo, make a function out of this, otherwhise its the same as metaioTracker.cs
		Matrix4x4 rotationMatrix = new Matrix4x4();
		NormalizeQuaternion(ref q);

		rotationMatrix.SetTRS(Vector3.zero, 
		                       q,
		                       new Vector3(1.0f, 1.0f, 1.0f));

		Matrix4x4 translationMatrix = new Matrix4x4();
		translationMatrix.SetTRS(p, 
		                       new Quaternion(0.0f, 0.0f, 0.0f, 1.0f),
		                       new Vector3(1.0f, 1.0f, 1.0f));
		
		Matrix4x4 composed = translationMatrix * rotationMatrix;

        if (transformCamera)
		{
			//center the camera in front of goal - z-axis			
			if (cameraToPositionMono != null && cameraToPositionMono.enabled)
			{
				Matrix4x4 hecMonoInverse;
				calcHandEyeCalibrationInverse(composed, CameraType.RenderingMono, out hecMonoInverse);
				
				cameraToPositionMono.transform.position = (Vector3)hecMonoInverse.GetColumn(3);
				cameraToPositionMono.transform.rotation = QuaternionFromMatrix(hecMonoInverse);
			}

			if (cameraToPositionLeft != null && cameraToPositionLeft.enabled)
			{
				Matrix4x4 hecLeftInverse;
				calcHandEyeCalibrationInverse(composed, CameraType.RenderingStereoLeft, out hecLeftInverse);

				cameraToPositionLeft.transform.position = (Vector3)hecLeftInverse.GetColumn(3);
				cameraToPositionLeft.transform.rotation = QuaternionFromMatrix(hecLeftInverse);
			}
			if (cameraToPositionRight != null && cameraToPositionRight.enabled)
			{
				Matrix4x4 hecRightInverse;
				calcHandEyeCalibrationInverse(composed, CameraType.RenderingStereoRight, out hecRightInverse);

				cameraToPositionRight.transform.position = (Vector3)hecRightInverse.GetColumn(3);
				cameraToPositionRight.transform.rotation = QuaternionFromMatrix(hecRightInverse);
			}
		}
		else
		{
			// This only works for mono mode
			Vector3 translation;
			Quaternion rotation;
			MetaioSDKUnity.getHandEyeCalibration(out translation, out rotation, CameraType.RenderingMono);

			transform.position = translation + p;
			transform.rotation = rotation * q;
		}
	}
	

	private void TransformFromMatrix(Matrix4x4 matrix, Transform trans) {
	    trans.rotation = QuaternionFromMatrix(matrix);
	    trans.position = matrix.GetColumn(3); // uses implicit conversion from Vector4 to Vector3
	}



	private Quaternion QuaternionFromMatrix(Matrix4x4 m) {
	    // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm

	    Quaternion q = new Quaternion();
	    q.w = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] + m[1,1] + m[2,2] ) ) / 2; 
	    q.x = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] - m[1,1] - m[2,2] ) ) / 2; 
	    q.y = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] + m[1,1] - m[2,2] ) ) / 2; 
	    q.z = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] - m[1,1] + m[2,2] ) ) / 2; 

	    q.x *= Mathf.Sign( q.x * ( m[2,1] - m[1,2] ) );
	    q.y *= Mathf.Sign( q.y * ( m[0,2] - m[2,0] ) );
	    q.z *= Mathf.Sign( q.z * ( m[1,0] - m[0,1] ) );

	    return q;
	}


	private void NormalizeQuaternion (ref Quaternion q)
	{
	    float sum = 0;
	    for (int i = 0; i < 4; ++i)
	        sum += q[i] * q[i];
	    float magnitudeInverse = 1 / Mathf.Sqrt(sum);
	    for (int i = 0; i < 4; ++i)
	        q[i] *= magnitudeInverse;
	}
	
	// Enable/disable rendering
	private void enableRenderingChilds(bool enable)
    {
		// Do nothing if enabled state is not changed
		if (childsEnabled == enable)
			return;
		
        Renderer[] rendererComponents = GetComponentsInChildren<Renderer>();

        foreach (Renderer component in rendererComponents) 
		{
            component.enabled = enable;
        }
		
		childsEnabled = enable;
		
    }
	
	public void ApplyModifications()
	{
		// thenn set the material
		// renderer.material.SetTexture( "_MainTex", texture ) ;
		Transform previewTransform = transform.FindChild("PreviewPlane");
		if(previewTransform==null)
			return;
		
		GameObject previewPlane = previewTransform.gameObject;
		
		// set the texture preview
		previewPlane.renderer.sharedMaterial.SetTexture( "_MainTex", texture ) ;
		
		// scale the plane
		previewPlane.transform.localScale = new Vector3( 20f*texture.texelSize.x*texture.width, 1, 20f*texture.texelSize.x*texture.width); 
		Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
		rotation.eulerAngles = new Vector3(0, -90, 0);
		previewPlane.transform.localRotation = rotation;
		
	}
	
	
}

