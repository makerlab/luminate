#pragma strict
import Vectrosity;

var hillMaterial : Material;
var numberOfPoints = 100;
var numberOfHills = 4;
var ball : GameObject;
private var storedPosition : Vector3;
private var hills : VectorLine;
private var splinePoints : Vector2[];

#if !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2
function Start () {
	storedPosition = ball.transform.position;
	ball.AddComponent (Rigidbody2D);
	ball.AddComponent (CircleCollider2D);
	splinePoints = new Vector2[numberOfHills*2 + 1];
	
	hills = new VectorLine("Hills", new Vector2[numberOfPoints], hillMaterial, 12.0, LineType.Continuous, Joins.Weld);
	hills.useViewportCoords = true;
//	hills.collider = true;
	
	Random.seed = 96;
	CreateHills();
}

function OnGUI () {
	if (GUI.Button (Rect(10, 10, 150, 40), "Make new hills")) {
		CreateHills();
		ball.transform.position = storedPosition;
		ball.rigidbody2D.velocity = Vector2.zero;
		ball.rigidbody2D.WakeUp();
	}
}

function CreateHills () {
	splinePoints[0] = Vector2(-0.02, Random.Range (0.1, 0.6));
	var x = 0.0;
	var distance = 1.0 / (numberOfHills * 2);
	for (var i = 1; i < splinePoints.Length; i += 2) {
		x += distance;
		splinePoints[i  ] = Vector2(x, Random.Range (0.3, 0.7));
		x += distance;
		splinePoints[i+1] = Vector2(x, Random.Range (0.1, 0.6));
	}
	splinePoints[i-1] = Vector2(1.02, Random.Range (0.1, 0.6));
	
	hills.MakeSpline (splinePoints);
	hills.Draw();
}
#else
function OnGUI () {
	GUILayout.Label ("This demo requires Unity 4.3 or later");
}
#endif