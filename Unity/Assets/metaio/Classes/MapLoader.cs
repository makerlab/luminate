using UnityEngine;
using System.Collections;
using System;
using metaio;

/// <summary>
/// Creates visual representation for 3D map in Unity
/// </summary>
public class MapLoader 
{
    // the root object for visual map representation
    private GameObject map;
    
    private float[] featuresRaw = new float[3 * 100000];

    private const int FEATURES_PACKAGES_AMOUNT = 20;
    
    private int totalFeatures;
    private int loadedFeatures;
    
    private Vector3 featureScale = Vector3.one * 5;

    private Vector3 mapScale = new Vector3(1, -1, 1);
    private Vector3 mapRotation = new Vector3(270, 90, 0);

    /// <summary>
    /// Loads 3D coordinates of features contained in the map
    /// </summary>
    public void loadMap(String mapPath) 
    {
        clearMap();
        loadedFeatures = 0;
        totalFeatures = MetaioSDKUnity.get3DPointsFrom3Dmap(Application.dataPath + "\\StreamingAssets\\" + mapPath, featuresRaw);
        Debug.Log(String.Format("Loaded map has {0} features", totalFeatures));
        
        map = new GameObject("Feature Map") as GameObject;
        
        // we transform the map due to differences between Unity COS and metaio COS  
        map.transform.eulerAngles = mapRotation;
        map.transform.localScale = mapScale;

        Debug.Log("Start loading map... Don't delete Feature Map object while loading!");
    }

    /// <summary>
    /// Creates visual representation for the part of the map. Number of features created per call defined by FEATURES_PACKAGES_AMOUNT.
    /// </summary>
    /// <returns>
    /// True if the whole map was loaded and false otherwise
    /// </returns>
    public bool createFeatures()
    {
        int i = 0;
        for (i = loadedFeatures; i < loadedFeatures + FEATURES_PACKAGES_AMOUNT && i < totalFeatures; i++)
        {
            GameObject feature = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            feature.transform.parent = map.transform;
            feature.transform.localScale = featureScale;
            feature.transform.localPosition = new Vector3(featuresRaw[i * 3], featuresRaw[i * 3 + 1], featuresRaw[i * 3 + 2]);
        }
        loadedFeatures += FEATURES_PACKAGES_AMOUNT;
        Debug.Log("Map is loading. Progress: " + (float) i / totalFeatures * 100 + "%");
        if (loadedFeatures >= totalFeatures) return true;
        return false;
    }

    public void setMapObject(GameObject map)
    {
        this.map = map; 
    }

    /// <summary>
    /// Clears the map (if any existed)
    /// </summary>
    public bool clearMap()
    {
        if (map == null) return false;

        GameObject.DestroyImmediate(map);

        return true;
    }
}
