using UnityEngine;
using System;

namespace metaio
{
	/// <summary>
	/// Makes object rendered only in Unity editor.
	/// </summary>
	public class EditorOnly : MonoBehaviour
	{
		public void Awake()
		{
			MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
			Destroy(meshRenderer);
			MeshFilter mesh = GetComponent<MeshFilter>();
			Destroy(mesh);
			
			// Hide this and all children
#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
			gameObject.SetActiveRecursively(false);
#else
			gameObject.SetActive(false);
#endif
		}
	}
}

