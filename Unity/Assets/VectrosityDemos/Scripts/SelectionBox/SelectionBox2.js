import Vectrosity;

var lineMaterial : Material;
var textureScale = 4.0;
private var selectionLine : VectorLine;
private var originalPos : Vector2;
private var oldWidth : int;

function Start () {
	selectionLine = new VectorLine("Selection", new Vector2[5], lineMaterial, 4.0, LineType.Continuous);
	selectionLine.textureScale = textureScale;
	oldWidth = Screen.width;
}

function OnGUI () {
	GUI.Label(Rect(10, 10, 300, 25), "Click & drag to make a selection box");
}

function Update () {
	if (Input.GetMouseButtonDown(0)) {
		originalPos = Input.mousePosition;
	}
	if (Input.GetMouseButton(0)) {
		selectionLine.MakeRect (originalPos, Input.mousePosition);
		selectionLine.Draw();
	}
	selectionLine.textureOffset = -Time.time*2.0 % 1;
		
	if (oldWidth != Screen.width) {
		oldWidth = Screen.width;
		VectorLine.SetCamera();
	}
}