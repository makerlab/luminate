using UnityEngine;

public class ColorHuePicker : MonoBehaviour
{
	void SetColor(HSBColor color)
	{
		SendMessage("SetDragPoint", new Vector3(color.h, 0, 0));
	}	


	void OnDrag(Vector3 point)
	{
		transform.parent.BroadcastMessage("SetHue", point.x);
	}
	

	private Vector3 lastMousePosition;
	
	void OnMouseDown() {
		lastMousePosition = Input.mousePosition;
	}
	
	void OnMouseDrag() {
		Vector3 distance = Input.mousePosition - lastMousePosition;
		Debug.Log("The mouse moved " + distance.magnitude + " pixels");
		//target.BroadcastMessage("SetHue", distance.x);
    }
}
