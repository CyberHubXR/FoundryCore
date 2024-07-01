using System;
using System.Collections.Generic;
using System.Linq;
using CyberHub.Foundry.Editor.UIUtils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CyberHub.Foundry.Editor
{
    public class FoundryConfigWindow : EditorWindow
    {
        [MenuItem("Foundry/Config", false, 1)]
        public static void OpenWindow()
        {
            var window = GetWindow<FoundryConfigWindow>(false, "Foundry Config Manager", true);
            window.Repaint();
        }

        private static List<IServiceDefinition> _serviceDefinitions = new();
        private static List<IModuleDefinition> _moduleDefinitions = new();
        
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CheckIfProjectValid()
        {
            UpdateServiceDefinitions();
            UpdateModuleDefinitions();
            var config = FoundryAppConfig.GetAsset();
            HashSet<Type> loadedServices = new();
            foreach (var loadedModule in config.modules)
            {
                if(!loadedModule || loadedModule.EnabledServices == null)
                    continue;
                int removed = loadedModule.EnabledServices.RemoveAll(s => string.IsNullOrWhiteSpace(s.ToString()));
                if (removed > 0)
                    EditorUtility.SetDirty(loadedModule);
                foreach (var providedService in loadedModule.EnabledServices)
                {
                    Type serviceType = providedService;
                    Debug.Assert(serviceType != null, $"Service type {providedService} not found!");
                    loadedServices.Add(serviceType);
                }
            }
            

            bool allServicesLoaded = true;
            foreach(var loadedModule in _moduleDefinitions)
            {
                var usedServices = loadedModule.GetUsedServices();
                foreach (var usedService in usedServices)
                {
                    if (!loadedServices.Contains(usedService.ServiceInterface) && !usedService.optional)
                    {
                        allServicesLoaded = false;
                        break;
                    }
                }
            }

            if (!allServicesLoaded)
                OpenWindow();
        }

        private static void UpdateServiceDefinitions()
        {
            _serviceDefinitions.Clear();
            var serviceDefinitions = ClassFinder.FindAllWithInterface<IServiceDefinition>();
            foreach (var serviceDefinition in serviceDefinitions)
                _serviceDefinitions.Add((IServiceDefinition) Activator.CreateInstance(serviceDefinition));
        }
        
        private static void UpdateModuleDefinitions()
        {
            _moduleDefinitions.Clear();
            foreach (var moduleDefinition in ClassFinder.FindAllWithInterface<IModuleDefinition>())
                _moduleDefinitions.Add((IModuleDefinition) Activator.CreateInstance(moduleDefinition));
            var moduleConfigs = _moduleDefinitions.Select(m => m.GetModuleConfig()).ToArray();
            var modules = FoundryAppConfig.GetAsset().modules ?? new FoundryModuleConfig[0];
            bool changed = false;
            if (modules.Length != moduleConfigs.Length)
                changed = true;
            else
            {
                for (int i = 0; i < modules.Length; i++)
                {
                    if(modules[i] == moduleConfigs[i])
                        continue;
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                FoundryAppConfig.GetAsset().modules = moduleConfigs;
                EditorUtility.SetDirty(FoundryAppConfig.GetAsset());
            }
        }

        private void CreateGUI()
        {
            UpdateServiceDefinitions();
            UpdateModuleDefinitions();
            
            float headerFontSize = 16f;
            float fontSize = 14f;

            var black = new Color(0.1f, 0.1f, 0.1f, 1f);
            var grey = new Color(0.3f, 0.3f, 0.3f, 0.6f);

            VisualElement root = rootVisualElement;

            var scrollView = new ScrollView();
            scrollView.style.paddingLeft = 10;
            scrollView.style.paddingRight = 10;
            root.Add(scrollView);
            
            //I already wrote everything and am too lazy to refactor
            root = scrollView;
            
            
            //Logo
            var logoData = Resources.Load<Texture2D>("foundry_icon");
            var logo = new Image();

            logo.image = logoData;
            logo.style.maxHeight = 120;
            logo.style.alignSelf = Align.Center;
            logo.style.marginTop = 20;
            logo.style.marginBottom = 20;
            root.Add(logo);

            var configTitle = new Label("Config");
            configTitle.style.fontSize = headerFontSize;
            configTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            configTitle.style.marginBottom = 10;
            root.Add(configTitle);

            var modulesTitle = new Label("Modules:");
            modulesTitle.style.fontSize = fontSize + 1;
            root.Add(modulesTitle);
            
            foreach (var moduleDefinition in _moduleDefinitions)
            {
                var moduleBox = new Box();
                EditorUIUtils.SetPadding(moduleBox, 5f);
                EditorUIUtils.SetBorderRadius( moduleBox, 7f);
                moduleBox.style.backgroundColor = black;
                moduleBox.style.marginBottom = 10;
                root.Add(moduleBox);
                
                var moduleName = new Label(moduleDefinition.ModuleName());
                moduleName.style.fontSize = fontSize;
                moduleBox.Add(moduleName);

                var providedServices = moduleDefinition.GetProvidedServices();
                if (providedServices.Count != 0)
                {
                    var servicesImplemented = new Foldout();
                    servicesImplemented.value = false;
                    servicesImplemented.text = "Services Provided";
                    servicesImplemented.tooltip = "Services that this module defines and provides to others";
                    moduleBox.Add(servicesImplemented);
                    foreach(var service in providedServices)
                        servicesImplemented.contentContainer.Add(new Label(service.ServiceInterface.Name));
                }
                
                var servicesUsedList = moduleDefinition.GetUsedServices();
                if (servicesUsedList.Count != 0) {
                    var servicesUsed = new Foldout();
                    servicesUsed.value = false;
                    servicesUsed.text = "Services Used";
                    servicesUsed.tooltip = "Services that this module uses";
                    moduleBox.Add(servicesUsed);
                    foreach (var service in servicesUsedList)
                    {
                        string optionText = service.optional ? " (optional)" : "";
                        servicesUsed.contentContainer.Add(new Label(service.ServiceInterface.Name + optionText));
                    }
                }
                
                var scriptable = moduleDefinition.GetModuleConfig();
                var scriptableField = new ObjectField("Config Object");
                scriptableField.value = scriptable;
                scriptableField.focusable = true;
                scriptableField.RegisterValueChangedCallback(e =>
                {
                    scriptableField.value = scriptable;
                });
                moduleBox.Add(scriptableField);
                
                var moduleConfig = moduleDefinition.GetModuleConfig();
                var configEditor = UnityEditor.Editor.CreateEditor(moduleConfig);
                var visualElements = configEditor.CreateInspectorGUI();
                if (visualElements != null)
                {
                    moduleBox.Add(visualElements);
                    continue;
                }

                moduleBox.Add(new IMGUIContainer(() =>
                {
                    configEditor.OnInspectorGUI();
                    if (EditorUtility.IsDirty(moduleConfig))
                    {
                        if (GUILayout.Button("Save"))
                        {
                            AssetDatabase.SaveAssetIfDirty(moduleConfig);
                            AssetDatabase.Refresh();
                        }
                    }
                }));
            }
            
            var servicesTitle = new Label("Services:");
            servicesTitle.style.fontSize = fontSize + 1;
            root.Add(servicesTitle);

            var serviceProviders = new Dictionary<Type, List<KeyValuePair<ProvidedService, FoundryModuleConfig>>>();
            var requiredServices = new HashSet<Type>();
            foreach (var module in _moduleDefinitions)
            {
                var config = module.GetModuleConfig();
                foreach (var service in module.GetProvidedServices())
                {
                    List<KeyValuePair<ProvidedService, FoundryModuleConfig>> providerList;
                    if (!serviceProviders.TryGetValue(service.ServiceInterface, out providerList))
                    {
                        providerList = new ();
                        serviceProviders.Add(service.ServiceInterface, providerList);
                    }
                    
                    providerList.Add(new (service, config));
                }
                    
                foreach (var service in module.GetUsedServices())
                {
                    if (service.optional)
                        continue;
                    requiredServices.Add(service.ServiceInterface);
                }
            }

            foreach (var service in _serviceDefinitions)
            {
                bool required = requiredServices.Contains(service.ServiceInterface());
                Type sysType = service.ServiceInterface();
                
                var providerNames = new List<string>();
                var providerLocations = new List<FoundryModuleConfig>();
                if (!required)
                {
                    providerNames.Add("None");
                    providerLocations.Add(null);
                }
                
                int selectedConfig = 0;
                if(serviceProviders.TryGetValue(sysType, out var implementations))
                {
                    foreach (var impl in implementations)
                    {
                        if(impl.Value.IsServiceEnabled(sysType))
                            selectedConfig = providerNames.Count;
                       providerNames.Add(impl.Key.ImplementationName);
                       providerLocations.Add(impl.Value);
                    }
                }
                
                if(required && providerNames.Count == 0)
                    providerNames.Add("Missing!");
                
                var dropdown = new DropdownField(service.PrettyName(), providerNames, providerNames[selectedConfig]);

                if (required && providerNames[selectedConfig] == "Missing!")
                {
                    EditorUIUtils.SetBorderColor(dropdown, Color.red);
                    EditorUIUtils.SetBorderRadius(dropdown, 2);
                    EditorUIUtils.SetBorderWidth(dropdown, 2);
                }
                root.Add(dropdown);
                if (providerLocations.Count > 0)
                {
                    providerLocations[selectedConfig]?.EnableService(sysType);
                    dropdown.RegisterValueChangedCallback(e =>
                    {
                        for (int i = 0; i < providerLocations.Count; i++)
                        {
                            if (providerNames[i] == e.newValue)
                                providerLocations[i]?.EnableService(service.ServiceInterface());
                            else if (providerNames[i] == e.previousValue)
                                providerLocations[i]?.DisableService(service.ServiceInterface());
                        }
                        AssetDatabase.SaveAssetIfDirty(providerLocations[selectedConfig]);
                    });
                }
            }
        }
    }
}
