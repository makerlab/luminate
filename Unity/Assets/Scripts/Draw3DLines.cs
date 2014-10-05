using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using metaio;


// This is a mesh based 3d ribbon line drawing utility for real 3d lines
//
//    + a ribbon is made out of features
//    + each feature has an orientation and a size - later i may support color per feature
//    + a user grows the ribbon by calling consider() with new data. When done growing call finish() to optimize the line optionally.
//    + a cheap culling strategy is used to remove features that are deemed not useful for describing the line (too close, coplanar)
//    + a more expensive culling strategy globally optimizes the line when done (douglas peucker )
//    + this code produces a real 3d shadow line on a ground plane as an option; this may be more useful than shader based shadows
//    + lines are double sided as a bonus; this is expensive and it would be nice to avoid based on shader however
//	  + works at the unity mesh level - although arguably it should be rewritten to do opengl calls directly to reduce memory thrashing
// 
//    - lines can be 2d ribbons, 3d prisms or boxes - only 2d ribbons are being used or tested by me (prisms and boxes probably fail)
//
//	  - to do textured 3d ribbons of varying sizes a custom shader will be needed to deal with bilinear interpolation
//		this has not been done yet so textured ribbons are not going to be perfect... see affine distortion
//
//    - i'd like to package up a variety of other effects here like cheap fast bloom - but ios/android don't support post fx shaders
//
//	  - may need to write some custom shaders to deal with light and shadow more effectively
//

class MyLine {

	public bool EnableShadows = true;
	
	public enum STYLE {
		NONE,
		SHADOW,
		CURSIVE_SINGLE_SIDED,
		CURSIVE_DOUBLE_SIDED,
		BOX,
		PRISM,
		//GLOW,
		//SPARKLE,
		//BLOCK,
		//PUTTY,
		//MAGNETIC
	};

	public STYLE style = STYLE.NONE;

	const int MAXLEN = 255;
	Color color = Color.white;
	Mesh mesh;
	Mesh shadowmesh;

	GameObject obj;
	GameObject shadow;
	
	List<Vector3> points = new List<Vector3>();
	List<Vector3> ups = new List<Vector3>();
	List<Vector3> rights = new List<Vector3>();
	List<Vector3> forwards = new List<Vector3>();
	List<float> velocities = new List<float>();
	
	List<Vector3> vertices = new List<Vector3>();
	List<Vector2> uvs = new List<Vector2>();
	List<int> triangles = new List<int>();
	List<Color> colors = new List<Color>();
	
	static int TotalLineCount = 0;
	
	public MyLine(Color _color, STYLE style = STYLE.CURSIVE_DOUBLE_SIDED, Material material = null, Material shadowmaterial = null) {

		TotalLineCount++;

		// clone the requested material so that local instance does not change color when the material changes color
		material = Object.Instantiate(material) as Material;
		
		material.color = color = _color;
		
		// make a new gameobj/mesh for the line		
		obj = new GameObject( "Ribbon" + TotalLineCount ); 
		obj.AddComponent<MeshFilter>();
		obj.AddComponent<MeshRenderer>();
		if(material != null) obj.renderer.material = material;
		mesh = obj.GetComponent<MeshFilter>().mesh;
		
		// make a shadow
		if(EnableShadows) {
			shadow = new GameObject( "Shadow" + TotalLineCount );
			shadow.transform.parent = obj.transform;
			shadow.AddComponent<MeshFilter>();
			shadow.AddComponent<MeshRenderer>();
			if(shadowmaterial!=null) shadow.renderer.material = shadowmaterial;
			shadowmesh = shadow.GetComponent<MeshFilter>().mesh;
		}
		
	}
	
	public void destroy() {
		GameObject.Destroy(obj);
		GameObject.Destroy(shadow);
	}
	
	//--------------------------------------------------------------------------------------------------------------------------
	// https://github.com/mourner/simplify-js/blob/3d/simplify.js

	float getSquareSegmentDistance(Vector3 p, Vector3 p1, Vector3 p2) {
		float x = p1.x, y = p1.y, z = p1.z,	dx = p2.x - x, dy = p2.y - y, dz = p2.z - z;
		if (dx != 0 || dy != 0 || dz != 0) {
			float t = ((p.x - x) * dx + (p.y - y) * dy + (p.z - z) * dz) / (dx * dx + dy * dy + dz * dz);
			if (t > 1) {
				x = p2.x;
				y = p2.y;
				z = p2.z;
			} else if (t > 0) {
				x += dx * t;
				y += dy * t;
				z += dz * t;
			}
		}
		dx = p.x - x;
		dy = p.y - y;
		dz = p.z - z;
		return dx * dx + dy * dy + dz * dz;
	}
	
	List<Vector3> simplifyRadialDistance(List<Vector3> points, float sqTolerance) {
		Vector3 p1 = points[0];
		List<Vector3> newPoints = new List<Vector3>();
		newPoints.Add(p1);
		Vector3 p2 = p1;
		for (int i = 1, len = points.Count; i < len; i++) {
			p2 = points[i];
			float dx=p1.x-p2.x, dy=p1.y-p2.y, dz=p1.z-p2.z;
			if(dx*dx + dy*dy + dz*dz > sqTolerance) {
				newPoints.Add(p2);
				p1 = p2;
			}
		}
		if (p2 != p1) {
			newPoints.Add(p2); // might as well keep where the player is currently focused
		}
		return newPoints;
	}
/*	
	// simplification using optimized Douglas-Peucker algorithm with recursion elimination
	void simplifyDouglasPeuckerOld(float sqTolerance) {
		
		int len = points.Count;
		
		int[] markers = new int[len];
		
		int first = 0;
		int last = len - 1;
		
		int index = 0;
		
		List<int> firstStack = new List<int>();
		List<int> lastStack = new List<int>();
		markers[first] = markers[last] = 1;

		while (last != null) {
			float maxSqDist = 0;
			for (int i = first + 1; i < last; i++) {
				float sqDist = getSquareSegmentDistance(points[i], points[first], points[last]);
				if (sqDist > maxSqDist) {
					index = i;
					maxSqDist = sqDist;
				}
			}
			
			if (maxSqDist > sqTolerance) {
				markers[index] = 1;
				firstStack.Add(first);
				lastStack.Add(index);
				firstStack.Add(index);
				lastStack.Add(last);
			}

			if (firstStack.Count > 1) { first = firstStack[firstStack.Count-1]; firstStack.RemoveAt(firstStack.Count - 1); } else first = 0;
			if (lastStack.Count > 1) { last = lastStack[lastStack.Count-1]; lastStack.RemoveAt(lastStack.Count - 1); } else last = 0;
		}
		
		// recopy
		List<Vector3> points2 = new List<Vector3>();
		List<Vector3> ups2 = new List<Vector3>();
		List<Vector3> rights2 = new List<Vector3>();
		List<Vector3> forwards2 = new List<Vector3>();
		List<float> velocities2 = new List<float>();
		for (int i = 0; i < len; i++) {
			if (markers[i]!=0) {
				points2.Add(points[i]);
				ups2.Add(ups[i]);
				rights2.Add(rights[i]);
				forwards2.Add(forwards[i]);
				velocities2.Add(velocities[i]);
			}
		}
		points = points2;
		ups = ups2;
		rights = rights2;
		forwards = forwards2;
		velocities = velocities2;
		
	}
*/
	//--------------------------------------------------------------------------------------------------------------------------
	
	public void simplifyDouglasPeucker(float Tolerance)
	{
		if (points == null || points.Count < 3) return;
		
		int firstPoint = 0;
		int lastPoint = points.Count - 1;
		List<int> pointIndexsToKeep = new List<int>();
		
		pointIndexsToKeep.Add(firstPoint);
		pointIndexsToKeep.Add(lastPoint);
		
		while (points[firstPoint].Equals(points[lastPoint])) {
			lastPoint--;
		}
		
		DouglasPeuckerReduction(firstPoint, lastPoint, Tolerance, ref pointIndexsToKeep);
		pointIndexsToKeep.Sort();
		
		//Debug.Log ("Number of points coming into the system was " + points.Count );
		
		List<Vector3> points2 = new List<Vector3>();
		List<Vector3> ups2 = new List<Vector3>();
		List<Vector3> rights2 = new List<Vector3>();
		List<Vector3> forwards2 = new List<Vector3>();
		List<float> velocities2 = new List<float>();			
		foreach (int i in pointIndexsToKeep) {
			points2.Add(points[i]);
			ups2.Add(ups[i]);
			rights2.Add(rights[i]);
			forwards2.Add(forwards[i]);
			velocities2.Add(velocities[i]);
		}
		points = points2;
		ups = ups2;
		rights = rights2;
		forwards = forwards2;
		velocities = velocities2;

		//Debug.Log ("Number of points after system was " + points.Count );
		
	}
	
	private void DouglasPeuckerReduction(int firstPoint, int lastPoint, float tolerance, ref List<int> pointIndexsToKeep) {
		float maxDistance = 0;
		int indexFarthest = 0;
		
		for (int index = firstPoint; index < lastPoint; index++) {
			float distance = getSquareSegmentDistance(points[index], points[firstPoint], points[lastPoint]);
			//float distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
			if (distance > maxDistance) {
				maxDistance = distance;
				indexFarthest = index;
			}
		}
		
		if (maxDistance > tolerance && indexFarthest != 0) {
			pointIndexsToKeep.Add(indexFarthest);
			DouglasPeuckerReduction(firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
			DouglasPeuckerReduction(indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
		}
	}
	
	//--------------------------------------------------------------------------------------------------------------------------
	
	float uvx = 0;
	float uvz = 0;
	
	public void pointToGeometry(Vector3 point, Vector3 up, Vector3 right, float width, Color c) {
		
		int shape = 0;
		
		Vector3 v1,v2,v3,v4;
		Vector2 u1,u2,u3,u4;
		
		switch(shape) {
		case 0:
			// calligraphy - a zero thickness ribbon
			v1 = point - right * width; // transform.TransformPoint(new Vector3 (-width, 0, 0)) ; // to the left
			v2 = point + right * width; //transform.TransformPoint(new Vector3 ( width, 0, 0)) ; // to the right
			vertices.Add ( v1 );
			vertices.Add ( v2 );
			u1 = new Vector2(-1,uvx); // we grow along the axis
			u2 = new Vector2(1,uvx);
			uvx += 0.5f;
			uvs.Add (u1);
			uvs.Add (u2);
			colors.Add( c );
			colors.Add( c );
			if(vertices.Count < 4) break;
			int j = vertices.Count - 4;
			triangles.Add(j+0); triangles.Add(j+1); triangles.Add (j+2);	// top
			triangles.Add(j+1); triangles.Add(j+3); triangles.Add (j+2);
			triangles.Add(j+0); triangles.Add(j+2); triangles.Add (j+1);	// bottom - not needed for one sided
			triangles.Add(j+1); triangles.Add(j+2); triangles.Add (j+3);
			break;
		case 1:
			// a prism
			v1 = point - right * width; // transform.TransformPoint(new Vector3 (-width, 0, 0));
			v2 = point + right * width; // transform.TransformPoint(new Vector3 ( width, 0, 0));
			v3 = point + up * width; //  transform.TransformPoint(new Vector3 ( 0, width, 0 ));
			vertices.Add ( v1 );
			vertices.Add ( v2 );
			vertices.Add ( v3 );
			u1 = new Vector2(uvx,0);
			u2 = new Vector2(uvx,0.5f);
			u3 = new Vector2(uvx,1);
			uvx += 0.1f; if(uvx>1)uvx=0;
			uvs.Add ( u1 );
			uvs.Add ( u2 );
			uvs.Add ( u3 );
			colors.Add( c );
			colors.Add( c );
			colors.Add( c );
			j = vertices.Count - 6;
			if(vertices.Count < 6) {
				//				triangles.Add(0); triangles.Add(1); triangles.Add (2);
				//				triangles.Add(0); triangles.Add(1); triangles.Add (2);
				break;
			}
			
			//			triangles.RemoveAt (triangles.Count-1);
			//			triangles.RemoveAt (triangles.Count-1);
			//			triangles.RemoveAt (triangles.Count-1);
			
			triangles.Add(j+0); triangles.Add(j+4); triangles.Add (j+1);
			triangles.Add(j+0); triangles.Add(j+3); triangles.Add (j+4);
			triangles.Add(j+1); triangles.Add(j+5); triangles.Add (j+2);
			triangles.Add(j+1); triangles.Add(j+4); triangles.Add (j+5);
			triangles.Add(j+2); triangles.Add(j+3); triangles.Add (j+0);
			triangles.Add(j+2); triangles.Add(j+5); triangles.Add (j+3);
			
			//			triangles.Add(j+3); triangles.Add(j+5); triangles.Add (j+4);
			
			break;
		case 2:
			// a cube
			v1 = point + (-right -up) * width; // transform.TransformPoint(new Vector3 (-width,-width, 0));
			v2 = point + ( right -up) * width; // transform.TransformPoint(new Vector3 ( width,-width, 0));
			v3 = point + (-right +up) * width; // transform.TransformPoint(new Vector3 (-width, width, 0 ));
			v4 = point + (right + up) * width; // transform.TransformPoint(new Vector3 ( width, width, 0 ));
			vertices.Add ( v1 );
			vertices.Add ( v2 );
			vertices.Add ( v3 );
			vertices.Add ( v4 );
			u1 = new Vector2(uvx,0);
			u2 = new Vector2(uvx,1);
			u3 = new Vector2(uvx,0);
			u4 = new Vector2(uvx,1);
			uvx += 0.1f; if(uvx>1)uvx=0;
			uvs.Add ( u1 );
			uvs.Add ( u2 );
			uvs.Add ( u3 );
			uvs.Add ( u4 );
			colors.Add( c );
			colors.Add( c );
			colors.Add( c );
			colors.Add( c );
			j = vertices.Count - 8;
			if(vertices.Count < 8) {
				//triangles.Add(0); triangles.Add(1); triangles.Add (2);
				//				triangles.Add(0); triangles.Add(1); triangles.Add (2);
				break;
			}
			
			//			triangles.RemoveAt (triangles.Count-1);
			//			triangles.RemoveAt (triangles.Count-1);
			//			triangles.RemoveAt (triangles.Count-1);
			
			triangles.Add(j+0); triangles.Add(j+5); triangles.Add (j+1);
			triangles.Add(j+0); triangles.Add(j+4); triangles.Add (j+5);
			triangles.Add(j+1); triangles.Add(j+5); triangles.Add (j+7);
			triangles.Add(j+1); triangles.Add(j+7); triangles.Add (j+3);
			triangles.Add(j+3); triangles.Add(j+7); triangles.Add (j+6);
			triangles.Add(j+3); triangles.Add(j+6); triangles.Add (j+2);
			triangles.Add(j+2); triangles.Add(j+6); triangles.Add (j+4);
			triangles.Add(j+2); triangles.Add(j+4); triangles.Add (j+0);
			
			//triangles.Add(j+3); triangles.Add(j+5); triangles.Add (j+4);
			
			break;
		}
		
	}
	
	const float sqrToleranceMin = 0.1f;
	
	public bool consider(Transform transform, Color c, float width = 3.0f ) {
		
		// if we already have some points then only add new points if they are far enough away
		if(points.Count > 0 && (transform.position-points[points.Count-1]).sqrMagnitude < sqrToleranceMin) {
			return false;
		}
		
		// it is difficult to remove collinear points at this time so we will hold onto them for now and run a global optimization later.
		
		points.Add(transform.position);
		rights.Add(transform.right);
		ups.Add (transform.up);
		forwards.Add (transform.forward);
		velocities.Add(width);
		
		// continue building the geometry based on this data; this is a stand in for a global optimization later when the line is done
		
		pointToGeometry(transform.position,transform.up, transform.right, width, c);
		
		// copy the entire thing over from scratch to the mesh because of limits in unity
		
		rebuildMesh();
		
		return true;
	}
	
	public bool finish(Color c) {
		
		// rewrites the points in place
		simplifyDouglasPeucker(sqrToleranceMin);
		
		// and sadly we have to remake the vertices because the global optimization is operating on a differently sequenced collection
		vertices = new List<Vector3>();
		uvs = new List<Vector2>();
		triangles = new List<int>();
		colors = new List<Color>();		
		for(int i = 0; i < points.Count;i++) {
			pointToGeometry(points[i],ups[i],rights[i],velocities[i],c);
		}
		
		// throw away everything if we have too little data - caller must handle this
		if(points.Count < 5) return false;
		
		// and recopy
		rebuildMesh();

		// enough data to be useful
		return true;
	}
	
	
	void rebuildMesh() {
	
		// XXX TODO this code thrashes memory but there is no way around it except to switch down to C++ or use OpenGL directly
	
		mesh.Clear();
		mesh.vertices = vertices.ToArray ();
		mesh.uv = uvs.ToArray ();
		mesh.triangles =  triangles.ToArray();
		mesh.colors = colors.ToArray();
		mesh.RecalculateNormals();
		
		if(EnableShadows) {
			// TODO the shadow mesh doesn't really need a bottom...
			int[] copy_of_triangles = triangles.ToArray();
			Vector3[] copy_of_vertices = vertices.ToArray();
			Vector2[] copy_of_uvs = uvs.ToArray();
			for(int i=0; i < copy_of_vertices.Length; i++) copy_of_vertices[i].y = 0;
			shadowmesh.Clear();
			shadowmesh.vertices = copy_of_vertices;
			shadowmesh.uv = copy_of_uvs;
			shadowmesh.triangles = copy_of_triangles;
			shadowmesh.RecalculateNormals();		
		}
	}
	
};

/// this is the brush tip effectively; fiddling with color or material here sets subsequent ribbon traits

public class Draw3DLines : MonoBehaviour {
	
	public Color color = Color.red;
	public Material material;
	public Material shadow;
	public Transform target;

	MyLine.STYLE style = MyLine.STYLE.CURSIVE_DOUBLE_SIDED;
	
	List<MyLine> lines = new List<MyLine>();
	MyLine line;

	void Update () {
		
		// physics based smoothing - move towards target
		transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * 10.0f );
		transform.rotation = Quaternion.Slerp (transform.rotation, target.rotation, Time.deltaTime * 10.0f );
		
		// on new touch event start a trail
		RuntimePlatform platform = Application.platform;
		if(platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer){
			if(Input.touchCount > 0) {
				if(Input.GetTouch(0).phase == TouchPhase.Began){
					float x = Input.GetTouch(0).position.x;
					float y = Input.GetTouch (0).position.y;
					if(x > 100 && y > 100 ) {
						line = new MyLine(color,style,material,shadow);
						lines.Add(line);
					}
				}
				if(Input.GetTouch(0).phase == TouchPhase.Ended){
					if(line!=null)line.finish(color);
					line = null;
				}
			}
		} else if(platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer){
			if(Input.GetMouseButtonUp(0)) {
				if(line!=null)line.finish(color);
				line = null;
			}
			if(Input.GetMouseButtonDown(0)) {
				Debug.Log ( Input.mousePosition.x + " " + Input.mousePosition.y );
				if(Input.mousePosition.x > 100 && Input.mousePosition.y > 100) {
					line = new MyLine(color,style,material,shadow);
					lines.Add(line);
				}
			}
		}
		
		// pass the current position for consideration
		if(line!=null) {
			line.consider(this.transform,Color.white,3);
		}
		
	}
	
	
	void SetColor(HSBColor color) {
		this.color = color.ToColor();
	}

	void OnGUI() {

		if(GUI.Button(new Rect(10,10,100,80),"Track")) {
			MetaioSDKUnity.startInstantTracking("INSTANT_3D", "");
			GUI.enabled = false;
		}
		
		if (GUI.Button(new Rect(120,580, 80, 40), "undo")) {
			if(lines.Count>0) { 
				MyLine line = lines[lines.Count-1];
				lines.RemoveAt(lines.Count-1);
				line.destroy();
			}
		}
	}	
}

// ribbons oct 15 2015
//
//		- i would like to try vary the line width by speed of drawing
//
//		- i would like to try harder to adjust ribbon brightness based on incident angle of sunlight; custom shader? or?
//
//		- i would like to try do a glow on the ribbon - play with glow shader
//
//		- correct interpolation on textures would let me draw swatches more easily and this would look quite good
//
//		- draw smoke, fog, particles and other kinds of things
//
//		- try using the default shadow support rather than my own custom shadows so that ribbons can self shadow
//
//	<	- try making real world pickers for color choices and for tips and for loading and saving
//
//		- try a minecraft block style paint brush tip
//
// other
//
//		- multiplayer
//
//		- time limit game
//
//		- replay build process
//
//		- save and save to web
//
//		- have a menu of previous files

