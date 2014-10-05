using UnityEngine;

public class Draggable : MonoBehaviour
{
	public bool fixX;
	public bool fixY;
	public Transform thumb;	
	bool dragging;

	void Update()
	{
		if (Input.GetMouseButtonDown(0)) {
			dragging = false;
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, 100)) {
				dragging = true;
			}
		}
		if (Input.GetMouseButtonUp(0)) dragging = false;
		if (dragging && Input.GetMouseButton(0)) {
			var point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			point = collider.ClosestPointOnBounds(point);
			SetThumbPosition(point);
			SendMessage("OnDrag", Vector3.one - (thumb.position - collider.bounds.min) / collider.bounds.size.x);
			Debug.Log ("send drag");
		}
	}

	void SetDragPoint(Vector3 point)
	{
		point = (Vector3.one - point) * collider.bounds.size.x + collider.bounds.min;
		SetThumbPosition(point);
	}

	void SetThumbPosition(Vector3 point)
	{
		thumb.position = new Vector3(fixX ? thumb.position.x : point.x, fixY ? thumb.position.y : point.y, thumb.position.z);
	}
}
