
using System.Collections.Generic;
using CyberHub.Foundry.Editor;

namespace CyberHub.Foundry
{
    public class FoundryCoreModuleDefinition: IModuleDefinition
    {
        public string ModuleName()
        {
            return "Foundry Core";
        }

        public List<ProvidedService> GetProvidedServices()
        {
            return new List<ProvidedService>
            {
            };
        }

        public List<UsedService> GetUsedServices()
        {
            return new List<UsedService>
            {
            };
        }

        public FoundryModuleConfig GetModuleConfig()
        {
            return FoundryCoreConfig.GetAsset();
        }
    }
}
