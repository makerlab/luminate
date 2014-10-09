
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

//
// This draws different kinds of 3d lines including many segmented ribbons as well as simple swatches (short ribbons basically)
//
//    + a ribbon is made out of features
//    + each feature has an orientation and a size - later i may support color per feature
//    + a user grows the ribbon incrementally and then a finish routine can be called to polish things optionally
//    + a cheap culling strategy is used to remove features that are deemed not useful for describing the line (too close, coplanar)
//    + a more expensive culling strategy globally optimizes the line when done (douglas peucker )
//    + this code produces a real 3d shadow line on a ground plane as an option; this may be more useful than shader based shadows
//    + lines are double sided as a bonus; this is expensive and it would be nice to avoid by writing a custom shader
//	  + works at the unity mesh level - although arguably it should be rewritten to do opengl calls directly to reduce memory thrashing
//
//	  - i've disabled prisms and boxes for now
//	  - affine textures need to be dealt with; possibly a custom shader
//	  - may want to write custom shaders for ios to do glow effects more nicely
//

public class Swatch3d: MonoBehaviour {
	
	public bool EnableShadows = true;
	public bool IncrementalDouglasPeucker = false; // does not apply to swatch type
	public Material mainMaterial;
	public Material shadowMaterial;
	
	public enum STYLE {
		NONE,
		CURSIVE_SINGLE_SIDED,
		CURSIVE_DOUBLE_SIDED,
		SWATCH,
		//BOX,
		//PRISM,
		//GLOW,
		//SPARKLE,
		//BLOCK,
		//PUTTY,
		//MAGNETIC
	};
	
	public STYLE style = STYLE.NONE;

	// A beefy base class collection of various kinds of painterly attributes shared by various kinds of swatch types
	GameObject main;
	GameObject bottom;
	GameObject shadow;
	Mesh mainMesh;
	Mesh bottomMesh;
	Mesh shadowMesh;

	// An index for tracking dirty clean state
	int cleanIndex = 0;

	// Total Swatch Count
	static int TotalCount = 0;

	// Setup...
	public void setup(Color _color, STYLE _style = STYLE.NONE, Material _material = null, Material _shadowMaterial = null) {

		TotalCount++;

		cleanIndex = 0;
		
		style = _style;
		
		// Set materials supplied else use default else crash
		if(_material != null) mainMaterial = _material;
		if(_shadowMaterial !=null) shadowMaterial = _material;

		// Always clone the top material so we can modify it without affecting other swatches
		mainMaterial = Object.Instantiate(mainMaterial) as Material;
		mainMaterial.color = _color;
		
		// build top of line
		main = gameObject;
		main.name = "Draw"+TotalCount;
		main.renderer.material = mainMaterial;
		mainMesh = gameObject.GetComponent<MeshFilter>().mesh;
		
		// build bottom of line
		// NOTE using geometry not shader for backfaces... arguable either way : http://danielbrauer.com/files/rendering-double-sided-geometry.html
		if(true) {
			bottom = new GameObject( "Bottom" + TotalCount );
			bottom.transform.parent = gameObject.transform;
			bottom.AddComponent<MeshFilter>();
			bottom.AddComponent<MeshRenderer>();
			bottom.renderer.material = mainMaterial;
			bottomMesh = bottom.GetComponent<MeshFilter>().mesh;
		}
		
		// build shadow
		// NOTE separate geometry is used for this due to material changes not being supported on a per polygon basis in Unity
		if(EnableShadows) {
			shadow = new GameObject( "Shadow" + TotalCount );
			shadow.transform.parent = gameObject.transform;
			shadow.AddComponent<MeshFilter>();
			shadow.AddComponent<MeshRenderer>();
			shadow.renderer.material = shadowMaterial;
			shadowMesh = shadow.GetComponent<MeshFilter>().mesh;
		}
	}
	

	public void destroy() {
		if(main!=null)GameObject.Destroy (main);
	}

	public void test() {
		
		Vector3 xyz;

		xyz = new Vector3(0,10,0);
		paintConsider(xyz,Vector3.right,3,false);
		
		xyz = new Vector3(0,10,20);
		paintConsider(xyz,Vector3.right,4,false);
		
		xyz = new Vector3(0,30,20);
		paintConsider(xyz,Vector3.right,5,false);
		
		xyz = new Vector3(0,40,10);
		paintConsider(xyz,Vector3.right,6,false);
		
		xyz = new Vector3(0,20,-40);
		paintConsider(xyz,Vector3.right,7,false);

		paintFinish();
	}
		
	//--------------------------------------------------------------------------------------------------------------------------
	
	// accumulated user input describing users drawing action through 3d space
	public List<Vector3> points = new List<Vector3>();
	public List<Vector3> rights = new List<Vector3>();
	public List<float> velocities = new List<float>();

	// produced collections of vertices in a form that lets me incrementally extend the set
	// NOTE the unity C# interface requires an array of the exact size as the output so no savings are had by static large buffers
	public List<Vector3> vertices = new List<Vector3>();
	public List<Vector2> uvs = new List<Vector2>();
	public List<int> triangles = new List<int>();
	//public List<Vector3> normals = new List<Vector3>();
	//public List<Color> colors = new List<Color>();

	// extra state information about where exactly we are painting
	Ray ray;
	public void paintSetRay(Ray _ray) {
		ray = _ray;
	}

	// call this to add points
	public bool paintConsider(Transform transform,float width = 3.0f ) {

		// TODO - use the ray to modify the position in the plane so we can draw with the mouse

		return paintConsider(transform.position,transform.right,width,true);
	}

	// or call this to add points
	public bool paintConsider(Vector3 xyz,Vector3 right,float width,bool toGeometry = true) {

		if(style == STYLE.SWATCH) {

			if(points.Count < 2) {
				points.Add(xyz);
				rights.Add(right);
				velocities.Add(width);
			} else {
				points[1] = xyz;
				rights[1] = right;
				velocities[1] = width;
			}
			
		} else {

			// If not doing a full blown early optimization then at least do a quick test to discard the many cospatial points
			if(IncrementalDouglasPeucker == false && (points.Count > 0 && (xyz-points[points.Count-1]).sqrMagnitude < sqrToleranceMin)) {
				return false;
			}

			points.Add(xyz);
			rights.Add(right);
			velocities.Add(width);
		}

		if(toGeometry) {
			paintToGeometry(IncrementalDouglasPeucker,false);
		}

		return true;
	}

	// optional final polish pass; simplifies line and lets unity optimize geometry; do not do incremental sanitization if did it before
	public bool paintFinish() {
		return paintToGeometry(IncrementalDouglasPeucker ? false : true,true);
	}

	// convert our points into unity geometry
	public bool paintToGeometry(bool sanitize = false, bool compress = false) {
		
		// Do nothing if we have too little to chew on
		if(points.Count < 2) return false;

		// For swatches just reset the line for now - xxx slightly hacked and lazy
		if(style == STYLE.SWATCH) {
			vertices = new List<Vector3>();
			uvs = new List<Vector2>();
			triangles = new List<int>();
			cleanIndex = 0;
			sanitize = false;
		}

		// Optionally can sanitize user input every time ( but never at all if is a swatch style)
		if(sanitize) {
			// pass user input through a sanitization filter
			int beforeLength = points.Count;
			simplifyDouglasPeucker();
			int afterLength = points.Count;
			if(afterLength < beforeLength ) {
				// it appears there was cleanup somewhere in the array; so force remake the display geometry completely
				vertices = new List<Vector3>();
				uvs = new List<Vector2>();
				triangles = new List<int>();
				cleanIndex = 0;
			} else {
				// appears there was no optimization; this isn't strictly an accurate test... should checksum ideally
			}
		}
		
		// produce intermediate vertices and triangles for any new points (ones marked dirty)
		for(int i = cleanIndex; i < points.Count;i++) {
			paintToGeometry2(points[i],rights[i],velocities[i]);
		}

		cleanIndex = points.Count;

		paintToGeometry3(compress);

		return true;
	}

	float uvx = 0;

	public void paintToGeometry2(Vector3 point, Vector3 right, float width) {

		// give the line dimensionality perpendicular to axis of travel...
		vertices.Add ( point - right * width );
		vertices.Add ( point + right * width );

		// NOTE for now UV's don't matter much but we'll have to refine the technique when supporting materials
		// TODO deal with affine textures or don't share vertices between triangles
		// TODO deal with different kinds of UV scales; sometimes we want world scale sometimes we want use an object scale
		// http://forum.unity3d.com/threads/correcting-affine-texture-mapping-for-trapezoids.151283/
		uvs.Add(new Vector2(-1.0f,uvx));
		uvs.Add(new Vector2(1.0f,uvx));
		uvx += 1.0f;

		// attach triangles
		// NOTE multiple triangles share vertex normals so lighting and shadows are not fully discrete and will be softer
		if(vertices.Count >= 4) {
			int j = vertices.Count - 4;
			triangles.Add(j+0); triangles.Add(j+1); triangles.Add (j+2);
			triangles.Add(j+1); triangles.Add(j+3); triangles.Add (j+2);
		}
	}

	public void paintToGeometry3(bool optimize=false) {

		// NOTE due to limits of C# and Unity we are forced to remake the mesh from scratch; cannot use static large arrays

		Vector3[] v = vertices.ToArray();
		Vector2[] uv = uvs.ToArray();
		int[] tri = triangles.ToArray();

		// Build top side of ribbon as a geometry

		mainMesh.Clear();
		mainMesh.vertices = v;
		mainMesh.uv = uv;
		mainMesh.triangles = tri;
		mainMesh.RecalculateNormals();
		//topMesh.RecalculateBounds();
		if(optimize) mainMesh.Optimize();
		
		// Build bottom side
		for(int i = 0; i < tri.Length; i+=3) { int val = tri[i+1]; tri[i+1]=tri[i+2]; tri[i+2]=val; }
		
		bottomMesh.Clear();
		bottomMesh.vertices = v;
		bottomMesh.uv = uv;
		bottomMesh.triangles = tri;
		bottomMesh.RecalculateNormals();
		//bottomMesh.RecalculateBounds();
		if(optimize) bottomMesh.Optimize();

		// Build shadow	
		for(int i=0; i < v.Length; i++) v[i].y = 0;
		
		if(EnableShadows == false) return;
		shadowMesh.Clear();
		shadowMesh.vertices = v;
		shadowMesh.uv = uv;
		shadowMesh.triangles = tri;
		//shadowMesh.RecalculateNormals();
		//shadowMesh.RecalculateBounds();
		if(optimize) shadowMesh.Optimize();

	}

	
	//--------------------------------------------------------------------------------------------------------------------------
	//
	// notes
	//	
	// drawing a fat line in 2d requires handling sharp corners elegantly
	// see http://www.codeproject.com/Articles/226569/Drawing-polylines-by-tessellation
	// and https://gist.github.com/anselm/1474156
	// https://www.mapbox.com/blog/drawing-antialiased-lines/
	//
	
	/* obsolete
	public void pointToGeometry(Vector3 point, Vector3 up, Vector3 right, float width, Color c) {
		
		int shape = 0;
		bool isNew = vertices.Count > 0 ? false : true;
		int j = 0;

		Vector3 v1,v2,v3,v4;
		Vector2 u1,u2,u3,u4;
		
		switch(shape) {
		case 0:
			// calligraphy - a zero thickness ribbon
			v1 = point - right * width; // transform.TransformPoint(new Vector3 (-width, 0, 0)) ; // to the left
			v2 = point + right * width; //transform.TransformPoint(new Vector3 ( width, 0, 0)) ; // to the right
			vertices.Add ( v1 );
			vertices.Add ( v2 );
			normals.Add (up * -1 );
			normals.Add (up * -1 );
			u1 = new Vector2(-1,uvx); // we grow along the axis
			u2 = new Vector2(1,uvx);
			uvx += 0.5f;
			uvs.Add (u1);
			uvs.Add (u2);
			colors.Add( c );
			colors.Add( c );
			if(!isNew) {
				j = vertices.Count - 6;
				triangles.Add(j+0); triangles.Add(j+1); triangles.Add (j+4);
				triangles.Add(j+1); triangles.Add(j+5); triangles.Add (j+4);
			}

			// bottom has to be separate vertices for normals to work sigh
			vertices.Add ( v1 );
			vertices.Add ( v2 );
			normals.Add (up);
			normals.Add (up);
			uvs.Add (u1);
			uvs.Add (u2);
			colors.Add( c );
			colors.Add( c );
			if(!isNew) {
				j = vertices.Count - 6;
				triangles.Add(j+0); triangles.Add(j+4); triangles.Add (j+1);
				triangles.Add(j+1); triangles.Add(j+4); triangles.Add (j+5);
			}
			break;
		case 1:
			// a prism
			v1 = point - right * width; // transform.TransformPoint(new Vector3 (-width, 0, 0));
			v2 = point + right * width; // transform.TransformPoint(new Vector3 ( width, 0, 0));
			v3 = point + up * width; //  transform.TransformPoint(new Vector3 ( 0, width, 0 ));
			vertices.Add ( v1 );
			vertices.Add ( v2 );
			vertices.Add ( v3 );
			normals.Add (up); // XXX wrong
			normals.Add (up);
			normals.Add (up);
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
			normals.Add (up); // XXX wrong
			normals.Add (up);
			normals.Add (up);
			normals.Add (up);
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
	*/
	
	//--------------------------------------------------------------------------------------------------------------------------
	// https://github.com/mourner/simplify-js/blob/3d/simplify.js
	//

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
	// utility

	const float sqrToleranceMin = 0.1f;
	
	public void simplifyDouglasPeucker(float Tolerance = sqrToleranceMin) {

		if (points == null || points.Count < 4) return;
		
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
		List<Vector3> rights2 = new List<Vector3>();
		List<float> velocities2 = new List<float>();			
		foreach (int i in pointIndexsToKeep) {
			points2.Add(points[i]);
			rights2.Add(rights[i]);
			velocities2.Add(velocities[i]);
		}
		points = points2;
		rights = rights2;
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
	
};
