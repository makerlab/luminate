using UnityEngine;

public class ExampleColorReceiver : MonoBehaviour {
	
    Color color;
	public GameObject target;
	
	void OnColorChange(HSBColor color) 
	{
        this.color = color.ToColor();
        Debug.Log ("Got color " );
        Debug.Log ( color );
		target.SendMessage("SetColor",color);
	}

    void OnGUI() {
//		var r = Camera.mainCamera.pixelRect;
//		var rect = new Rect(r.center.x + r.height / 6 + 50, r.center.y, 100, 100);
//		GUI.Label (rect, "#" + ToHex(color.r) + ToHex(color.g) + ToHex(color.b));	
    }

	string ToHex(float n)
	{
		return ((int)(n * 255)).ToString("X").PadLeft(2, '0');
	}
}
