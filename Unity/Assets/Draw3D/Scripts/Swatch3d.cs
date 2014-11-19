
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

//
// This source file is a big mish mash of different kinds of 3d line drawing routines I've been noodling with.
//
//    + Draws swatches ( simple textured quads ).
//	  + Draws 3d ribbons (that are not merely GL lines and not merely shader hacks but real geometry). These are double sided optionally.
//    + Draws 3d capped tubes out of ngons, prisms or tubular boxes.
//    + Has vertex reduction strategies ( douglas peucker and as well a simple "point too close to previous point" culling strategy)
//    + Draws a 3d geometry based shadow of the line on the ground as an option.
//    + Works with Unity Meshes ( which although inefficient do fit better within Unity scheme of managing things).
//    + Has an incremental drawing philosophy so lines can be extended while the intermediate representation can be seen on screen.
//
//    - There is no affine distortion correction shader - should be added although I don't see any visual problems with this so far.
//    - Could use a velocity based model of fatness of a ribbon or tube so that if you draw faster you get a fatter line for example.
//    - Still needs a line smoothing strategy such as a bicubic spline or catmull or a physical mass based interpolator.
//    - Could use some more custom shaders for glow effects and the like - especially for mobile.
//    - Doesn't do 2d lines - it should eventually - see https://gist.github.com/anselm/1474156
//
// more notes regarding 2d lines:
//	
// drawing a fat line in 2d requires handling sharp corners elegantly - we don't need this yet... it is arguably useful
// there are some examples of tube and ribbon renderers in unity asset store as an option but i want everything to be freely reusable
// https://www.shadertoy.com/view/4sjXzG <- could use a shader instead? arguably has utility also
// acko.net has some beautiful work also
// see http://www.codeproject.com/Articles/226569/Drawing-polylines-by-tessellation
// https://www.mapbox.com/blog/drawing-antialiased-lines/
// https://www.assetstore.unity3d.com/en/#!/content/20272
// paul o'rourke had a great example - not visible on the net anymore
//

public class Swatch3d: MonoBehaviour {

	public Material mainMaterial;
	public Material shadowMaterial;
		
	public enum STYLE {
		NONE,
		RIBBON,
		SWATCH,
		TUBE,
		//GLOW,
		//SPARKLE,
		//BLOCK,
		//PUTTY,
		//MAGNETIC
	};
	
	public STYLE style = STYLE.NONE;

	// A beefy base class collection of various kinds of painterly attributes shared by various kinds of drawing types
	GameObject main;
	GameObject bottom;
	GameObject shadow;
	Mesh mainMesh;
	Mesh bottomMesh;
	Mesh shadowMesh;
	
	// Configuration
	int cleanIndex = 0;
	int rotationStyle = 0;
	bool endCaps = false;
	bool IsFinished = false;
	bool EnableShadows = false;
	bool EnableBottom = false;
	bool IncrementalDouglasPeucker = false;
	const float sqrToleranceMin = 0.1f;
	const float sqrToleranceQuick = 3 * 3;
	
	// Total Drawing Count
	static int TotalCount = 0;

	// ----------------------------------------------------------------------------------------------------------------------------------------
	
	// Setup...
	public void setup(Color _color, STYLE _style = STYLE.NONE, Material _material = null, Material _shadowMaterial = null) {

		TotalCount++;

		cleanIndex = 0;
		
		style = _style;
		
		switch(style) {
		case STYLE.RIBBON:
			EnableBottom = true;
			EnableShadows = true;
			IncrementalDouglasPeucker = false;
			crossSegments = 2;
			endCaps = false;
			rotationStyle = 0;
			break;
		case STYLE.SWATCH:
			EnableBottom = false;
			EnableShadows = false;
			IncrementalDouglasPeucker = false;
			crossSegments = 2;
			endCaps = false;
			rotationStyle = 0;
			break;
		case STYLE.TUBE:
			EnableBottom = false;
			EnableShadows = false;
			IncrementalDouglasPeucker = false;
			crossSegments = 5;
			endCaps = true;
			rotationStyle = 0;
			gameObject.renderer.castShadows = false;
			//gameObject.renderer.receiveShadows = false;
			break;
		};

		// Bevel is pre-computed
		initTubeHelper();

		// Set materials supplied else use default else crash
		if(_material != null) mainMaterial = _material;
		if(_shadowMaterial !=null) shadowMaterial = _material;

		// Always clone the main material so we can modify it without affecting other swatches
		mainMaterial = Object.Instantiate(mainMaterial) as Material;
		mainMaterial.color = _color;

		// Basic mesh
		if(true) {
			main = gameObject;
			main.name = "Draw"+TotalCount;
			main.renderer.material = mainMaterial;
			mainMesh = gameObject.GetComponent<MeshFilter>().mesh;
		}

		// Ribbon style renders the bottom separately in order to guarantee mesh normals are independent (due to limits in Unity).
		// NOTE using geometry not shader for backfaces... arguable either way : http://danielbrauer.com/files/rendering-double-sided-geometry.html
		if(EnableBottom) {
			bottom = new GameObject( "Bottom" + TotalCount );
			bottom.transform.parent = gameObject.transform;
			bottom.AddComponent<MeshFilter>();
			bottom.AddComponent<MeshRenderer>();
			bottom.renderer.material = mainMaterial;
			bottomMesh = bottom.GetComponent<MeshFilter>().mesh;
		}

		// some geometries have shadows		
		// NOTE a separate geometry object is used for this rather than say tacking some shadow polys onto the end of the main obj geometry
		if(EnableShadows) {
			shadow = new GameObject( "Shadow" + TotalCount );
			shadow.transform.parent = gameObject.transform;
			shadow.AddComponent<MeshFilter>();
			shadow.AddComponent<MeshRenderer>();
			shadow.renderer.material = shadowMaterial;
			shadowMesh = shadow.GetComponent<MeshFilter>().mesh;
		}
		
	}

	public void test(Vector3 camerapos, Vector3 right, Vector3 forward,float width = 3.0f) {
		paintConsider(camerapos,Vector3.up * 100 ,right,forward,width);
		paintConsider(camerapos,Vector3.up * 100 + Vector3.forward * 100,right,forward,width);
		paintConsider(camerapos,Vector3.up *-100 + Vector3.forward * 100,right,forward,width);
		paintConsider(camerapos,Vector3.up * 0 ,right,forward,width);
		paintConsider(camerapos,Vector3.up * 0 + Vector3.left * 100,right,forward,width);
	}

	//--------------------------------------------------------------------------------------------------------------------------
	
	// accumulated user input describing users drawing action through 3d space
	public List<Vector3> points = new List<Vector3>();
	public List<Vector3> rights = new List<Vector3>();
	public List<Vector3> forwards = new List<Vector3>();
	public List<float> velocities = new List<float>();

	// produced collections of vertices in a form that lets me incrementally extend the set
	public List<Vector3> vertices = new List<Vector3>();
	public List<Vector2> uvs = new List<Vector2>();
	public List<int> triangles = new List<int>();

	// add a point (or replace the last point if it is insufficiently novel
	public void paintConsider(Vector3 camerapos, Vector3 xyz, Vector3 right, Vector3 forward,float width = 3.0f ) {

		if(IsFinished) {
			Debug.Log ("Illegal points; object was marked finished... while not critical it probably means there is a bug elsewhere.");
			return;
		}
		
		// given a point decide on how to store it
		int count = points.Count;
		int writestyle = 1;
		if(count < 2) {
			writestyle = 1; // = 0; <- disabled this because it is confusing to rewrite the second point later
		} else if(style == STYLE.SWATCH) {
			writestyle = 2;
			right = Vector3.Cross( forward, xyz - points[0] ).normalized * width; // hack
		} else if(IncrementalDouglasPeucker == false) {
			if( (xyz-points[count-1]).sqrMagnitude < sqrToleranceQuick) {
				// TODO this seems to be producing non consistent results - examine XXX
				writestyle = 3;
			}
		}

		// store point
		switch(writestyle) {
		case 0: // unused right now - always want to rewrite the second point and that is a hassle with this approach.
			// force two points into the system
			points.Add(xyz);
			rights.Add(Vector3.right);
			forwards.Add(forward);
			velocities.Add(width);
			goto case 1;
		case 1:
			// tack on a point
			points.Add(xyz);
			rights.Add(Vector3.right);
			forwards.Add(forward);
			velocities.Add(width);
			if(IncrementalDouglasPeucker == true && simplifyDouglasPeucker()) flushIntermediate();
			break;
		case 2:
			// force coplanarity + rewrite last point insitu; (this is a special mode for swatches which only have two points total ever)
			rights[count-2] = right;
			points[count-1] = xyz;
			rights[count-1] = right;
			forwards[count-1] = forward;
			velocities[count-1] = width;
			flushIntermediate();
			break;
		case 3:
			// rewrite last point insitu; do not consider flushing intermediate
			cleanIndex = count-1;
			points[count-1] = xyz;
			rights[count-1] = right;
			forwards[count-1] = forward;
			velocities[count-1] = width;
			flushIntermediate(); // TODO XXX it would be nice to be able to just flush the cap
			break;
		}

flushIntermediate(); // necessary for tubes for now argh.TODO remove

		paintToIntermediate();
	}

	// finalize - will tell caller if line is worth keeping
	public bool paintFinish() {
		IsFinished = true;
		if(style == STYLE.SWATCH) {
			if( points.Count < 2) return false;
			if( (points[1]-points[0]).sqrMagnitude < sqrToleranceMin) return false;
		} else {
			if(points.Count < 3) return false;
			if(IncrementalDouglasPeucker == false && simplifyDouglasPeucker()) flushIntermediate();
			paintToIntermediate();
			paintOptimizeMesh();
		}
		return true;
	}
	
	// helper
	void paintToIntermediate() {
		if(cleanIndex >= points.Count) return;
		for(;cleanIndex<points.Count;cleanIndex++) {
			if(style == STYLE.RIBBON || style == STYLE.SWATCH) {
				paintToRibbonIntermediateRepresentation(cleanIndex);
			} else {
				paintToTubeIntermediateRepresentation(cleanIndex);
			}
		}
		paintToUnity3DRepresentation();
	}

	// flush intermediate
	void flushIntermediate() {
		cleanIndex = 0;
		vertices = new List<Vector3>();
		triangles = new List<int>();
		endCapVerts = 0;
		endCapTris = 0;
	}

	// ----------------------------------------------------------------------------------------------------------------------------------------
	
	public void paintToRibbonIntermediateRepresentation(int p) {

		// slightly obsolete ribbon renderer - still being used
		// the benefit of this is that it treats tops and bottoms as separate geometry so that normals are not shared - producing better shadows
		// also it avoids having to adjust the hypotenuse of back faces - if i used a tube renderer with only 2 facets it would produce an X hypotenuse
		// and it has a simpler bevel approach; naturally suited to cursive
		
		Vector3 point = points[p];
		Vector3 right = rights[p];
		Vector3 forward = forwards[p];
		float width = velocities[p];

		// verts
		vertices.Add ( point - right * width );
		vertices.Add ( point + right * width );

		// tris
		if(p>0) {
			int j = vertices.Count - 1 - 3;
			triangles.Add(j+0); triangles.Add(j+1); triangles.Add (j+2);
			triangles.Add(j+1); triangles.Add(j+3); triangles.Add (j+2);
		}
	}

	// ----------------------------------------------------------------------------------------------------------------------------------------

	// Tube properties - but I will probably move everything over to tubes even if rendering ribbons TODO
	static Vector3[] crossPoints;
	static Quaternion rot = Quaternion.identity;
	int crossSegments = 2;
	int endCapVerts = 0;
	int endCapTris = 0;
	const float capsize = 5.0f;
	
	public void initTubeHelper() {
		crossPoints = new Vector3[crossSegments];
		float theta = 2.0f*Mathf.PI/crossSegments;
		for (int c=0;c<crossSegments;c++) {
			crossPoints[c] = new Vector3(Mathf.Cos(theta*c), Mathf.Sin(theta*c), 0);
		}
	}

	// Tube Renderer - this is highly modified from http://wiki.unity3d.com/index.php?title=TubeRenderer
	public void paintToTubeIntermediateRepresentation(int p) {

		if(p < 1) {
			return;
		}

		Vector3 prev = points[p-1];
		Vector3 point = points[p];
		Vector3 forward = forwards[p];
		Vector3 dir = (point-prev).normalized;
		float width = velocities[p];


		// decide what angle and rotation the bevel will have relative to the segments on either side of it.
		// it has a dramatic effect on the visual appearance

rotationStyle = 6;

		switch(rotationStyle) {
		case 0:
			// camera right == widest axis ... for ribbons this produces a cursive line effect where dragging horizontally is narrow and vertical is widest.
			break;
		case 1:
			// travel direction x camera == widest axis ... this produces a wide line that always faces camera - it is another useful ribbon style
			break;
		case 2:
			// travel direction x origin == widest axis ... this produces a line that hopefully minimizes torosional rotation - good for tubes
			break;
		case 3:
			// travel direction x lethargy == widest axis ... this is another strategy to minimize torosional rotation for tubes
			break;
		case 4:
			// a test: rotate the bevel around Y into stretching from (0,0,-1) to (0,0,1) completely ignoring everything about the line direction etc...
			rot = Quaternion.Euler ( new Vector3(0,90,0) );
			break;
		case 5:
			// a test: rotate the bevel around X into stretching from (0,-1,0) to (0,1,0) completely ignoring everything about the line direction etc...
			rot = Quaternion.Euler ( new Vector3(90,0,0) );
			break;
		case 6:
			// rotate bevel to be best fit between this and next line if any - trying to minimize torosional rotation over time
			if(points.Count - 1 > p) {

				Vector3 next = points[p+1];
				//float angle = Vector3.Angle (point-prev,point-next);
				//Vector3 cross = Vector3.Cross ((point-prev).normalized,(next-point).normalized);
				//Vector3 mid = Vector3.Lerp ((point-prev).normalized,(point-next).normalized,0.5f);
				//Debug.Log ("angle between point #"+p+" and point #"+(p+1)+" is="+angle);
				//Debug.Log ("cross is " + cross.normalized );
				//Debug.Log ("lerp is " + mid );
				//rot = Quaternion.FromToRotation((point-next).normalized,mid);

				// make a rotation that would rotate something to look in this way ignoring up				
				Quaternion rot1 = Quaternion.LookRotation((point-prev),Vector3.right);
				Quaternion rot2 = Quaternion.LookRotation((next-point),Vector3.right);
				rot = Quaternion.Lerp (rot1,rot2,0.5f);
				
			} else {
				rot = Quaternion.FromToRotation(Vector3.forward,point-prev);
			}
			break;
		case 7:
			// rotate the bevel to be perpendicular to this line - not particularily smart since doesn't consider inter segment angle
			rot = Quaternion.FromToRotation(Vector3.forward,point-prev);
			break;
		default:
			break;
		}
		
		// inject a start cap
		if(p == 1) {
			float start = endCaps == false ? capsize - 1 : 0;
			for(float i = start; i < capsize; i++) {
				float width2 = Mathf.Sin(Mathf.PI/2.0f*i/(capsize-1.0f)) * width;
				float height = Mathf.Cos(Mathf.PI/2.0f*i/(capsize-1.0f)) * width;
				for (int j=0;j<crossSegments;j++) {
					vertices.Add ( prev + rot * crossPoints[j] * width2 - dir * height );
					if(i==start)continue;
					int c = vertices.Count - 1;
					int d = c - j + ((j+1)%crossSegments);
					int a = c - crossSegments;
					int b = d - crossSegments;
					triangles.Add(c); triangles.Add(a); triangles.Add(b);
					triangles.Add(c); triangles.Add(b); triangles.Add(d);
				}
			}
		}

		// remove the end cap (not the super most elegant way to deal with this - would be slightly cleaner to leave the memory and just change index)
		//int capisat = ( p + (int)capsize - 1 ) * crossSegments;
		if(endCapVerts>0) {
			vertices.RemoveRange(endCapVerts,vertices.Count-endCapVerts);
			triangles.RemoveRange(endCapTris,triangles.Count-endCapTris);
		}

		// add main tube
		for (int j=0;j<crossSegments;j++) {
			vertices.Add ( point + rot * crossPoints[j] * width );
			int c = vertices.Count - 1;
			int d = c - j + ((j+1)%crossSegments);
			int a = c - crossSegments;
			int b = d - crossSegments;
			triangles.Add(c); triangles.Add(a); triangles.Add(b);
			triangles.Add(c); triangles.Add(b); triangles.Add(d);
		}

		// add (or read) the end cap
		if(endCaps) {
			endCapVerts = vertices.Count;
			endCapTris = triangles.Count;
			// here i know that there is at least one full width ring so i'm interested only in adding the parts that get smaller down to the tip.
			for(float i = 1; i < capsize; i++) {
				float width2 = Mathf.Cos(Mathf.PI/2*i/(capsize-1)) * width;
				float height = Mathf.Sin(Mathf.PI/2*i/(capsize-1)) * width;
				for (int j=0;j<crossSegments;j++) {
					vertices.Add ( point + rot * crossPoints[j] * width2 + dir * height );
					int c = vertices.Count - 1;
					int d = c - j + ((j+1)%crossSegments);
					int a = c - crossSegments;
					int b = d - crossSegments;
					triangles.Add(c); triangles.Add(a); triangles.Add(b);
					triangles.Add(c); triangles.Add(b); triangles.Add(d);
				}
			}
		}
	}

	// ----------------------------------------------------------------------------------------------------------------------------------------
	
	public void paintToUnity3DRepresentation() {

		// NOTE due to limits of C# and Unity we are forced to remake the mesh from scratch; cannot use static large arrays
		// NOTE it would be nice if Unity let us have a static vertex or uv pool for intermediate representation rather than reallocating... sigh.

		int c = vertices.Count;
		Vector3[] v = vertices.ToArray();
		Vector2[] uv = new Vector2[c];
		int[] tri = triangles.ToArray();

		for(int i = 0; i < c; i+= crossSegments ) {
			// generate smooth UV along entire length
			// TODO later it would be nice to have a non linear distortion for ribbons so that textured ends wouldn't stretch out so far
			for(int j = 0; j < crossSegments; j++ ) {
				float p1 = (float)j/(float)(crossSegments-1);
				float p2 = (float)i/(float)(c-1);
				uv[i+j] = new Vector2(p1,p2);
			}
		}

		if(true) {
			// Promote geometry to unity - for ribbons, swatches and tubes
			mainMesh.Clear();
			mainMesh.vertices = v;
			mainMesh.uv = uv;
			mainMesh.triangles = tri;
			mainMesh.RecalculateNormals();
			mainMesh.RecalculateBounds();
		}

		if(EnableBottom) {
			// Build bottom side (use this for ribbon only - largely because more fine grained control over normals is desired)
			for(int i = 0; i < tri.Length; i+=3) { int val = tri[i+1]; tri[i+1]=tri[i+2]; tri[i+2]=val; }		
			bottomMesh.Clear();
			bottomMesh.vertices = v;
			bottomMesh.uv = uv;
			bottomMesh.triangles = tri;
			bottomMesh.RecalculateNormals();
			bottomMesh.RecalculateBounds();
		}

		if(EnableShadows) {
			// Build 3d geometry shadow	
			// TODO this is inefficient for anything except ribbons (for ngons/tubes a ribbon like shadow could be made).
			for(int i=0; i < v.Length; i++) v[i].y = 0;
			shadowMesh.Clear();
			shadowMesh.vertices = v;
			shadowMesh.uv = uv;
			shadowMesh.triangles = tri;
			shadowMesh.RecalculateNormals();
			shadowMesh.RecalculateBounds();
		}	
	}
	
	void paintOptimizeMesh() {
		if(mainMesh!=null)mainMesh.Optimize ();
		if(EnableShadows && shadowMesh!=null)shadowMesh.Optimize();
	}
	
	//--------------------------------------------------------------------------------------------------------------------------
	// douglas peucker - utility
	// https://github.com/mourner/simplify-js/blob/3d/simplify.js
	//--------------------------------------------------------------------------------------------------------------------------
	
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
	
	public bool simplifyDouglasPeucker(float Tolerance = sqrToleranceMin) {

		if (points.Count < 4) return false;

		List<int> pointIndexsToKeep = new List<int>();
		
		int firstPoint = 0;
		int lastPoint = points.Count - 1;
		
		pointIndexsToKeep.Add(firstPoint);
		pointIndexsToKeep.Add(lastPoint);
		
		while (points[firstPoint].Equals(points[lastPoint])) {
			lastPoint--;
		}
		
		DouglasPeuckerReduction(firstPoint, lastPoint, Tolerance, ref pointIndexsToKeep);

		if(pointIndexsToKeep.Count == points.Count) return false;
		
		//Debug.Log ("Number of points coming into the system was " + points.Count );
		pointIndexsToKeep.Sort();		
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

		return true;		
	}
	
	private void DouglasPeuckerReduction(int firstPoint, int lastPoint, float tolerance, ref List<int> pointIndexsToKeep) {
		float maxDistance = 0;
		int indexFarthest = 0;
		
		// find the biggest bump
		for (int index = firstPoint; index < lastPoint; index++) {
			float distance = getSquareSegmentDistance(points[index], points[firstPoint], points[lastPoint]);
			//float distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
			if (distance > maxDistance) {
				maxDistance = distance;
				indexFarthest = index;
			}
		}

		// keep it	
		if (maxDistance > tolerance && indexFarthest != 0) {
			pointIndexsToKeep.Add(indexFarthest);
			DouglasPeuckerReduction(firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
			DouglasPeuckerReduction(indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
		}
	}
	
};



