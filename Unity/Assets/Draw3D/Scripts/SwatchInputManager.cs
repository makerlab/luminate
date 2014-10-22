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
				case "Track": 	 Track(); return true;
				case "Sunlight": SetSunPosition(); return true;
				case "Undo":     Undo(); return true;
				case "Ribbon":   style = Swatch3d.STYLE.CURSIVE_DOUBLE_SIDED; return true;
				case "Swatch":   style = Swatch3d.STYLE.SWATCH; return true;
				case "SaveExit": SaveAndExit(); return true;
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

		test();	
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

/*

Things to try out,

bugs

< - almost certainly need do massively reduce point count in tubes and in general

revise ui:
	[] paint with a swatch
	[] paint with a brush
	[] paint with a neon
	[] freeze the drawing plane; perhaps also letting you rotate it with your finger even???
	[] move the sun
	[] erase with finger or tip ( anything touched gets erased with some touch latency

	< make a neon 4h
	< increase the delta for minimum movement and make the beam tip keep recycling the current end rather than rejecting? 1h
	< multi segment swatches 2h
	< smaller swatches? somehow control the size 2h
	< uv stretched swatches 2h
	< try make a main menu? 8h
	< try a blockworld? 8h
	< have ribbonizer do 2d curves support - https://www.assetstore.unity3d.com/en/#!/content/20272

To try

	- gridding or snap to nearest line?
	- arguably we could have a freezeable drawing plane not always orthogonal to the camera....? thoughts?	
	- maybe we could show the drawing plane as a semi-transparent div while drawing so that it helps you guage context?
			( i tried this before and it basically was just a box on the screen facing the camera so it was boring... try again?)
			
	- maybe semi transparent paint color is a good idea?
	< having a ball shadow on the ground would help

Other pen tips?

	- is it worth trying to get the standard assets bloom glow running on ios? can it run? is it fast enough?
	- is vectrosity worth looking at again to see if they've dealt with 3d lines better?
- Try a block style
- Smoke, Fog, Lights, Sparklers
- Shadows also may be optional
- And ribbon width should vary by speed

Small Issues,

	- buttons: animation could be better for buttons
	- buttons: maybe keep indicating current selection
	- buttons: should be labelled or new better art
	- tracker: have some other tracker modes?
	- tracker: have tracker be a button now?

SPATIAL SENSE PROBLEM:

- I still don't have a sense of "where I am" when painting; need to try different kinds of ideas to provide a sense of working plane

- Arguably the ribbon should rotate to face the direction of travel; does this mean I should be doing 2d bevels also then? on a plane?
  Maybe this can be a separate mode of ribbon since we would lose the cursive quality
 
UX

	- get rid of the track button - auto track after a while?
	- test constant delauny
	
	- debate having hard shadows on or off... could be a user option?

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
