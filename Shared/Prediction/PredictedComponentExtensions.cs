using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Entities;

namespace Shared.Prediction
{
    /// <summary>
    /// Extension methods for working with predicted components on entities.
    /// These helpers simplify adding, retrieving, and updating predicted state for client-side prediction and reconciliation.
    /// </summary>
    public static class PredictedComponentExtensions
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
        /// Gets the closed generic type for PredictedComponent[T] based on the provided component type.
        /// Uses a cache for high performance.
        /// That is, gets the PredictedComponent[T] type for the provided componentType of type T.
        /// </summary>
        public static Type GetPredictedType(Type componentType)
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
        /// Gets the local counterpart type of the given PredictedComponent[T].
        /// That is, if the predictedComponentType is PredictedComponent[T],
        /// this returns T, the original component type.
        /// </summary>
        /// <returns></returns>
        public static Type GetLocalType(Type predictedComponentType)
        {
            if (predictedComponentType == null) throw new ArgumentNullException(nameof(predictedComponentType));

            // Check if the type is a predicted component
            if (!IsPredicted(predictedComponentType))
            {
                throw new InvalidOperationException($"Type {predictedComponentType.Name} is not a predicted component.");
            }

            // Get the generic type argument, which is the original component type
            return predictedComponentType.GetGenericArguments().FirstOrDefault() ?? throw new InvalidOperationException(
                $"Predicted component {predictedComponentType.Name} does not have a valid generic argument.");
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
        public static bool TrySetServerAuthoritativeValue<T>(this Entity entity, IComponent serverComponent)
            where T : IComponent
        {
            return TrySetServerAuthoritativeValue(entity, typeof(T), serverComponent);
        }

        /// <summary>
        /// Gets the server authoritative value for a predicted component.
        /// This retrieves the ServerValue property from the PredictedComponent[T] wrapper.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IComponent GetServerAuthoritativeValue(this IComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            var prop = GetServerValueProperty(component.GetType());
            if (prop == null)
            {
                throw new InvalidOperationException(
                    $"Predicted component {component.GetType().Name} does not have a ServerValue property.");
            }

            return (IComponent)prop.GetValue(component);
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

            var predictedType = GetPredictedType(componentType);
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
        /// Returns true if the entity has a predicted component of the specified type.
        /// That is, if the entity has a component of type PredictedComponent[T]
        /// with T being the provided componentType.
        /// </summary>
        public static bool HasPredictedComponent(this Entity entity, Type componentType)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));

            var predictedType = GetPredictedType(componentType);
            return entity.Has(predictedType);
        }

        /// <summary>
        /// Attempts to get the predicted component of the specified type from the entity.
        /// That is, it tries to get the component of type PredictedComponent[T]
        /// where T is the provided componentType.
        /// </summary>
        public static bool TryGetPredictedComponent(this Entity entity, Type componentType, out IComponent? component)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));

            var predictedType = GetPredictedType(componentType);
            if (entity.TryGet(predictedType, out var predictedComponent))
            {
                component = predictedComponent;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Adds a predicted component to the entity.
        /// This should be used only on server-side to create a component that will be predicted by the client.
        /// Both ServerValue and the predicted component value are initialized to the provided component.
        /// </summary>
        /// <typeparam name="T">The type of the component to predict (must implement <see cref="IComponent"/>).</typeparam>
        /// <param name="entity">The entity to add the predicted component to.</param>
        /// <param name="component">The initial value for both server and client prediction.</param>
        /// <param name="replicationMode">The replication mode for this component, determining how often it should be replicated to clients.</param>
        /// <exception cref="ArgumentNullException">Thrown if entity is null.</exception>
        public static void AddPredictedComponent<T>(this Entity entity,
            T component,
            ReplicationMode replicationMode = ReplicationMode.EveryTick) where T : IComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            }

            // Create a new PredictedComponent that marks this component for prediction
            entity.AddComponent(new PredictedComponent<T>
            {
                Mode = replicationMode
            });

            // Add the component to the entity, which will hold the server state on the server,
            // and the predicted state on the client.
            entity.AddComponent(component);
        }

        /// <summary>
        /// Checks if the component is a predicted component.
        /// </summary>
        /// <param name="component">The component instance to check.</param>
        /// <returns>True if the component is of type PredictedComponent<T> for any T.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the component is null.</exception>
        public static bool IsPredicted(this IComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var type = component.GetType();
            return IsPredicted(type);
        }

        /// <summary>
        /// Checks if the component type is a predicted component.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>True if the component is of type PredictedComponent<T> for any Type.</returns>
        public static bool IsPredicted(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PredictedComponent<>);
        }

        /// <summary>
        /// Returns true if the predicted component should be replicated at the given tick number.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="tickNumber"></param>
        /// <returns></returns>
        public static bool ShouldBeReplicatedAtTick(this IComponent component, uint tickNumber)
        {
            var p = (IPredictedComponent)component;
            if (p.Mode.HasFlag(ReplicationMode.EveryTick))
            {
                return true;
            }

            if (p.Mode.HasFlag(ReplicationMode.SomeTicks) &&
                (tickNumber - p.LastSentAtTick >= p.ReplicationTickRate))
            {
                return true;
            }

            // InitialValue is only sent if it has never been sent before.
            if (p.Mode.HasFlag(ReplicationMode.InitialValue) &&
                p.LastSentAtTick == 0)
            {
                return true;
            }

            return false;
        }
    }
}