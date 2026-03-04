using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

namespace CyberHub.Foundry
{
    /// <summary>
    /// The core for Foundry. This class is responsible for managing systems and services.
    /// </summary>
    public class FoundryApp
    {
        // Foundry initializes itself as soon as the type is loaded so downstream code can
        // immediately request services without a manual bootstrap call.
        private static FoundryApp _instance = new ();
        
        /// <summary>
        /// Where modular singleton services are stored.
        /// </summary>
        private readonly ServiceContainer services = new();
        
        private FoundryAppConfig config;
        // Runtime lookup table used by GetConfig<T>() to return module-specific settings quickly.
        private readonly Dictionary<Type, FoundryModuleConfig> moduleConfigs = new();

        internal FoundryApp()
        {
            // The app configuration lives in a Resources folder so it can be loaded both
            // in editor tooling and in player builds without an explicit scene reference.
            config = Resources.Load<FoundryAppConfig>("FoundryAppConfig");
            Debug.Assert(config, "FoundryAppConfig not found!");

            // Each module is given a chance to register its service constructors and then
            // instantiate only the services that are marked as enabled in project settings.
            config.RegisterServices(this);

            // Cache module configs by their concrete type so call sites can query config via
            // GetConfig<T>() without walking the modules array every time.
            foreach(var module in config.modules)
                moduleConfigs.Add(module.GetType(), module);
        }
        
        /// <summary>
        /// Add a service to the App instance.
        /// </summary>
        /// <param name="type">Interface this service implements</param>
        /// <param name="service">Class instance that implements the interface</param>
        public void AddService(Type type, object service)
        {
            // ServiceContainer does not guard against accidental overrides, so we assert here
            // to catch duplicate registrations early during startup.
            Debug.Assert(services.GetService(type) == null, "Service implementing " + type.Name +" already exists!");
            services.AddService(type, service);
        }
        
        /// <summary>
        /// Get a Foundry service. Returns an error if the service was not found.
        /// </summary>
        /// <param name="type">Interface to get</param>
        /// <returns></returns>
        public object GetService(Type type)
        {
            var result = services.GetService(type);
            // A missing service is considered an invalid app state for this API. If callers
            // want nullable behavior they should use TryGetService.
            Debug.Assert(result != null, "Service implementing " + type.FullName + " not found!");
            return result;
        }

        /// <summary>
        /// Try getting a Foundry service.
        /// </summary>
        /// <param name="type">Interface type of the service</param>
        /// <param name="service">returned service, null if not found</param>
        /// <returns>true if the service was found</returns>
        public bool TryGetService(Type type, out object service)
        {
            // Non-throwing/ non-asserting variant used by optional integrations.
            service = services.GetService(type);
            return service != null;
        }

        /// <summary>
        /// Add a service to the Foundry instance.
        /// </summary>
        /// <typeparam name="I">Interface that this class implements</typeparam>
        /// <typeparam name="T">Service implementation to be instantiated</typeparam>
        public void AddService<I, T>()
            where T: new()
            => _instance.AddService(typeof(I), new T());
        
        /// <summary>
        /// Add a service to the Foundry instance.
        /// </summary>
        /// <typeparam name="I">Interface that this class implements</typeparam>
        /// <typeparam name="T">Service implementation to be added</typeparam>
        public void AddService<I, T>(T service)
            where T: new()
            => _instance.AddService(typeof(I), service);
        
        /// <summary>
        /// Get a Foundry service. 
        /// </summary>
        /// <typeparam name="T">Interface for the service you want</typeparam>
        /// <returns></returns>
        public static T GetService<T>()
            where T: class
            => _instance.GetService(typeof(T)) as T;

        /// <summary>
        /// Try getting a Foundry service. Returns true if the service was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryGetService<T>(out T service)
            where T : class
        {
            if (_instance.TryGetService(typeof(T), out object s))
            {
                // Safe because we key services by the interface type passed to TryGetService<T>().
                service = s as T;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Get the config object for a loaded module.
        /// </summary>
        /// <typeparam name="T">class deriving from FoundryModuleConfig</typeparam>
        /// <returns>Returns a config object, or null if it was not found</returns>
        public static T GetConfig<T>()
        where T : FoundryModuleConfig
        {
            // Config lookup intentionally returns null instead of asserting; modules may be
            // optional in a project and call sites can branch based on availability.
            if(_instance.moduleConfigs.TryGetValue(typeof(T), out FoundryModuleConfig config))
                return config as T;
            return null;
        }
            

        /// <summary>
        /// The currently active services running in the Foundry instance.
        /// </summary>
        public static ServiceContainer Services => _instance.services;
    }
}
