using UnityEngine;
using System.Collections;
using Vectrosity;

public class GroundGridScript : MonoBehaviour {

	void Start () {
		Vector3[] grid = new Vector3[16];
		int i=0;
		float w=80;
		for(int j = -4; j < 4; j++) {
			grid[i++] = new Vector3(-w,0,j*10);
			grid[i++] = new Vector3( w,0,j*10);
			grid[i++] = new Vector3(j*10,0,-w);
			grid[i++] = new Vector3(j*10,0, w);
			
			VectorLine.SetRay3D(Color.green,grid[i-4],grid[i-4]*20);
		}

		// var myLine = new VectorLine("MyLine", linePoints, lineMaterial, 2.0, LineType.Continuous, Joins.Weld);

	}

	void Update () {
	
	}
}
