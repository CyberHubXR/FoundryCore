using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace CyberHub.Foundry
{
    /// <summary>
    /// Modules override this class as a place to store settings specific to them, and to register services with FoundryCore.
    /// </summary>
    public abstract class FoundryModuleConfig : ScriptableObject
    {
        public delegate object ServiceConstructor();

        public const string ConfigPath = "Assets/Foundry/Settings";
        
        #if UNITY_EDITOR
        protected static T GetOrCreateAsset<T>(string assetName) where T : FoundryModuleConfig
        {
            var assetPath = Path.Join(ConfigPath, assetName);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);  
            if (asset == null)
            {
                asset = CreateInstance<T>();
                Directory.CreateDirectory(ConfigPath);
                UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }
        #endif

        [Serializable]
        public class SerializedType
        {
            [SerializeField]
            private string assemblyQualifiedName;
            
            protected bool Equals(SerializedType other)
            {
                return assemblyQualifiedName == other.assemblyQualifiedName;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((SerializedType)obj);
            }

            public override int GetHashCode()
            {
                return (assemblyQualifiedName != null ? assemblyQualifiedName.GetHashCode() : 0);
            }

            public Type Type
            {
                get {
                    try
                    {
                        return Type.GetType(assemblyQualifiedName);
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                }
            }

            public SerializedType(Type type)
            {
                Debug.Assert(type.AssemblyQualifiedName != null, "Type assembly qualified name is null!");
                assemblyQualifiedName = type.AssemblyQualifiedName;
            }

            public static implicit operator Type(SerializedType serializedType)
            {
                return serializedType.Type;
            }
            
            public static implicit operator SerializedType(Type type)
            {
                return new SerializedType(type);
            }
            
            public static bool operator==(SerializedType a, SerializedType b)
            {
                return a.assemblyQualifiedName == b.assemblyQualifiedName;
            }

            public static bool operator !=(SerializedType a, SerializedType b)
            {
                return !(a == b);
            }
            
            public static bool operator==(SerializedType a, Type b)
            {
                return a.assemblyQualifiedName == b.AssemblyQualifiedName;
            }

            public static bool operator !=(SerializedType a, Type b)
            {
                return !(a == b);
            }

            public override string ToString()
            {
                return assemblyQualifiedName;
            }
        }
        
        /// <summary>
        /// List of full paths to system interfaces enabled for this module
        /// </summary>
        public List<SerializedType> EnabledServices = new();

        internal void RegisterServices(FoundryApp instance)
        {
            Dictionary<Type, ServiceConstructor> constructors = new();
            RegisterServices(constructors);
            
            foreach (var system in EnabledServices)
            {
                Type systemType = system;
                Debug.Assert(systemType != null, $"Could not find type {system}!");
                Debug.Assert(constructors.ContainsKey(systemType), $"{GetType().Name} did not provide a constructor for {systemType.Name}!");
                instance.AddService(systemType, constructors[systemType]());
                Debug.Log("Registered service: " + systemType.FullName + " from module " + GetType().Name);
            }
        }
        
        /// <summary>
        /// Returns true if a system is enabled for this module
        /// </summary>
        /// <param name="systemType"></param>
        public bool IsServiceEnabled(Type systemType)
        {
            foreach (var system in EnabledServices)
            {
                if (system == systemType)
                    return true;
            }
            return false;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Enable a system for this module
        /// </summary>
        /// <param name="systemType"></param>
        public void EnableService(Type systemType)
        {
            if (!IsServiceEnabled(systemType))
            {
                Debug.Log("Enabled service: " + systemType.Name + " for " + GetType().Name + " module.");
                EnabledServices.Add(systemType);
                EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// Disable a system for this module
        /// </summary>
        /// <param name="systemType"></param>
        public void DisableService(Type systemType)
        {
            if (IsServiceEnabled(systemType))
            {
                EnabledServices.Remove(systemType);
                EditorUtility.SetDirty(this);
            }
        }
        #endif
        
        /// <summary>
        /// Called on startup to register service constructors with FoundryCore.
        /// </summary>
        /// <param name="constructors">Dictionary to add service constructors too. These will be called conditionally
        /// depending on the project config. </param>
        public abstract void RegisterServices(Dictionary<Type, ServiceConstructor> constructors);
    }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(FoundryModuleConfig), true)]
    public class FoundryModuleConfigEditor : UnityEditor.Editor
    {
        public bool showEnabledServices = false;
        public override void OnInspectorGUI()
        {
            var config = (FoundryModuleConfig) target;

            if (config.EnabledServices.Count != 0)
            {
                showEnabledServices = EditorGUILayout.BeginFoldoutHeaderGroup(showEnabledServices, "Enabled Services");
                if (showEnabledServices)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    foreach(var service in config.EnabledServices)
                        EditorGUILayout.LabelField(service.ToString());
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.Space();

            var prop = serializedObject.GetIterator();
            prop.Next(true);
            
            // Skip EnabledServices, and any other fields we don't want to show up in the inspector
            for (int i = 0; i < 10; i++)
                prop.Next(false);

            serializedObject.UpdateIfRequiredOrScript();
            
            // Draw the rest of the inspector
            while (prop.Next(false))
            {
                EditorGUILayout.PropertyField(prop);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
}
