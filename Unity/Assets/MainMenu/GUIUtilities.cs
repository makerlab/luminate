using UnityEngine;
using System.Collections;


public class GUIUtilities : MonoBehaviour {
	
			//layout sizeing
	private float WidthFactor;
	private float HeightFactor;
	public static float SizeFactor;
	
	private static Matrix4x4 old;
	
	void Awake () {
	
		WidthFactor = (float)Screen.width / 960f;
		HeightFactor = (float)Screen.height / 600f;
		

		if (WidthFactor < HeightFactor)
			SizeFactor = WidthFactor;
		else
			SizeFactor = HeightFactor;
	}
	
	void Update () {
		if(Screen.orientation == ScreenOrientation.Landscape)
		{
			WidthFactor = (float)Screen.width / 960f;
			HeightFactor = (float)Screen.height / 600f;
			
	
			if (WidthFactor < HeightFactor)
				SizeFactor = WidthFactor;
			else
				SizeFactor = HeightFactor;
		}
		else
		{
			WidthFactor = (float)Screen.height / 960f;
			HeightFactor = (float)Screen.width / 600f;
			
	
			if (WidthFactor < HeightFactor)
				SizeFactor = WidthFactor;
			else
				SizeFactor = HeightFactor;
		}

	}
	
	// renders a text with or without shadow
	public static void Text(Rect rect, string text, GUIStyle textStyle)
	{
		old =  GUI.matrix;
		
		GUIUtility.ScaleAroundPivot(new Vector2(SizeFactor,SizeFactor), getScalingPivot(rect, textStyle.alignment));

		GUI.Label(rect, text, textStyle);
		
		GUI.matrix = old;
	}

	
	// renders a text with or without shadow
	public static bool ButtonWithText(Rect rect, string text, GUIStyle buttonStyle,  GUIStyle textStyle)
	{
		bool pressed = false;
		if(buttonStyle != null)
			pressed = GUI.Button(rect, "", buttonStyle);
		else
			pressed = GUI.Button(rect, "");
			
		old =  GUI.matrix;
		
		GUIUtility.ScaleAroundPivot(new Vector2(SizeFactor,SizeFactor), getScalingPivot(rect, textStyle.alignment));
		GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), text, textStyle);
		GUI.matrix = old;
		
		return pressed;
	}
	
	// returns the size a guicontent will have when rendered
	public static Vector2 getSize(GUIStyle style, GUIContent content)
	{
		return style.CalcSize(content) * SizeFactor;
	}
	
	private static Vector2 getScalingPivot(Rect rect, TextAnchor anchor)
	{
		switch (anchor)
		{
			case TextAnchor.LowerCenter:
				return new Vector2(rect.x + rect.width/2f, rect.y + rect.height);
			case TextAnchor.LowerLeft:
				return new Vector2(rect.x, rect.y + rect.height);
			case TextAnchor.LowerRight:
				return new Vector2(rect.x + rect.width, rect.y + rect.height);
			case TextAnchor.MiddleCenter:
				return new Vector2(rect.x + rect.width/2f, rect.y + rect.height/2f);
			case TextAnchor.MiddleLeft:
				return new Vector2(rect.x, rect.y + rect.height/2f);
			case TextAnchor.MiddleRight:
				return new Vector2(rect.x + rect.width, rect.y + rect.height/2f);
			case TextAnchor.UpperCenter:
				return new Vector2(rect.x + rect.width/2f, rect.y );
			case TextAnchor.UpperLeft:
				return new Vector2(rect.x, rect.y );
			case TextAnchor.UpperRight:
				return new Vector2(rect.x + rect.width, rect.y );
		}
		return Vector2.zero;
		
	}
}
