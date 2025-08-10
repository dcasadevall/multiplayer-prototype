using System;
using System.Collections.Generic;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// A registry that maps component types to unique IDs for efficient network serialization.
    /// This avoids sending full type names over the network. The mapping is generated at build time
    /// by the ComponentIdGenerator tool.
    /// </summary>
    public partial class ComponentTypeRegistry
    {
        private readonly Dictionary<Type, ushort> _typeToId = new();
        private readonly Dictionary<ushort, Type> _idToType = new();

        public ComponentTypeRegistry()
        {
            InitializeMapping();
        }

        /// <summary>
        /// Gets the unique ID for a given component type.
        /// </summary>
        public ushort GetId(Type type)
        {
            if (!_typeToId.TryGetValue(type, out var id))
                throw new KeyNotFoundException($"Component type {type.FullName} is not registered. Run the ComponentIdGenerator tool.");
            return id;
        }

        /// <summary>
        /// Gets the component type for a given unique ID.
        /// </summary>
        public Type GetType(ushort id)
        {
            if (!_idToType.TryGetValue(id, out var type))
                throw new KeyNotFoundException($"Component ID {id} is not registered. Run the ComponentIdGenerator tool.");
            return type;
        }
        
        /// <summary>
        /// This partial method is implemented by the code generator.
        /// </summary>
        partial void InitializeMapping();
    }
}