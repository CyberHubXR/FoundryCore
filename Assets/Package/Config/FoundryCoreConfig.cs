using System;
using System.Collections.Generic;
using UnityEngine;

namespace CyberHub.Foundry
{
    /// <summary>
    /// Contains all settings to do with the foundry core package, not to be confused with the foundry app config, which stores settings about the application as a whole.
    /// </summary>
    public class FoundryCoreConfig : FoundryModuleConfig
    {
        public string AppKey = "";
        
        [HideInInspector]
        public string OverrideDatabaseUrl = "";
        
#if UNITY_EDITOR
        public static FoundryCoreConfig GetAsset()
        {
            return GetOrCreateAsset<FoundryCoreConfig>("FoundryCoreConfig.asset");
        }
#endif
        public override void RegisterServices(Dictionary<Type, ServiceConstructor> constructors)
        {
            
        }

        public string GetDatabaseUrl()
        {
            return string.IsNullOrEmpty(OverrideDatabaseUrl) ? "https://api.cyberhubxr.com" : OverrideDatabaseUrl;
        }
    }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(FoundryCoreConfig))]
    public class FoundryCoreCoreConfigEditor : UnityEditor.Editor
    {
        private bool showAdvanced = false;
        
        private UnityEditor.SerializedProperty appKey;
        private UnityEditor.SerializedProperty overrideDatabaseUrl;

        public void OnEnable()
        {
            appKey = serializedObject.FindProperty("AppKey");
            overrideDatabaseUrl = serializedObject.FindProperty("OverrideDatabaseUrl");
        }

        public override void OnInspectorGUI()
        {
            var config = (FoundryCoreConfig) target;
            UnityEditor.EditorGUILayout.PropertyField(appKey);
            showAdvanced = UnityEditor.EditorGUILayout.Foldout(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                UnityEditor.EditorGUILayout.PropertyField(overrideDatabaseUrl);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif 
}
