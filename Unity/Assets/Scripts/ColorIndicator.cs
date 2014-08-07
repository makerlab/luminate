using UnityEngine;

public class ColorIndicator : MonoBehaviour {

	HSBColor color;

	void Start() {
		color = HSBColor.FromColor(renderer.sharedMaterial.GetColor("_Color"));
		transform.parent.BroadcastMessage("SetColor", color);
	}

	void ApplyColor ()
	{
		renderer.sharedMaterial.SetColor ("_Color", color.ToColor());
		transform.parent.BroadcastMessage("OnColorChange", color, SendMessageOptions.DontRequireReceiver);
	}

	void SetHue(float hue)
	{
		color.h = hue;
		ApplyColor();
    }	

	void SetSaturationBrightness(Vector2 sb) {
		color.s = sb.x;
		color.b = sb.y;
		ApplyColor();
	}
}
