using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

public class AssetsManager
{
	// This table holds copied resources that will be used by metaioSDK
	private static Hashtable mAssets;
	
	/// <summary>
	/// Extracts the streaming assets.
	/// </summary>
	/// <returns>
	/// <c>true</c> on success
	/// </returns>
	/// <param name='overrideAssets'>
	/// If set to <c>true</c> override assets.
	/// </param>
	public static bool extractAssets(bool overrideAssets)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		// Use Java AssetsManager class on Android
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
		AndroidJavaClass cls = new AndroidJavaClass("com.metaio.tools.io.AssetsManager");
		String parentPath = "";
		String[] ignoreList = {"bin", "libs", "webkit", "sounds", "images"};
		object[] args = {jo, parentPath, ignoreList, overrideAssets};
		cls.CallStatic("extractAllAssets", args);
		
#elif UNITY_IPHONE || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
		
		if (mAssets == null)
		{
			mAssets = new Hashtable();
		}
		
		DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);
		FileInfo[] assets = dir.GetFiles("*", SearchOption.AllDirectories);
		foreach (FileInfo asset in assets)
		{			
			String filename = asset.FullName.Remove(0, Application.streamingAssetsPath.Length+1);
			if (!mAssets.ContainsKey(filename) && !filename.EndsWith(".meta"))
			{
				Debug.Log("AssetsManager: copying asset: " + filename);
				Debug.Log("AssetsManager: asset copied: " + asset.FullName);
				mAssets.Add(filename, asset.FullName);
			}
		}
#endif
			
		return true;
	}

	/// <summary>
	/// Get fullpath to the streaming asset
	/// </summary>
	/// <returns>
	/// Full path if the asset was extracted, else <c>null</c>
	/// </returns>
	/// <param name='filename'>
	/// Streaming asset file name
	/// </param>
	public static String getAssetPath(String filename)
	{
		Debug.Log("AssetsManager.getAssetPath: "+filename);
		
		filename = filename.Replace('/', Path.DirectorySeparatorChar);
		String assetPath = null;
		
#if UNITY_ANDROID && !UNITY_EDITOR
		// Use Java AssetsManager class on Android
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
		AndroidJavaObject context = jo.Call<AndroidJavaObject>("getApplicationContext");
		AndroidJavaClass cls = new AndroidJavaClass("com.metaio.tools.io.AssetsManager");
		object[] args = {context, filename};
		assetPath = cls.CallStatic<String>("getAssetPath", args);

#elif UNITY_IPHONE || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
		
		if (mAssets == null)
		{
			Debug.LogError("AssetsManager.getAssetPath: streaming assets are not extracted");
			return null;
		}
		if (mAssets.ContainsKey(filename))
		{
			assetPath = (String)mAssets[filename];
			Debug.Log("AssetsManager: fullpath: "+assetPath);
		}
		
#endif
		return assetPath;
	}
		
		
	/// <summary>
	/// Set fullpath to a streaming asset
	/// </summary>
	/// <param name='filename'>
	/// Streaming asset's filename
	/// </param>
	/// <param name='assetpath'>
	/// Streaming asset's full path
	/// </param>
	public static void setAssetPath(String filename, String assetpath)
	{
		if (mAssets == null)
		{
			mAssets = new Hashtable();
		}
		if(mAssets.Contains(filename))
			mAssets[filename] = assetpath;
		else
			mAssets.Add(filename, assetpath);
	}	
		
}
