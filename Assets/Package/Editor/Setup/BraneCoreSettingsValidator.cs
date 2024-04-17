using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane.Editor;
using CyberHub.Brane.Setup;
using UnityEditor.VersionControl;

namespace CyberHub.Brane.Setup
{
    public class BraneCoreSettingsValidator: IModuleSetupTasks
    {
    
        public BraneCoreSettingsValidator()
        {
        
        }
    
        public IModuleSetupTasks.State GetTaskState()
        {
            var config = BraneCoreConfig.GetAsset();
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
                        BraneConfigWindow.OpenWindow();
                    }
                },
                disableAfterAction = false
            };
            appKeyTask.SetTextDescription("The app key is required to connect to the Brane service.");
            settings.Add(appKeyTask);
        
            lists.Add(settings);
            return lists;
        }

        public string ModuleName()
        {
            return "Brane Core";
        }

        public string ModuleSource()
        {
            return "com.cyberhub.brane.core";
        }
    }

}
