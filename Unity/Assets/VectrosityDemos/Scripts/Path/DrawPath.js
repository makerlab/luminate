import Vectrosity;

var lineMaterial : Material;
var maxPoints = 500;
var continuousUpdate = true;
var ballPrefab : GameObject;
var force = 16.0;

private var pathLine : VectorLine;
private var pathIndex = 0;
private var pathPoints : Vector3[];
private var oldWidth : int;
private var ball : GameObject;

function Start () {
	oldWidth = Screen.width;
	pathPoints = new Vector3[maxPoints];
	pathLine = new VectorLine("Path", pathPoints, lineMaterial, 12.0, LineType.Continuous);
	pathLine.textureScale = 1.0;
	
	MakeBall();
	SamplePoints (ball.transform);
}

function MakeBall () {
	if (ball) {
		Destroy(ball);
	}
	ball = Instantiate(ballPrefab, Vector3(-2.25, -4.4, -1.9), Quaternion.Euler(300.0, 70.0, 310.0));
	ball.rigidbody.useGravity = true;
	ball.rigidbody.AddForce (ball.transform.forward * force, ForceMode.Impulse);
}

function SamplePoints (thisTransform : Transform) {
	var running = true;
	while (running) {
		pathPoints[pathIndex] = thisTransform.position;
		if (++pathIndex == maxPoints) {
			running = false;
		}
		yield WaitForSeconds(.05);
		
		if (continuousUpdate) {
			DrawPath();
		}
	}
}

function OnGUI () {
	if (GUI.Button(Rect(10, 10, 100, 30), "Reset")) {
		Reset();
	}
	if (!continuousUpdate && GUI.Button(Rect(10, 40, 100, 30), "Draw Path")) {
		DrawPath();
	}
}

function Reset () {
	StopAllCoroutines();
	MakeBall();
	pathLine.ZeroPoints();
	pathLine.maxDrawIndex = maxPoints;
	pathLine.Draw();	// Draw the cleared line in order to remove all drawn segments that might exist, since we've been messing with maxDrawIndex
	pathIndex = 0;
	SamplePoints (ball.transform);
}

function Update () {
	if (oldWidth != Screen.width) {
		oldWidth = Screen.width;
		VectorLine.SetCamera();
	}
}

function DrawPath () {
	if (pathIndex < 2) return;
	pathLine.maxDrawIndex = pathIndex-1;
	pathLine.Draw();
}