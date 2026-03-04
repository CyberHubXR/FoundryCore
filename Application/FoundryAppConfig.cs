using System;
using System.IO;
using UnityEngine;

namespace CyberHub.Foundry
{
    public class FoundryAppConfig : ScriptableObject
    {
        // Ordered list of module configs participating in startup. Each module can expose
        // its own toggles while still sharing a single boot pipeline.
        public FoundryModuleConfig[] modules = Array.Empty<FoundryModuleConfig>();

        public void RegisterServices(FoundryApp app)
        {
            // Startup is delegated to each module so packages can stay loosely coupled and
            // only register the services they own.
            foreach (var module in modules)
                module.RegisterServices(app);
        }
        
#if UNITY_EDITOR
        public static FoundryAppConfig GetAsset()
        {
            // Editor helper used by setup tooling: load existing config or lazily create one
            // at the conventional Resources path so runtime boot can always find it.
            var asset = Resources.Load<FoundryAppConfig>("FoundryAppConfig");
            if (asset == null)
            {
                asset = CreateInstance<FoundryAppConfig>();
                Directory.CreateDirectory("Assets/Foundry/Resources");
                UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Foundry/Resources/FoundryAppConfig.asset");
            }

            return asset;
        }
#endif
    }
}
