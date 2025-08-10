using System;
using System.Collections.Generic;
using System.Threading;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// A thread-safe, dynamically-growing registry that maps component types to unique IDs
    /// for efficient network serialization. This avoids sending full type names over the network.
    /// It uses a ReaderWriterLockSlim for high-performance concurrent reads.
    /// </summary>
    public class ComponentTypeRegistry
    {
        private readonly Dictionary<Type, ushort> _typeToId = new();
        private readonly Dictionary<ushort, Type> _idToType = new();
        private ushort _nextId = 0;
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        /// Gets the unique ID for a given component type. If the type is not already registered,
        /// it will be dynamically and thread-safely assigned a new ID.
        /// </summary>
        public ushort GetId(Type type)
        {
            // 1. Enter a lock that allows reading but can be upgraded to a write lock.
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_typeToId.TryGetValue(type, out var id)) {
                    return id;
                }

                // 2. The type doesn't exist, so upgrade to a full write lock.
                _lock.EnterWriteLock();
                try
                {
                    // The double-check is still necessary! Another upgradeable reader
                    // might have upgraded and added the type while we waited.
                    if (_typeToId.TryGetValue(type, out id)) {
                        return id;
                    }
                    
                    if (_nextId == ushort.MaxValue) {
                        throw new InvalidOperationException("ComponentTypeRegistry has reached its maximum capacity.");
                    }

                    id = _nextId++;
                    _typeToId[type] = id;
                    _idToType[id] = type;
                    return id;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Gets the component type for a given unique ID.
        /// </summary>
        public Type GetType(ushort id)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_idToType.TryGetValue(id, out var type))
                    throw new KeyNotFoundException($"Component ID {id} is not registered.");

                return type;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}