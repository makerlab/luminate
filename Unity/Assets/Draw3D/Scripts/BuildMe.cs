using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BuildMe {
	public static void BuildiOS() {
#if UNITY_EDITOR
		BuildOptions opt = BuildOptions.None; // SymlinkLibraries| BuildOptions.Development|BuildOptions.ConnectWithProfiler|BuildOptions.AllowDebugging;
		string[] levels = { "Assets/AnselmSept2014.unity" };
		BuildPipeline.BuildPlayer(levels,"iOS",BuildTarget.iPhone,opt);
#endif
	}
}