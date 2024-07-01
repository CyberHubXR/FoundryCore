using System.Collections;
using System.Collections.Generic;
using CyberHub.Foundry.Setup;
using CyberHub.Foundry.Editor;
using UnityEditor.VersionControl;

namespace CyberHub.Foundry.Setup
{
    public class FoundryCoreSettingsValidator: IModuleSetupTasks
    {
    
        public FoundryCoreSettingsValidator()
        {
        
        }
    
        public IModuleSetupTasks.State GetTaskState()
        {
            var config = FoundryCoreConfig.GetAsset();
            return string.IsNullOrWhiteSpace(config.AppKey) ? IModuleSetupTasks.State.UncompletedRequiredTasks : IModuleSetupTasks.State.Completed;
        }

        public List<SetupTaskList> GetTasks()
        { 
            var lists = new List<SetupTaskList>();
        
            var settings = new SetupTaskList("Settings");
            var appKeyTask = new SetupTask
            {
                name = "App Key",
                action = new SetupAction
                {
                    name = "Open Config Window",
                    callback = () =>
                    {
                        FoundryConfigWindow.OpenWindow();
                    }
                },
                disableAfterAction = false
            };
            appKeyTask.SetTextDescription("The app key is required to connect to the Foundry service.");
            settings.Add(appKeyTask);
        
            lists.Add(settings);
            return lists;
        }

        public string ModuleName()
        {
            return "Foundry Core";
        }

        public string ModuleSource()
        {
            return "com.cyberhub.foundry.core";
        }
    }

}
