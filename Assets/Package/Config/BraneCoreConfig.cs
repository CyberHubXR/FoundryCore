using System;
using System.Collections.Generic;
using System.IO;

namespace CyberHub.Brane
{
    /// <summary>
    /// Contains all settings to do with the foundry core package, not to be confused with the foundry app config, which stores settings about the application as a whole.
    /// </summary>
    public class BraneCoreConfig : BraneModuleConfig
    {
        public string AppKey = "";
        public string OverrideApiUrl = "";
#if UNITY_EDITOR
        public static BraneCoreConfig GetAsset()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<BraneCoreConfig>(
                "Assets/Brane/Settings/BraneCoreConfig.asset");  
            if (asset == null)
            {
                asset = CreateInstance<BraneCoreConfig>();
                Directory.CreateDirectory("Assets/Brane/Settings");
                UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Brane/Settings/BraneCoreConfig.asset");
            }
            return asset;
        }
#endif
        public override void RegisterServices(Dictionary<Type, ServiceConstructor> constructors)
        {
            
        }
        
        public string GetApiUrl()
        {
            return string.IsNullOrWhiteSpace(OverrideApiUrl) ? "https://35.215.38.35" : OverrideApiUrl;
        }
    }
}
