using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using metaio;

/// Implements a concept of a brush tip - different kinds of painterly modes are set here and draw different kinds of art

public class SwatchInputManager : MonoBehaviour {

	// A concept of a physical brush that chases an idealized target to smooth out line drawing
	public Transform brush;
	public Transform target;
	public Transform choice1;
	public Transform choice2;
	public Light sunlight;
	
	// Shared state for drawable things
	public GameObject prefabSwatch;
	public Color color = Color.green;
	public Material material;
	Swatch3d.STYLE style = Swatch3d.STYLE.TUBE;
	public float DrawSizeDefault = 8.0f;
	
	// current focus of drawing
	GameObject focus;
	bool ignoreFingerPosition = false;
	
	// ----------------------------------------------------------------------------
	
	void SetMaterial(Material m) {
		material = Object.Instantiate(m) as Material;
		material.color = color;		
		brush.renderer.material = material;
		target.renderer.material = material;
	}

	void SetColor(Color c) {
		color = c;
		material.color = color;
		brush.renderer.material = material;
		target.renderer.material = material;
	}

	float bounce = 0;
	Transform t;
	void HandleBounce(Transform _t) {
		t = _t;
		bounce = 10;
	}

	void HandleUpdate() {
		if(bounce < 1) return;
		bounce--;
		float size = 0.8f + (10.0f-bounce)/50.0f;
		t.localScale = new Vector3(size,size,size);
	}
	
	void SetChoice(GameObject g) {
		choice1.transform.position = g.transform.position;
	}
	
	void SetChoice2(GameObject g) {
		choice2.transform.position = g.transform.position;
	}
	
	bool HandleButtons(Vector3 input) {
		// look for button events
		Ray ray = Camera.main.ScreenPointToRay(input);
		RaycastHit hit;
		if( Physics.Raycast (ray,out hit) ) {
			HandleBounce(hit.transform);
			switch(hit.transform.name) {
				case "Palette1": SetColor(hit.transform.gameObject.renderer.material.color); SetChoice(hit.transform.gameObject); return true;
				case "Palette2": SetColor(hit.transform.gameObject.renderer.material.color); SetChoice(hit.transform.gameObject); return true;
				case "Palette3": SetColor(hit.transform.gameObject.renderer.material.color); SetChoice(hit.transform.gameObject); return true;
				case "Palette4": SetColor(hit.transform.gameObject.renderer.material.color); SetChoice(hit.transform.gameObject); return true;
				case "Palette5": SetColor(hit.transform.gameObject.renderer.material.color); SetChoice(hit.transform.gameObject); return true;
				case "Palette6": SetColor(hit.transform.gameObject.renderer.material.color); SetChoice(hit.transform.gameObject); return true;
				case "Swatch1":  SetMaterial(hit.transform.gameObject.renderer.material); return true;
				case "Swatch2":  SetMaterial(hit.transform.gameObject.renderer.material); return true;
				case "Track": 	 Track(); return true;
				case "SaveExit": SaveAndExit(); return true;
				case "Sun": 	 SetSunPosition(); SetChoice2(hit.transform.gameObject); return true;
				case "Undo":     Undo(); SetChoice2(hit.transform.gameObject); return true;
				case "Tube":     style = Swatch3d.STYLE.TUBE; SetChoice2(hit.transform.gameObject); return true;
				case "Ribbon":   style = Swatch3d.STYLE.CURSIVE_DOUBLE_SIDED; SetChoice2(hit.transform.gameObject); return true;
				case "Swatch":   style = Swatch3d.STYLE.SWATCH; SetChoice2(hit.transform.gameObject); return true;
				default: break;
			}
		}
		return false;
	}

	void HandleDown(Vector3 input) {

		// check for 3d in world button events
		if(HandleButtons(input)) {
			return;
		}

		// ignore finger position if you are painting in the right corner
		if( (input.x > Screen.width - 100) && (input.y < 100)) {
			brush.gameObject.SetActive (true);
			brush.position = target.position;
			ignoreFingerPosition = true;
		} else {
			brush.gameObject.SetActive (true);
			ignoreFingerPosition = false;
		}

		// start new art
		focus = Instantiate(prefabSwatch) as GameObject;
		focus.transform.parent = this.transform;
		
		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;
		art.setup(color,style,material);
		HandleMove(input);
	}

	void HandleMove(Vector3 input) {
		if(focus==null) return;

		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;

		Vector3 xyz = brush.transform.position;
		Vector3 right = brush.transform.right;
		Vector3 forward = brush.transform.forward;
		
		if(ignoreFingerPosition == false) {
			// If the user is moving their finger around then use that as a draw hint
			input.z = 400;
			xyz = brush.position = Camera.main.ScreenToWorldPoint(input);
		}

		art.paintConsider(xyz,forward,right,DrawSizeDefault);
	}

	void HandleUp() {
		brush.gameObject.SetActive(false);
		Debug.Log ("Finished " + focus );
		if(focus==null) return;
		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;
		focus = null;
		if(art.paintFinish() == false) {
			Destroy(art);
		}
	}

	// ----------------------------------------------------------------------------
	
	// shake detection
	const float accelerometerUpdateInterval = 1.0f / 60.0f;
	// The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa).
	const float lowPassKernelWidthInSeconds = 1.0f;
	// This next parameter is initialized to 2.0 per Apple's recommendation, or at least according to Brady! ;)
	float shakeDetectionThreshold = 2.0f;
	
	private float lowPassFilterFactor = accelerometerUpdateInterval / lowPassKernelWidthInSeconds;
	private Vector3 lowPassValue = Vector3.zero;
	private Vector3 acceleration = Vector3.zero;
	private Vector3 deltaAcceleration = Vector3.zero;
	int shakelatch = 0;

	void ShakeStart() {
		shakeDetectionThreshold *= shakeDetectionThreshold;
		lowPassValue = Input.acceleration;
	}

	void ShakeUpdate() {
		acceleration = Input.acceleration;
		lowPassValue = Vector3.Lerp(lowPassValue, acceleration, lowPassFilterFactor);
		deltaAcceleration = acceleration - lowPassValue;
		if(shakelatch>0) { shakelatch--; return; }
		if (deltaAcceleration.sqrMagnitude < shakeDetectionThreshold) return;
		if( transform.childCount < 1) return;
		Undo();
		shakelatch = 30;
	}

	void Undo() {
		if( transform.childCount < 1) return;
		Destroy(transform.GetChild ( transform.childCount - 1 ).gameObject );
	}

	// ----------------------------------------------------------------------------

	void Start() {
		material = Object.Instantiate(material) as Material;
		Screen.orientation = ScreenOrientation.LandscapeLeft;
		ShakeStart();

		// test();	
	}

	void test() {
	
		focus = Instantiate(prefabSwatch) as GameObject;
		focus.transform.parent = this.transform;
		
		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;
		art.setup(color,style,material);
		
		art.test ();
		
		focus = null;
	
	}

	void Update () {

		// update
		ShakeUpdate();
		HandleUpdate();
				
		// physics based smoothing - cursor move towards target to smoothen out user interaction
		brush.transform.position = Vector3.Lerp(brush.transform.position, target.position, Time.deltaTime * 10.0f );
		brush.transform.rotation = Quaternion.Slerp(brush.transform.rotation, target.rotation, Time.deltaTime * 10.0f );
		
		// deal with platform specific mouse down and move and up events
		RuntimePlatform platform = Application.platform;
		if(platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer){
			if(Input.touchCount > 0) {
				Vector3 position = Input.GetTouch (0).position;
				switch(Input.GetTouch(0).phase) {
					case TouchPhase.Began: HandleDown(position); break;
					case TouchPhase.Ended: HandleUp(); break;
					default: HandleMove(position); break;
				}
			}
		} else if(platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer){
			if(Input.GetMouseButtonDown(0)) {
				HandleDown(Input.mousePosition);
			}
			else if(Input.GetMouseButtonUp(0)) {
				HandleUp();
			} else {
				HandleMove(Input.mousePosition);
			}
		}		
	}
	
	void Track() {
		MetaioSDKUnity.startInstantTracking("INSTANT_3D", "");
	}

	void SetSunPosition() {
		// do it
		sunlight.transform.rotation = Camera.main.transform.rotation;
	}
	
	void SaveAndExit() {
	}

}

