using HybridCLR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;


namespace ET
{
	public class CodeLoader: Singleton<CodeLoader> 
	{
		private Assembly model;

		public static List<string> AOTMetaAssemblyNames { get; } = new List<string>()
		{
			"mscorlib.dll",
			"System.dll",
			"System.Core.dll", // 如果使用了Linq，需要这个
			"Unity.Core.dll",
			"Unity.ThirdParty.dll",
			"Unity.Loader.dll",
			"MongoDB.Bson.dll",
		};

		public void Start(MonoBehaviour initMono)
		{
#if ENABLE_IL2CPP || UNITY_EDITOR
			initMono?.StartCoroutine(this.DownLoadAssets(this.StartGame));
#else
			if (Define.EnableCodes)
			{
				GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
				if (globalConfig.CodeMode != CodeMode.ClientServer)
				{
					throw new Exception("ENABLE_CODES mode must use ClientServer code mode!");
				}
				
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(assemblies);
				EventSystem.Instance.Add(types);
				foreach (Assembly ass in assemblies)
				{
					string name = ass.GetName().Name;
					if (name == "Unity.Model.Codes")
					{
						this.model = ass;
					}
				}
				
				IStaticMethod start = new StaticMethod(this.model, "ET.Entry", "Start");
				start.Run();
			}
			else
			{
				byte[] assBytes;
				byte[] pdbBytes;
				if (!Define.IsEditor)
				{
					Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
					assBytes = ((TextAsset)dictionary["Model.dll"]).bytes;
					pdbBytes = ((TextAsset)dictionary["Model.pdb"]).bytes;
				}
				else
				{
					assBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, "Model.dll"));
					pdbBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, "Model.pdb"));
				}
			
				this.model = Assembly.Load(assBytes, pdbBytes);
				this.LoadHotfix();
			
				IStaticMethod start = new StaticMethod(this.model, "ET.Entry", "Start");
				start.Run();
			}
#endif
		}

		// 热重载调用该方法
		public void LoadHotfix()
		{
			byte[] assBytes;
			byte[] pdbBytes;
			if (!Define.IsEditor)
			{
				Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
				assBytes = ((TextAsset)dictionary["Hotfix.dll"]).bytes;
				pdbBytes = ((TextAsset)dictionary["Hotfix.pdb"]).bytes;
			}
			else
			{
				// 傻屌Unity在这里搞了个傻逼优化，认为同一个路径的dll，返回的程序集就一样。所以这里每次编译都要随机名字
				string[] logicFiles = Directory.GetFiles(Define.BuildOutputDir, "Hotfix_*.dll");
				if (logicFiles.Length != 1)
				{
					throw new Exception("Logic dll count != 1");
				}
				string logicName = Path.GetFileNameWithoutExtension(logicFiles[0]);
				assBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, $"{logicName}.dll"));
				pdbBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, $"{logicName}.pdb"));
			}

			Assembly hotfixAssembly = Assembly.Load(assBytes, pdbBytes);
			
			Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(typeof (Game).Assembly, typeof(Init).Assembly, this.model, hotfixAssembly);
			
			EventSystem.Instance.Add(types);
		}

		private string GetWebRequestPath(string asset)
		{
			var path = $"{Application.streamingAssetsPath}/{asset}";
			if (!path.Contains("://"))
			{
				path = "file://" + path;
			}
			if (path.EndsWith(".dll"))
			{
				path += ".bytes";
			}
			return path;
		}

		System.Collections.IEnumerator DownLoadAssets(Action onDownloadComplete)
		{
			//var assets = new List<string>();
			//assets.AddRange(HotfixDll);
			//assets.AddRange(AOTMetaAssemblyNames);

			var assets = HotfixDll.Concat(AOTMetaAssemblyNames);

			foreach (var asset in assets)
			{
				string dllPath = GetWebRequestPath(asset);
				Debug.Log($"start download asset:{dllPath}");
				UnityWebRequest www = UnityWebRequest.Get(dllPath);
				yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(www.error);
				}
#else
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.Log(www.error);
            }
#endif
				else
				{
					// Or retrieve results as binary data
					byte[] assetData = www.downloadHandler.data;
					Debug.Log($"dll:{asset}  size:{assetData.Length}");
					s_assetDatas[asset] = assetData;
				}
			}

			onDownloadComplete();
		}

		private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();
		public static byte[] GetAssetData(string dllName)
		{
			return s_assetDatas[dllName];
		}

		List<string> HotfixDll = new List<string>()
		{
			"Unity.Model.Codes.dll",
			"Unity.ModelView.Codes.dll",
			"Unity.Hotfix.Codes.dll",
			"Unity.HotfixView.Codes.dll"
		};

		void StartGame()
		{
			LoadMetadataForAOTAssemblies();

			List<Assembly> listAssembly = new List<Assembly>();
			foreach (var item in HotfixDll)
			{
				byte[] assModelBytes = GetAssetData(item);
				var assembly = Assembly.Load(assModelBytes);
				if (item == "Unity.Model.Codes.dll")
				{
					this.model = assembly;
				}
				listAssembly.Add(assembly);
			}

			listAssembly.Add(typeof(Game).Assembly);
			listAssembly.Add(typeof(Init).Assembly);

			Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(listAssembly.ToArray());
			EventSystem.Instance.Add(types);

			IStaticMethod start = new StaticMethod(this.model, "ET.Entry", "Start");
			start.Run();
		}

		/// <summary>
		/// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
		/// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
		/// </summary>
		private static void LoadMetadataForAOTAssemblies()
		{
			// 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
			// 我们在BuildProcessors里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

			/// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
			/// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
			/// 
			HomologousImageMode mode = HomologousImageMode.SuperSet;
			foreach (var aotDllName in AOTMetaAssemblyNames)
			{
				byte[] dllBytes = GetAssetData(aotDllName);
				// 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
				LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
				Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
			}
		}
	}
}