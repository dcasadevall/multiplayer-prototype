using System;
using Adapters.Character;
using Adapters.Input;
using Core;
using Core.Input;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.Scheduling;
using UnityEngine;

namespace Adapters
{
    /// <summary>
    /// Ths class is responsible for creating the service collection and
    /// initializing the service provider including core systems.
    /// It is our "root of composition".
    /// </summary>
    public class GameServiceInitializer : MonoBehaviour
    {
        [SerializeField] 
        private UnityServiceProvider serviceProvider;
        
        private void Awake()
        {
            var serviceCollection = new ServiceCollection();
            
            // Game systems
            serviceCollection.AddSingleton<ISystem, PlayerViewSystem>();
            
            // Input
            serviceCollection.AddSingleton<InputListener>();
            serviceCollection.AddSingleton<IInputListener>(sp => sp.GetRequiredService<InputListener>());
            serviceCollection.AddSingleton<ITickable>(sp => sp.GetRequiredService<InputListener>());
            
            // Core dependencies 
            serviceProvider.RegisterCoreServices(serviceCollection);
        }
    }
}