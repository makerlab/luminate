using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using metaio;

/// Implements a concept of a brush tip - different kinds of painterly modes are set here and draw different kinds of art

public class SwatchInputManager : MonoBehaviour {

	// For tracking 3d UX elements
	public Camera camera;

	// A concept of a physical brush that chases an idealized target to smooth out line drawing
	public Transform brush;
	public Transform target;
	
	// Sharead state for drawable things
	public GameObject prefabSwatch;
	public Color color = Color.green;
	public Material material;
	Swatch3d.STYLE style = Swatch3d.STYLE.SWATCH;

	// current focus of drawing
	GameObject focus;
	
	// ----------------------------------------------------------------------------

	bool HandleButtons(Vector3 input) {
		// look for button events
		Ray ray = camera.ScreenPointToRay(input);
		RaycastHit hit;
		if( Physics.Raycast (ray,out hit) ) {
			Debug.Log ( hit );
			Debug.Log ( hit.transform.name );
			if(hit.transform.name.StartsWith ("Palette")) { // TODO hack - build a more formal GUI widget system
				material = hit.transform.gameObject.renderer.material;
				color = material.color;
				Debug.Log ("set material");
				return true;
			}
		}
		return false;
	}

	void HandleDown(Vector3 input) {

		// block out 2d gui button area for now
		// TODO remove when we remove track gui button
		if(input.x < 100 && input.y < 100) return;

		// check for 3d in world button events
		if(HandleButtons(input)) return;

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
		Ray ray = camera.ScreenPointToRay(input);
		art.paintSetRay(ray);
		art.paintConsider(brush.transform,3);
	}

	void HandleUp() {
		if(focus==null) return;
		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;
		art.paintFinish();
		focus = null;
	}

	// ----------------------------------------------------------------------------
	// Test

	void Awake() {
		focus = Instantiate(prefabSwatch) as GameObject;
		Swatch3d art = focus.GetComponent<Swatch3d>() as Swatch3d;
		art.setup(color,style,material);
		art.test ();
		focus = null;
	}

	// ----------------------------------------------------------------------------
	
	void Update () {
		
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

		if (GUI.Button(new Rect(10,100,100,80), "UNDO")) {
			//if(lines.Count>0) { 
			//	Ribbon3d line = lines[lines.Count-1];
			//	lines.RemoveAt(lines.Count-1);
			//	line.destroy();
			//}
		}
	}	
}


/*

Small
	- shake to undo
	- get rid of the track button - auto track after a while
	- test constant delauny
	- larger buttons
	- rotate buttons 

Swatch Appearance


	- support a swatch type
		- it would be fabulous to support a 1 element swatch style with a ui draw
		- affine textures
		- 2d drawing option

	- support minecraft
	
	- smoke, fog, lights, etc

	- try vary line by speed

UX
			
	<- support various colors using 3d buttons
	
	- debate having hard shadows on or off... could be a user option

	- save
	
	- undo
	
	- clear

	- fatness
	
	- games
	
	- multiplayer

	- publish
		
	- main menu showing all files

*/
