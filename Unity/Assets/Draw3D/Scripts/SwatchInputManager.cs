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
	
	// Shared state for drawable things
	public GameObject prefabSwatch;
	public Color color = Color.green;
	public Material material;
	Swatch3d.STYLE style = Swatch3d.STYLE.SWATCH;
	public float DrawSizeDefault = 8.0f;

	// current focus of drawing
	GameObject focus;
	
	// ----------------------------------------------------------------------------
	
	void SetMaterial(Material m) {
		material = Object.Instantiate(m) as Material;
		material.color = color;		
		brush.renderer.material = m;
		target.renderer.material = m;
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

	bool HandleButtons(Vector3 input) {
		// look for button events
		Ray ray = Camera.main.ScreenPointToRay(input);
		RaycastHit hit;
		if( Physics.Raycast (ray,out hit) ) {
			HandleBounce(hit.transform);
			switch(hit.transform.name) {
				case "Palette1": SetColor(hit.transform.gameObject.renderer.material.color); return true;
				case "Palette2": SetColor(hit.transform.gameObject.renderer.material.color); return true;
				case "Palette3": SetColor(hit.transform.gameObject.renderer.material.color); return true;
				case "Palette4": SetColor(hit.transform.gameObject.renderer.material.color); return true;
				case "Palette5": SetColor(hit.transform.gameObject.renderer.material.color); return true;
				case "Palette6": SetColor(hit.transform.gameObject.renderer.material.color); return true;
				case "Swatch1":  SetMaterial(hit.transform.gameObject.renderer.material); return true;
				case "Swatch2":  SetMaterial(hit.transform.gameObject.renderer.material); return true;
				case "SaveExit": break;
				case "Sunlight": break;
				case "Undo":     Undo(); return true;
				case "Redo":     break;
				case "Ribbon":   style = Swatch3d.STYLE.CURSIVE_DOUBLE_SIDED; return true;
				case "Swatch":   style = Swatch3d.STYLE.SWATCH; return true;
				default: break;
			}
		}
		return false;
	}

	void HandleDown(Vector3 input) {

		// block out 2d gui button area for now
		// TODO remove when we remove track gui button
		if(input.x < 100 && input.y < 100) return;

		// check for 3d in world button events
		if(HandleButtons(input)) {
			return;
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

/*
		Ray ray = camera.ScreenPointToRay(input);
		Plane plane = new Plane(brush.transform.up, xyz);
		float distance = 0; // this will return the distance from the camerass
		if (plane.Raycast(ray, out distance)){ // if plane hit...
			Debug.Log ("a hit at " + ray.GetPoint (distance));
		} else {
			Debug.Log ("NO HIT");
		}
		
		Debug.Log ("is at " + xyz);


		art.paintSetRay(ray);

		Plane plane = new Plane(Vector3.up, Vector3.zero);
		float distance = 0; // this will return the distance from the camerass
		if (plane.Raycast(ray, out distance)){ // if plane hit...
			Debug.Log ("hit at " + ray.GetPoint (distance));
		}

		input.z = 300.0f;
		 ray = camera.ScreenPointToRay(input);
		art.paintSetRay(ray);
		Debug.Log ("ray is " + ray );
*/
		Vector3 xyz = brush.transform.position;
		Vector3 right = brush.transform.right;
		Vector3 forward = brush.transform.forward;
		input.z = 400;
		Vector3 point = Camera.main.ScreenToWorldPoint(input);
		xyz = point;

		art.paintConsider(xyz,forward,right,DrawSizeDefault);
	}

	void HandleUp() {
		Debug.Log ("Finished " + focus );
		if(focus==null) return;
		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;
		if(art.paintFinish() == false) {
			Destroy(art);
		}
		focus = null;
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
	
	void SetColor(HSBColor color) {
		this.color = color.ToColor();
	}

	void OnGUI() {

		if(GUI.Button(new Rect(10,10,100,80),"TRACK")) {
			MetaioSDKUnity.startInstantTracking("INSTANT_3D", "");
		}

	}	
}


/*

Small

- buttons: animation could be better for buttons
- buttons: maybe keep indicating current selection
- buttons: should be labelled or new better art
- buttons: there are curious grainy glitches on them

- do we really need the center of brush attached to the camera now?

< test undo and shake undo

- tracker: have some other tracker modes?
- tracker: have tracker be a button now?

Larger

- I still don't have a sense of "where I am" when painting; need to try different kinds of ideas to provide a sense of working plane

- Arguably the ribbon should rotate to face the direction of travel; does this mean I should be doing 2d bevels also then? on a plane?
  Maybe this can be a separate mode of ribbon since we would lose the cursive quality
  Shadows also may be optional
  And ribbon width should vary by speed

- Try a block style

- Smoke, Fog, Lights, Sparklers

UX

	- get rid of the track button - auto track after a while?
	- test constant delauny
	
	- debate having hard shadows on or off... could be a user option

	- save
	- undo
	- clear
	- fatness
	- speed games
	- multiplayer
	- publish		
	- main menu showing all files
	- website using webgl showing art off

Release

	- video
	- bug tracker
	- clean up source
	- hashtag, url etc

*/
