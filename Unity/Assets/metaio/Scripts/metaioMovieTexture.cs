using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using metaio;

public class metaioMovieTexture : MonoBehaviour
{
	[HideInInspector]
	[SerializeField]
	public bool autoResizeGameObjectToVideoDimensions = true;

	[HideInInspector]
	[SerializeField]
	public bool isRotatedCCW = false;

	[HideInInspector]
	[SerializeField]
	public string movieFile;
	
#region Editor script fields
	
	public static String[] movieAssets = {"StreamingAssets...", "Absolute Path..."};
	
	[HideInInspector]
	[SerializeField]
	public int movieAssetIndex;
	
	[HideInInspector]
	[SerializeField]
	public UnityEngine.Object movieAsset = null;
	
#endregion
	
	// Pointer to the movie geometry
	private IntPtr movieGeometry;
	
	private static Dictionary<IntPtr, string> movieGeometryToGameObjectNameCache = new Dictionary<IntPtr, string>();
	
	private int textureID;
	
	/// <summary>
	/// Flag to create movie only once in LateUpdate
	/// </summary> 
	private bool createMovie;
	
	/// <summary>
	/// To save loop parameter
	/// </summary>
	private bool isLooping;
	
	/// <summary>
	/// To save playing state, so that it can be restored when scene is activated
	/// </summary>
	private bool isPlaying;
	
	private void autoResizeGameObject()
	{
		var mesh = gameObject.GetComponent<MeshFilter>().mesh;

		if (mesh == null)
			Debug.LogError("Cannot adjust game object dimensions to movie, no mesh attached (try a plane)");
		else
		{
			float objectWidth = mesh.bounds.size.x * gameObject.transform.localScale.x;
			float objectHeight = mesh.bounds.size.z * gameObject.transform.localScale.z;
			float movieAspect = MetaioSDKUnity.getMovieTextureDisplayAspect(movieGeometry); // NOT movieWidth/movieHeight, this is the DAR

			if (objectWidth <= 0.01 || objectHeight <= 0.01)
			{
				Debug.LogWarning("Not adjusting game object dimensions to movie, X or Z bound/scale is zero, is this a plane mesh?");
			}
			else if (Math.Abs(objectWidth/objectHeight - movieAspect) > 0.03)
			{
				Debug.Log(string.Format("Adjusting game object to movie texture aspect ratio {0}", movieAspect));

				if (isRotatedCCW)
				{
					// Implementation note: If isRotatedCCW=true, then movieWidth/movieHeight are actually swapped, i.e.
					// represent the inverse display size.

					// In that case, we'll rotate the mesh (see below), so X scale is for height, Z scale for width
					gameObject.transform.localScale = new Vector3(
						gameObject.transform.localScale.x * movieAspect,
						gameObject.transform.localScale.y,
						gameObject.transform.localScale.z * (objectWidth/objectHeight));
				}
				else
					gameObject.transform.localScale = new Vector3(
						gameObject.transform.localScale.x,
						gameObject.transform.localScale.y,
						gameObject.transform.localScale.z * (objectWidth/objectHeight) / movieAspect);
			}
			else
				Debug.Log("Not adjusting game object dimensions, already has same aspect ratio as movie");
		}
	}

	private void createTexture()
	{
		uint movieWidth = MetaioSDKUnity.getMovieTextureWidth(movieGeometry);
		uint movieHeight = MetaioSDKUnity.getMovieTextureHeight(movieGeometry);

		// Shouldn't happen, dimensions are initialized by now
		if (movieWidth == 0 || movieHeight == 0)
			throw new Exception("Movie not loaded yet, width/height is zero");

		int textureSizeX = nextPowerOf2(movieWidth);
		int textureSizeY = nextPowerOf2(movieHeight);

		Texture2D texture = new Texture2D(textureSizeX, textureSizeY, TextureFormat.RGBA32, false);

		// User has to set transparency-supporting shader himself since he might as well overwrite the shader with
		// a custom one, so we do not force anyone to use the Unity built-in shader
		Material mat = gameObject.renderer.material;
		texture.wrapMode = TextureWrapMode.Clamp;
		mat.mainTexture = texture;

		// Most videos are not a power of 2 in dimensions, so scale the UV coordinates appropriately.
		// Unity seems to interpret the (0,0) coordinate of OpenGL textures as bottom left, so we
		// need to vertically flip the V coordinate here.
		mat.mainTextureScale = new Vector2((float)movieWidth / textureSizeX, - (float)movieHeight / textureSizeY);
		mat.mainTextureOffset = new Vector2(0, (float)movieHeight / textureSizeY);

		textureID = texture.GetNativeTextureID();

		Debug.Log(string.Format("Texture ID for movie {0} ({1}x{2}): {3}", movieFile, movieWidth, movieHeight, textureID));
	}
	
	internal static string getGameObjectNameForMovieTextureGeometryPtr(IntPtr movieTextureGeometry)
	{
		string gameObjectName = null;

		if (!movieGeometryToGameObjectNameCache.TryGetValue(movieTextureGeometry, out gameObjectName))
		{
			Debug.LogError("Could not find matching game object for movie texture, game object already removed?");
			return null;
		}

		return gameObjectName;
	}

	private int nextPowerOf2(uint n)
	{
		if (n > 4096)
			throw new ArgumentException("Value too large");

		int ret = 1;

		while (ret < n)
			ret *= 2;

		return ret;
	}

	void OnDestroy()
	{
		if (movieGeometry != IntPtr.Zero)
		{
			MetaioSDKUnity.deleteMovieGeometry(movieGeometry);
			movieGeometryToGameObjectNameCache.Remove(movieGeometry);
		}
	}
	
	void OnBecameInvisible()
	{
		// pause plackback when GameObject becomes invisible
		if (isPlaying && movieGeometry != IntPtr.Zero)
		{
			MetaioSDKUnity.pauseMovieTexture(movieGeometry);
		}
	}
	
	void OnBecameVisible()
	{
		// Resume playback if it was playing
		if (isPlaying)
		{
			play (isLooping);
		}
	}
	
	[MethodImpl(MethodImplOptions.Synchronized)]
	public void pause()
	{
		if (movieGeometry != IntPtr.Zero)
		{
			MetaioSDKUnity.pauseMovieTexture(movieGeometry);
		}
		isPlaying = false;
	}
	
	[MethodImpl(MethodImplOptions.Synchronized)]
	public void play(bool loop)
	{
		if (movieGeometry != IntPtr.Zero)
		{
			MetaioSDKUnity.startMovieTexture(movieGeometry, loop ? 1 : 0);
		}
		
		isLooping = loop;
		isPlaying = true;
	}
	
	[MethodImpl(MethodImplOptions.Synchronized)]
	public void stop()
	{
		if (movieGeometry == IntPtr.Zero)
		{
			MetaioSDKUnity.stopMovieTexture(movieGeometry);
		}
		isPlaying = false;
	}
	
	void Awake()
	{
		movieGeometry = IntPtr.Zero;
		createMovie = true;
		isLooping = false;
		isPlaying = false;
	}
	
	void LateUpdate()
	{
		// Only create movie once
		if (createMovie == false)
		{
			return;
		}
		createMovie = false;

		if (movieFile.Length == 0)
		{
			Debug.LogError("No movie texture file specified");
			return;
		}

		String fullPath = AssetsManager.getAssetPath(movieFile);
		if (fullPath == null)
		{
			Debug.Log("Movie texture file not found in streaming assets, using absolute path: "+movieFile);
			fullPath = movieFile;
		}
		
		movieGeometry = MetaioSDKUnity.createGeometryFromMovie(fullPath);

		if (movieGeometry == IntPtr.Zero)
			throw new Exception(string.Format("Failed to load movie {0}", fullPath));

		movieGeometryToGameObjectNameCache[movieGeometry] = this.name;

		createTexture();

		MetaioSDKUnity.setMovieTextureTargetTextureID(movieGeometry, textureID);

		Debug.Log("Loaded movie " + movieFile);

		if (autoResizeGameObjectToVideoDimensions)
			autoResizeGameObject();
		
		// If we know the video frame is rotated, we automatically rotate it to the correct orientation here
		if (isRotatedCCW)
		{
			// Y axis (green) faces up from the plane, so do left-handed 90° rotation (i.e. clockwise by 90°)
			gameObject.transform.Rotate(new Vector3(0, 90, 0));
		}
		
		// if the associated GameObject is visible, play movie if it should
		// be played
		if (gameObject.renderer.isVisible && isPlaying)
			play(isLooping);
	}

}
