using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// A static registry that maps component types to unique IDs for efficient network serialization.
    /// This avoids sending full type names over the network.
    /// </summary>
    public class ComponentTypeRegistry
    {
        private readonly Dictionary<Type, ushort> _typeToId = new();
        private readonly Dictionary<ushort, Type> _idToType = new();

        /// <summary>
        /// Scans all loaded assemblies for types that implement <see cref="IComponent"/>
        /// </summary>
        public ComponentTypeRegistry()
        {
            var componentTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => t.AssemblyQualifiedName) // Guarantees consistent ordering
                .ToList();

            ushort id = 0;
            foreach (var type in componentTypes)
            {
                _typeToId[type] = id;
                _idToType[id] = type;
                id++;
            }
        }

        /// <summary>
        /// Gets the unique ID for a given component type.
        /// </summary>
        public ushort GetId(Type type)
        {
            if (!_typeToId.TryGetValue(type, out var id))
                throw new KeyNotFoundException($"Component type {type.FullName} is not registered. Ensure Initialize() has been called.");

            return id;
        }

        /// <summary>
        /// Gets the component type for a given unique ID.
        /// </summary>
        public Type GetType(ushort id)
        {
            if (!_idToType.TryGetValue(id, out var type))
                throw new KeyNotFoundException($"Component ID {id} is not registered. Ensure Initialize() has been called.");

            return type;
        }
    }
}