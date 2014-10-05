using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using metaio;

class MyLine {
	
	public enum STYLE {
		NONE,
		SHADOW,
		CURSIVE,
		BOX,
		PRISM,
		GLOW,
		SPARKLE,
		BLOCK,
		PUTTY,
		MAGNETIC
	};
	STYLE style = STYLE.NONE;
	const int MAXLEN = 255;
	Color color = Color.white;
	Mesh mesh;
	Mesh shadowmesh;
	public GameObject obj;
	public GameObject shadow;
	
	struct Feature {
		Vector3 point;
		Vector3 up;
		Vector3 right;
		Vector3 forward;
		float	size;
	};
	
	List<Vector3> points = new List<Vector3>();
	List<Vector3> ups = new List<Vector3>();
	List<Vector3> rights = new List<Vector3>();
	List<Vector3> forwards = new List<Vector3>();
	List<float> velocities = new List<float>();
	
	List<Vector3> vertices = new List<Vector3>();
	List<Vector2> uvs = new List<Vector2>();
	List<int> triangles = new List<int>();
	List<Color> colors = new List<Color>();
	
	static int lines = 0;
	
	public MyLine(Color _color,STYLE style = STYLE.BOX, Material material = null, Material shadowmaterial = null) {
		
		// setup material stuff
		//material = new Material(Shader.Find("Transparent/Diffuse"));
		material.color = color = _color;
		
		// make a new gameobj/mesh for the line		
		obj = new GameObject( "line " + lines ); 
		obj.AddComponent<MeshFilter>();
		obj.AddComponent<MeshRenderer>();
		if(material != null) obj.renderer.material = material;
		mesh = obj.GetComponent<MeshFilter>().mesh;
		
		// make a shadow
		shadow = new GameObject( "shadow " + lines );
		shadow.transform.parent = obj.transform;
		shadow.AddComponent<MeshFilter>();
		shadow.AddComponent<MeshRenderer>();
		if(shadowmaterial!=null) shadow.renderer.material = shadowmaterial;
		shadowmesh = shadow.GetComponent<MeshFilter>().mesh;
		
		// obj.renderer.material.SetColor("_Emission", color);
		// obj.renderer.currentColor = mat.color;
		
		lines++;
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
		
		Debug.Log ("Number of points coming into the system was " + points.Count );
		
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

		Debug.Log ("Number of points after system was " + points.Count );
		
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
			//triangles.Add(j+0); triangles.Add(j+2); triangles.Add (j+1);	// bottom - not needed for one sided
			//triangles.Add(j+1); triangles.Add(j+3); triangles.Add (j+2);
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
			
			//			triangles.Add(j+3); triangles.Add(j+5); triangles.Add (j+4);
			
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
		
		if(points.Count < 5) return false;
		
		// and recopy
		rebuildMesh();

		// enough data to be useful
		return true;
	}
	
	
	void rebuildMesh() {
		mesh.Clear();
		mesh.vertices = vertices.ToArray ();
		mesh.uv = uvs.ToArray ();
		mesh.triangles =  triangles.ToArray();
		mesh.colors = colors.ToArray();
		mesh.RecalculateNormals();
		
		// shadow
		// TODO the shadow mesh doesn't really need a bottom...
		//	shadowmesh.Clear();
		//	for(int i=0; i < copy_of_vertices.Length; i++) copy_of_vertices[i].y = 0;
		//	shadowmesh.vertices = copy_of_vertices;
		//	shadowmesh.uv = copy_of_uvs;
		//	shadowmesh.triangles = copy_of_triangles;
		//	shadowmesh.RecalculateNormals();		
	}
	
	
};

public class chaser : MonoBehaviour {
	
	public Color color = Color.red;
	public Material sourceMaterial;
	public Material sourceMaterial2;
	public Transform source;
	public Transform target;
	public Transform target2;
	public float speed = 90.0f;
	MyLine.STYLE style = MyLine.STYLE.CURSIVE;
	
	List<MyLine> lines = new List<MyLine>();
	MyLine line;
	
	int timeout = 0;
	float x,y;
	
	void Start () {
		
		// playing with shader
		//MyLine line = new MyLine(color,style,sourceMaterial,sourceMaterial2);
		//lines.Add(line);
		//line.add(transform,Color.white, 1);
		//line.add(target.transform,Color.white, 1);
		//line.add(target2.transform,Color.red, 10);
		
		// taking a look at the ordinary line renderer
		//LineRenderer lr = gameObject.AddComponent ("LineRenderer") as LineRenderer;
		//lr.SetColors (new Color(100,0,0,100), new Color(0,0,100,100));
		//lr.SetVertexCount(3);
		//lr.SetPosition(0,new Vector3(4,0,0));
		//lr.SetPosition(1,new Vector3(4,0,2));
		//lr.SetPosition(2,new Vector3(8,0,4));
		//lr.SetWidth(1,1);
		//lr.useWorldSpace = false;
		//lr.material = new Material(Shader.Find("Particles/Additive"));
	}
	
	void Update () {
		
		// physics based smoothing - move towards target
		transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * 10.0f );
		transform.rotation = Quaternion.Slerp (transform.rotation, target.rotation, Time.deltaTime * 10.0f );
		
		// turn off buttons eventually		
		timeout=timeout+1;
		
		// on new touch event start a trail
		RuntimePlatform platform = Application.platform;
		if(platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer){
			if(Input.touchCount > 0) {
				if(Input.GetTouch(0).phase == TouchPhase.Began){
					timeout = 0;
					//if(chaser3 != null) 			chaser3.gameObject.SetActive (true);
					x = Input.GetTouch(0).position.x;
					y = Input.GetTouch (0).position.y;
					if(x > 100 && y > 100 ) {
						Material m = Instantiate(sourceMaterial) as Material;
						line = new MyLine(color,style,m,sourceMaterial2);
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
				Debug.Log ("Line finished");
			}
			if(Input.GetMouseButtonDown(0)) {
				timeout = 0;
				//if(chaser3 != null) 			chaser3.gameObject.SetActive (true);
				Debug.Log ( Input.mousePosition.x + " " + Input.mousePosition.y );
				if(Input.mousePosition.x > 100 && Input.mousePosition.y > 100) {
					Material m = Instantiate(sourceMaterial) as Material;
					line = new MyLine(color,style,m,sourceMaterial2);
					lines.Add(line);
					Debug.Log ("Line started");
				}
			}
		}
		
		// pass the current position for consideration
		if(line!=null) {
			line.consider(this.transform,Color.white,3);
		}
		
		// a bad idea not used now
		//		if(chaser3 != null && timeout > 3 * 60 ) {
		//			chaser3.gameObject.SetActive (false);
		//		}
		
	}
	
	
	void SetColor(HSBColor color) {
		this.color = color.ToColor();
		Debug.Log ("Got color " );
		Debug.Log ( color );
	}
	
	public Texture btnTexture;
	
	void OnGUI() {

		if ( GUI.Button (new Rect (10,10,100,60), "Track" )) {
			MetaioSDKUnity.startInstantTracking("INSTANT_3D","");
			GUI.enabled = false; // what does this mean? XXX TODO
		}
		
		if (GUI.Button(new Rect(120,10,80,60), "undo")) {
			if(lines.Count>0) {
				MyLine line = lines[lines.Count-1];
				lines.RemoveAt(lines.Count-1);
				line.destroy();
			}
		}

		if (GUI.Button(new Rect(220,10, 80, 40), "calligraphy")) {
			style = MyLine.STYLE.CURSIVE;
		}
	}	
}
