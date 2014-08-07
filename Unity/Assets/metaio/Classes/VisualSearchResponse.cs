using UnityEngine;
using System;

public struct VisualSearchResponse
{
	/// <summary>
	/// The name of the tracking configuration that is found
	/// </summary>
	private String trackingConfigurationName;
	
	/// <summary>
	/// The tracking configuration of the found target that can be directly loaded into metaioSDK
	/// </summary>
	private String trackingConfiguration;
	
	/// <summary>
	/// The name of the tracking configuration that is found
	/// </summary>
	public String TrackingConfigurationName
    {
        get { return trackingConfigurationName; }
        set { trackingConfigurationName = value; }
    }
	
	/// <summary>
	/// The tracking configuration of the found target that can be directly loaded into metaioSDK
	/// </summary>
	public String TrackingConfiguration
    {
        get { return trackingConfiguration; }
        set { trackingConfiguration = value; }
    }
}
