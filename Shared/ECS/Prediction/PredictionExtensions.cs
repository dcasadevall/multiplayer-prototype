using System;
using System.Collections.Generic;
using System.Reflection;
using Shared.ECS.Entities;
using Shared.ECS.Replication;

namespace Shared.ECS.Prediction
{
    /// <summary>
    /// Extension methods for working with predicted components on entities.
    /// These helpers simplify adding, retrieving, and updating predicted state for client-side prediction and reconciliation.
    /// </summary>
    public static class PredictionExtensions
    {
        // The cache stores the results of MakeGenericType, mapping a component type (e.g., typeof(Position))
        // to its corresponding predicted type (e.g., typeof(PredictedComponent<Position>)).
        private static readonly Dictionary<Type, Type> _predictedTypeCache = new();

        // This cache stores the PropertyInfo for the ServerValue property of PredictedComponent<T>.
        private static readonly Dictionary<Type, PropertyInfo> _serverValuePropertyCache = new();

        // A simple lock to ensure thread safety if this code is ever called from multiple threads.
        private static readonly object _predictedTypeCacheLock = new();

        // A lock for the server value property cache to ensure thread safety.
        private static readonly object _serverValuePropertyCacheLock = new();

        /// <summary>
        /// Gets the closed generic type for PredictedComponent<T> based on the provided component type.
        /// Uses a cache for high performance.
        /// </summary>
        private static Type GetPredictedComponentType(Type componentType)
        {
            lock (_predictedTypeCacheLock)
            {
                // Check for cache hits
                if (_predictedTypeCache.TryGetValue(componentType, out var predictedType))
                {
                    return predictedType;
                }

                // Use reflection and cache the result
                predictedType = typeof(PredictedComponent<>).MakeGenericType(componentType);
                _predictedTypeCache[componentType] = predictedType;
                return predictedType;
            }
        }

        /// <summary>
        /// Gets the PropertyInfo for the ServerValue property of a PredictedComponent<T>.
        /// </summary>
        /// <param name="predictedComponentType"></param>
        /// <returns></returns>
        private static PropertyInfo? GetServerValueProperty(Type predictedComponentType)
        {
            lock (_serverValuePropertyCacheLock)
            {
                // Check if we already have the property cached
                if (_serverValuePropertyCache.TryGetValue(predictedComponentType, out var prop))
                {
                    return prop;
                }

                // Use reflection to get the ServerValue property
                prop = predictedComponentType.GetProperty("ServerValue", BindingFlags.Public | BindingFlags.Instance);

                // Cache the property info if found
                if (prop != null)
                {
                    _serverValuePropertyCache[predictedComponentType] = prop;
                }

                return prop;
            }
        }

        /// <summary>
        /// Sets the ServerValue field of the predicted component, given the component type and value
        /// if the entity has a predicted component of that type.
        /// </summary>
        public static bool TrySetServerAuthoritativeValue(this Entity entity, Type componentType, IComponent serverComponent)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            if (serverComponent == null) throw new ArgumentNullException(nameof(serverComponent));

            var predictedType = GetPredictedComponentType(componentType);
            if (!entity.TryGet(predictedType, out var wrapper))
            {
                return false;
            }

            var prop = GetServerValueProperty(predictedType);
            if (prop == null)
            {
                return false;
            }

            try
            {
                prop.SetValue(wrapper, serverComponent);
                return true;
            }
            catch (Exception ex)
            {
                // Optionally log this
                Console.WriteLine($"Failed to set ServerValue: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adds a predicted component to the entity.
        /// This should be used only on server-side to create a component that will be predicted by the client.
        /// Both ServerValue and the predicted component value are initialized to the provided component.
        /// </summary>
        /// <typeparam name="T">The type of the component to predict (must implement <see cref="IComponent"/>).</typeparam>
        /// <param name="entity">The entity to add the predicted component to.</param>
        /// <param name="component">The initial value for both server and client prediction.</param>
        /// <exception cref="ArgumentNullException">Thrown if entity is null.</exception>
        public static void AddPredictedComponent<T>(this Entity entity, T component) where T : IComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            }

            // Create a new PredictedComponent that marks this component for prediction
            entity.AddComponent(new PredictedComponent<T>());

            // Add the component to the entity, which will hold the server state on the server,
            // and the predicted state on the client.
            entity.AddComponent(component);
        }
    }
}