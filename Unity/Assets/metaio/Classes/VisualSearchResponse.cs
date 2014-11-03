using UnityEngine;
using System;

public struct VisualSearchResponse
{
	/// <summary>
	/// The name of the tracking configuration that is found
	/// </summary>
	public string trackingConfigurationName;

	/// <summary>
	/// The tracking configuration of the found target that can be directly loaded into metaioSDK
	/// </summary>
	public string trackingConfiguration;

	/// <summary>
	/// The visual search score
	/// </summary>
	public float visualSearchScore;

	/// <summary>
	/// Meta data (in JSON format)
	/// </summary>
	public string metadata;
	
	public static VisualSearchResponse FromPB(metaio.unitycommunication.VisualSearchResponse resp)
	{
		VisualSearchResponse ret = new VisualSearchResponse();
		ret.trackingConfigurationName = resp.TrackingConfigurationName;
		ret.trackingConfiguration = resp.TrackingConfiguration;
		ret.visualSearchScore = resp.VisualSearchScore;
		ret.metadata = resp.Metadata;
		return ret;
	}
}