namespace ET
{
	public static class Define
	{
		public const string BuildOutputDir = "./Temp/Bin/Debug";

#if UNITY_EDITOR && !ASYNC
		[StaticField]
		public static bool IsAsync = false;
#else
		[StaticField]
        public static bool IsAsync = true;
#endif

#if UNITY_EDITOR
		[StaticField]
		public static bool IsEditor = true;
#else
		[StaticField]
        public static bool IsEditor = false;
#endif

#if ENABLE_CODES
		[StaticField]
		public static bool EnableCodes = true;
#else
		[StaticField]
        public static bool EnableCodes = false;
#endif

#if ENABLE_VIEW
		[StaticField]
		public static bool EnableView = true;
#else
		[StaticField]
		public static bool EnableView = false;
#endif

#if ENABLE_IL2CPP
		[StaticField]
		public static bool EnableIL2CPP = true;
#else
		[StaticField]
		public static bool EnableIL2CPP = false;
#endif
		
		public static UnityEngine.Object LoadAssetAtPath(string s)
		{
#if UNITY_EDITOR	
			return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(s);
#else
			return null;
#endif
		}
		
		public static string[] GetAssetPathsFromAssetBundle(string assetBundleName)
		{
#if UNITY_EDITOR	
			return UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
#else
			return new string[0];
#endif
		}
		
		public static string[] GetAssetBundleDependencies(string assetBundleName, bool v)
		{
#if UNITY_EDITOR	
			return UnityEditor.AssetDatabase.GetAssetBundleDependencies(assetBundleName, v);
#else
			return new string[0];
#endif
		}
	}
}