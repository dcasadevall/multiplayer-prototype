using System;
using System.Collections.Generic;

namespace Shared.ECS.Entities
{
    /// <summary>
    /// Represents a single entity in the ECS world.
    /// Entities are identified by an <see cref="EntityId"/> and are composed of <see cref="IComponent"/>s.
    /// </summary>
    public class Entity
    {
        private readonly Dictionary<Type, IComponent> _components = new();

        public EntityId Id { get; }

        public Entity(EntityId id)
        {
            Id = id;
        }

        /// <summary>
        /// Add a component to the entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to add.</typeparam>
        /// <param name="component">The component to add.</param>
        public void AddComponent<T>(T component) where T : IComponent
        {
            _components[typeof(T)] = component;
        }

        /// <summary>
        /// Add a new empty component to the entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to add.</typeparam>
        public void AddComponent<T>() where T : IComponent, new()
        {
            var component = new T();
            _components[typeof(T)] = component;
        }

        /// <summary>
        /// Add a component to the entity.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <param name="componentType"></param>
        public void AddComponent(IComponent component, Type componentType)
        {
            _components[componentType] = component;
        }

        /// <summary>
        /// Adds a component to the entity, or replaces the existing component of the same type.
        /// </summary>
        /// <param name="component">The component to add or replace.</param>
        public void AddOrReplaceComponent(IComponent component)
        {
            var type = component.GetType();
            _components[type] = component;
        }

        /// <summary>
        /// Try to get a component from the entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <param name="component">The component to get.</param>
        /// <returns>True if the component was found, false otherwise.</returns>
        public bool TryGet<T>(out T component) where T : class, IComponent
        {
            if (_components.TryGetValue(typeof(T), out var value))
            {
                component = (value as T)!;
                return true;
            }

            // Ok to suppress nullability warning here because we return false if not found
            component = null!;
            return false;
        }

        /// <summary>
        /// Try to get a component from the entity.
        /// </summary>
        /// <param name="componentType"> The type of the component to get.</param>
        /// <param name="component">The component to get.</param>
        /// <returns>True if the component was found, false otherwise.</returns>
        public bool TryGet(Type componentType, out IComponent component)
        {
            if (_components.TryGetValue(componentType, out var value))
            {
                component = value;
                return true;
            }

            // Ok to suppress nullability warning here because we return false if not found
            component = null!;
            return false;
        }

        /// <summary>
        /// Gets the component of the given type from the entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns>The component of the given type, or null if not found.</returns>
        public T? Get<T>() where T : class, IComponent
        {
            if (TryGet(out T component))
            {
                return component;
            }

            return null;
        }

        /// <summary>
        /// Gets the component of the given type from the entity, or throws an exception if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T GetRequired<T>() where T : class, IComponent
        {
            if (TryGet(out T component))
            {
                return component;
            }

            // If the component is not found, throw an exception
            // This is useful for components that are expected to always be present.
            throw new InvalidOperationException($"Entity {Id} does not have required component of type {typeof(T).Name}");
        }

        /// <summary>
        /// Check if the entity has a component of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the component to check for.</typeparam>
        /// <returns>True if the entity has the component, false otherwise.</returns>
        public bool Has<T>() where T : IComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Check if the entity has a component of the given type.
        /// </summary>
        /// <returns>True if the entity has the component, false otherwise.</returns>
        public bool Has(Type componentType)
        {
            return _components.ContainsKey(componentType);
        }

        /// <summary>
        /// Remove a component from the entity.
        /// </summary>
        /// <typeparam name="T">The type of the component to remove.</typeparam>
        public void Remove<T>() where T : IComponent
        {
            _components.Remove(typeof(T));
        }

        /// <summary>
        /// Remove a component from the entity.
        /// </summary>
        /// <param name="type">The type of the component to remove.</param>
        public void Remove(Type type)
        {
            _components.Remove(type);
        }

        /// <summary>
        /// Get all components of the entity.
        /// </summary>
        /// <returns>An enumerable of all components of the entity.</returns>
        public IEnumerable<IComponent> GetAllComponents()
        {
            return _components.Values;
        }
    }
}