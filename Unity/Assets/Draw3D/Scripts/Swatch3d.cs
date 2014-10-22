
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
//
// notes
//	
// drawing a fat line in 2d requires handling sharp corners elegantly - we don't need this yet... it is arguably useful
// there are some examples of tube and ribbon renderers in unity asset store as an option but i want everything to be freely reusable
// https://www.shadertoy.com/view/4sjXzG <- could use a shader instead? arguably has utility also
// acko.net has some beautiful work also
// see http://www.codeproject.com/Articles/226569/Drawing-polylines-by-tessellation
// and https://gist.github.com/anselm/1474156
// https://www.mapbox.com/blog/drawing-antialiased-lines/
// paul o'rourke had a great example - not visible on the net anymore
//

public class Swatch3d: MonoBehaviour {
	
	public bool EnableShadows = true;
	public bool EnableBottom = true;
	public bool IncrementalDouglasPeucker = false; // does not apply to swatch type
	public Material mainMaterial;
	public Material shadowMaterial;
	
	public enum STYLE {
		NONE,
		CURSIVE_SINGLE_SIDED,
		CURSIVE_DOUBLE_SIDED,
		SWATCH,
		TUBE,
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
		
		// set style aspects
		if(style == STYLE.SWATCH) {
			EnableShadows = false;
			IncrementalDouglasPeucker = false;
		}
		
		if(style == STYLE.TUBE) {
			EnableShadows = false;
			EnableBottom = false;
			initTubeHelper();
		}
		
		// Set materials supplied else use default else crash
		if(_material != null) mainMaterial = _material;
		if(_shadowMaterial !=null) shadowMaterial = _material;

		// Always clone the main material so we can modify it without affecting other swatches
		mainMaterial = Object.Instantiate(mainMaterial) as Material;
		mainMaterial.color = _color;
		
		if(true) {
			// build top of line
			main = gameObject;
			main.name = "Draw"+TotalCount;
			main.renderer.material = mainMaterial;
			mainMesh = gameObject.GetComponent<MeshFilter>().mesh;
		}
	
		if(EnableBottom) {
			// build bottom of line
			// NOTE using geometry not shader for backfaces... arguable either way : http://danielbrauer.com/files/rendering-double-sided-geometry.html
			bottom = new GameObject( "Bottom" + TotalCount );
			bottom.transform.parent = gameObject.transform;
			bottom.AddComponent<MeshFilter>();
			bottom.AddComponent<MeshRenderer>();
			bottom.renderer.material = mainMaterial;
			bottomMesh = bottom.GetComponent<MeshFilter>().mesh;
		}
		
		if(EnableShadows) {
			// build shadow
			// NOTE separate geometry is used for this due to material changes not being supported on a per polygon basis in Unity
			shadow = new GameObject( "Shadow" + TotalCount );
			shadow.transform.parent = gameObject.transform;
			shadow.AddComponent<MeshFilter>();
			shadow.AddComponent<MeshRenderer>();
			shadow.renderer.material = shadowMaterial;
			shadowMesh = shadow.GetComponent<MeshFilter>().mesh;
		}
		
	}

	public void test() {
		Vector3 xyz;
		Vector3 forward = Vector3.forward;
		Vector3 right = Vector3.right;
		float width = 10.0f;
		
		xyz = Vector3.zero;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100 + Vector3.right * 40;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100 + Vector3.right * 200;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100 + Vector3.right * 300;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100 + Vector3.right * 350;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100 + Vector3.right * 400;
		paintConsider(xyz,forward,right,width);
		xyz = Vector3.up * 100 + Vector3.right * 450;
		paintConsider(xyz,forward,right,width);
	}

	//--------------------------------------------------------------------------------------------------------------------------
	
	// accumulated user input describing users drawing action through 3d space
	public List<Vector3> points = new List<Vector3>();
	public List<Vector3> rights = new List<Vector3>();
	public List<Vector3> forwards = new List<Vector3>();
	public List<float> velocities = new List<float>();

	// produced collections of vertices in a form that lets me incrementally extend the set
	// NOTE the unity C# interface requires an array of the exact size as the output so no savings are had by static large buffers
	public List<Vector3> vertices = new List<Vector3>();
	public List<Vector2> uvs = new List<Vector2>();
	public List<int> triangles = new List<int>();
	//public List<Vector3> normals = new List<Vector3>();
	//public List<Color> colors = new List<Color>();

	// call this to add points
	public bool paintConsider(Vector3 xyz, Vector3 forward, Vector3 right,float width = 3.0f ) {

		if(style == STYLE.SWATCH) {

			if(points.Count < 2) {
				points.Add(xyz);
				rights.Add(Vector3.right);
				velocities.Add(width);
			} else {

				// a swatch is flattest at right angles to the intersection of the camera forward and swatch direction vector
				right = Vector3.Cross( forward, xyz - points[0] ).normalized * width;
								
				points[1] = xyz;
				rights[1] = right;
				rights[0] = right;
				velocities[1] = width;
			}
			
		} else {
			
			// If not doing a full blown early optimization then at least do a quick test to discard the many cospatial points
			if(IncrementalDouglasPeucker == false) {
				if((points.Count > 0 && (xyz-points[points.Count-1]).sqrMagnitude < sqrToleranceMin)) {
					return false;
				}
			}

			points.Add(xyz);
			rights.Add(right);
			forwards.Add(forward);
			velocities.Add(width);
		}

		paintToGeometry(false);

		return true;
	}

	// optional final polish pass; simplifies line and lets unity optimize geometry; do not do incremental sanitization if did it before
	public bool paintFinish() {
		return paintToGeometry(true);
	}

	// convert our points into unity geometry
	public bool paintToGeometry(bool compress = false) {
		
		// Do nothing if we have too little to chew on
		if(points.Count < 2) return false;

		if(style == STYLE.SWATCH) {
			// For swatches just reset the line for now - xxx slightly hacked and lazy
			vertices = new List<Vector3>();
			uvs = new List<Vector2>();
			triangles = new List<int>();
			cleanIndex = 0;
		} else {

			// For most line drawings - we're either incrementally optimizing or we optimize once at the end
			if(IncrementalDouglasPeucker != compress) {
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
		}
		
		// produce intermediate vertices and triangles for any new points (ones marked dirty)
		for(int i = cleanIndex; i < points.Count;i++) {
			if(style == STYLE.TUBE) {
				paintToTubeIntermediateRepresentation(i);
			} else {
				paintToRibbonIntermediateRepresentation(i);
			}
		}
		cleanIndex = points.Count;

		// make unity art
		paintToUnity3DRepresentation(compress);

		return true;
	}

	public void paintToRibbonIntermediateRepresentation(int p) {

		Vector3 point = points[p];
		Vector3 right = rights[p];
		float width = velocities[p];
		
		// verts
		vertices.Add ( point - right * width );
		vertices.Add ( point + right * width );

		// uvs
		uvs.Add(new Vector2(0,0));
		uvs.Add(new Vector2(1.0f,0));

		// tris
		if(p>0) {
			int j = vertices.Count - 4;
			triangles.Add(j+0); triangles.Add(j+1); triangles.Add (j+2);
			triangles.Add(j+1); triangles.Add(j+3); triangles.Add (j+2);
		}
	}

	const int crossSegments = 8;
	static Vector3[] crossPoints;
	static Quaternion rot = Quaternion.identity;
	int endCapVerts = 0;
	int endCapTris = 0;

	public void initTubeHelper() {
		crossPoints = new Vector3[crossSegments];
		float theta = 2.0f*Mathf.PI/crossSegments;
		for (int c=0;c<crossSegments;c++) {
			crossPoints[c] = new Vector3(Mathf.Cos(theta*c), Mathf.Sin(theta*c), 0);
		}
	}

	// this is from http://wiki.unity3d.com/index.php?title=TubeRenderer
	public void paintToTubeIntermediateRepresentation(int p) {
	
		// wait till we have enough data to do something useful
		if(p == 0) {
			return;
		}

		Vector3 point = points[p];
		Vector3 prev = points[p-1];
		Vector3 forward = forwards[p];
		const float capsize = 5.0f;
		Vector3 dir = (point-prev).normalized;
		float width = velocities[p];

		// get rotation of this segment
		rot = Quaternion.FromToRotation(Vector3.forward,point-prev);

		// inject a start cap
		if(p == 1) {
			for(float i = 0; i < capsize; i++) {
				float width2 = Mathf.Sin(Mathf.PI/2*i/capsize) * width;
				float height = Mathf.Cos(Mathf.PI/2*i/capsize) * width;
				for (int j=0;j<crossSegments;j++) {
					vertices.Add ( prev + rot * crossPoints[j] * width2 - dir * height );
					uvs.Add ( new Vector2(((float)j)/((float)crossSegments),0) );
					if(j==0)continue;
					int c = p*crossSegments+j;
					int d = p*crossSegments+((j+1)%crossSegments);
					int a = c-crossSegments;
					int b = d-crossSegments;
					triangles.Add(c); triangles.Add(a); triangles.Add(b);
					triangles.Add(c); triangles.Add(b); triangles.Add(d);
				}
			}			
		}

		// remove the end cap
		if(endCapVerts>0) {
			vertices.RemoveRange(endCapVerts,vertices.Count-endCapVerts);
			uvs.RemoveRange(endCapVerts,vertices.Count-endCapVerts);
			triangles.RemoveRange(endCapTris,triangles.Count-endCapTris);
		}

		// verts
		for (int j=0;j<crossSegments;j++) {
			vertices.Add ( point + rot * crossPoints[j] * width );
			uvs.Add ( new Vector2(((float)j)/((float)crossSegments),0) );
		}

		// triangles
		for (int j=0;p > 0 && j<crossSegments;j++) {
			int c = p*crossSegments+j;
			int d = p*crossSegments+((j+1)%crossSegments);
			int a = c-crossSegments;
			int b = d-crossSegments;
			triangles.Add(c); triangles.Add(a); triangles.Add(b);
			triangles.Add(c); triangles.Add(b); triangles.Add(d);
		}

		// add the end cap back on
		if(p == 7) {
			endCapVerts = vertices.Count;
			endCapTris = triangles.Count;
			for(float i = 1; i < capsize; i++) {
				float width2 = 10; //Mathf.Cos(Mathf.PI/2*i/capsize) * width * 10;
				float height = 19; // Mathf.Sin(Mathf.PI/2*i/capsize) * width * 10;
				for (int j=0;j<crossSegments;j++) {
					vertices.Add ( point + rot * crossPoints[j] * width2 + dir * height );
					uvs.Add ( new Vector2(((float)j)/((float)crossSegments),0) );
					int c = p*crossSegments+j;
					int d = p*crossSegments+((j+1)%crossSegments);
					int a = c-crossSegments;
					int b = d-crossSegments;
					triangles.Add(c); triangles.Add(a); triangles.Add(b);
					triangles.Add(c); triangles.Add(b); triangles.Add(d);
					Debug.Log (" added " + a );
				}
			}
		}
	}

	public void paintToUnity3DRepresentation(bool optimize=false) {

		// NOTE due to limits of C# and Unity we are forced to remake the mesh from scratch; cannot use static large arrays
		// NOTE it would be nice to have a static vertex or uv pool for intermediate representation rather than reallocating...

		Vector3[] v = vertices.ToArray();
		Vector2[] uv = uvs.ToArray();
		int[] tri = triangles.ToArray();
		
		// patch uv to be a single 0 to 1 span along length
		if(true) {
			for(int i = 0; i < uv.Length; i++) {
				uv[i][1] = ((float)i)/((float)uv.Length);
			}
		}

		if(true) {
			// Build top side of ribbon as a geometry
			mainMesh.Clear();
			mainMesh.vertices = v;
			mainMesh.uv = uv;
			mainMesh.triangles = tri;
			mainMesh.RecalculateNormals();
			//topMesh.RecalculateBounds();
			if(optimize) mainMesh.Optimize();
		}
	
		if(EnableBottom) {
			// Build bottom side
			for(int i = 0; i < tri.Length; i+=3) { int val = tri[i+1]; tri[i+1]=tri[i+2]; tri[i+2]=val; }		
			bottomMesh.Clear();
			bottomMesh.vertices = v;
			bottomMesh.uv = uv;
			bottomMesh.triangles = tri;
			bottomMesh.RecalculateNormals();
			//bottomMesh.RecalculateBounds();
			if(optimize) bottomMesh.Optimize();
		}

		if(EnableShadows) {
			// Build shadow	
			for(int i=0; i < v.Length; i++) v[i].y = 0;
			shadowMesh.Clear();
			shadowMesh.vertices = v;
			shadowMesh.uv = uv;
			shadowMesh.triangles = tri;
			//shadowMesh.RecalculateNormals();
			//shadowMesh.RecalculateBounds();
			if(optimize) shadowMesh.Optimize();
		}	
	}
	
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
		List<Vector3> forwards2 = new List<Vector3>();
		List<float> velocities2 = new List<float>();			
		foreach (int i in pointIndexsToKeep) {
			points2.Add(points[i]);
			rights2.Add(rights[i]);
			forwards2.Add(forwards[i]);
			velocities2.Add(velocities[i]);
		}
		points = points2;
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
	
};



