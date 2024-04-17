
using System.Collections.Generic;
using CyberHub.Brane.Editor;

namespace CyberHub.Brane
{
    public class BraneCoreModuleDefinition: IModuleDefinition
    {
        public string ModuleName()
        {
            return "Brane Core";
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

        public BraneModuleConfig GetModuleConfig()
        {
            return BraneCoreConfig.GetAsset();
        }
    }
}
