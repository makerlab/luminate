using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(metaioMovieTexture))]
public class metaioMovieTextureEditor : Editor
{
	private metaioMovieTexture movieTextureScript;
 	
    public void OnEnable()
    {
        movieTextureScript = (metaioMovieTexture)target;
    }
	
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		
		bool mustRepaint = false;
		
		
		EditorGUIUtility.LookLikeControls(200);
		
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Movie texture source:");
        movieTextureScript.movieAssetIndex = EditorGUILayout.Popup("", movieTextureScript.movieAssetIndex, metaioMovieTexture.movieAssets, EditorStyles.popup);
		
		if (movieTextureScript.movieAssetIndex == 0)
		{
		
			EditorGUILayout.HelpBox(
				"Drag an drop a movie file from StreamingAssets folder. " +
					"If the video contains transparency, please ensure the file has '.alpha.' in its name.",
				MessageType.Info);
			
			EditorGUILayout.BeginHorizontal();
			
			movieTextureScript.movieAsset = EditorGUILayout.ObjectField( movieTextureScript.movieAsset, typeof(UnityEngine.Object), true);	
			
			movieTextureScript.movieFile = AssetDatabase.GetAssetPath(movieTextureScript.movieAsset);
			movieTextureScript.movieFile = movieTextureScript.movieFile.Replace("Assets/StreamingAssets/", "");

		}
		else
		{
			EditorGUILayout.HelpBox(
				"Enter absolute path of a movie file." +
					"If the video contains transparency, please ensure the file has '.alpha.' in its name.",
				MessageType.Info);
			
			EditorGUILayout.BeginHorizontal();
			
			movieTextureScript.movieFile = EditorGUILayout.TextField("", movieTextureScript.movieFile);
		}
		
		// TODO: Nice file selection dialog (http://forum.unity3d.com/threads/172846-Repaint-not-working-as-expected?p=1182475)
		/*if (GUILayout.Button("Select movie file"))
		{
			string s = Path.GetFileName(EditorUtility.OpenFilePanel("Movie", "Assets/StreamingAssets", "3g2"));

			mustRepaint = true;
		}*/
		
		EditorGUILayout.EndHorizontal();
		
		
		EditorGUILayout.HelpBox(
			"Do you want to auto-resize this object to fit the video dimensions? Object width " +
			"(local X axis) is kept the same, while the height (local Z axis) is adjusted.",
			MessageType.Info);
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Auto-resize object");
		movieTextureScript.autoResizeGameObjectToVideoDimensions = EditorGUILayout.Toggle(
			movieTextureScript.autoResizeGameObjectToVideoDimensions);
		EditorGUILayout.EndHorizontal();
		
		
		EditorGUILayout.HelpBox(
			"If this is a video including transparency, created using our tutorial " +
				"(http://dev.metaio.com/content-creation/movie/transparent-movie-textures/), it should be rotated " +
				"counterclockwise. Please specify this property here:",
			MessageType.Info);
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Video is rotated counterclockwise");
		movieTextureScript.isRotatedCCW = EditorGUILayout.Toggle(movieTextureScript.isRotatedCCW);
		EditorGUILayout.EndHorizontal();
		
		
		if (mustRepaint)
			Repaint();
	}
}

