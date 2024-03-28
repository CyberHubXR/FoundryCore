using System;
using System.IO;
using UnityEngine;

namespace CyberHub.Brane
{
    public class BraneAppConfig : ScriptableObject
    {
        public BraneModuleConfig[] modules = Array.Empty<BraneModuleConfig>();
        public void RegisterServices(BraneApp app)
        {
            foreach (var module in modules)
                module.RegisterServices(app);
        }
        
#if UNITY_EDITOR
        public static BraneAppConfig GetAsset()
        {
            var asset = Resources.Load<BraneAppConfig>("BraneAppConfig");
            if (asset == null)
            {
                asset = CreateInstance<BraneAppConfig>();
                Directory.CreateDirectory("Assets/Brane/Resources");
                UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Brane/Resources/BraneAppConfig.asset");
            }

            return asset;
        }
#endif
    }
}
