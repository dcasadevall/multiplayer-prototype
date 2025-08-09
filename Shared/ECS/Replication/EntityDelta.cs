using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Shared.ECS.Entities;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Represents the changes (delta) to a single entity for replication between server and client.
    /// Used to efficiently synchronize only the differences in entity state, rather than full snapshots.
    /// This class is designed for binary serialization to minimize network overhead.
    /// </summary>
    public class EntityDelta : INetSerializable
    {
        /// <summary>
        /// The unique identifier of the entity whose state has changed.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Indicates whether this entity is newly created in this delta.
        /// If true, the client should create the entity; otherwise, apply changes to an existing entity.
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// If true, the entity has been destroyed and should be removed from the client-side registry.
        /// </summary>
        public bool IsDestroyed { get; set; }

        /// <summary>
        /// The list of components that have been added or modified on the entity since the last update.
        /// These should be applied to the entity on the receiving side.
        /// </summary>
        public List<IComponent> AddedOrModifiedComponents { get; set; } = new();

        /// <summary>
        /// The list of component types that have been removed from the entity since the last update.
        /// The receiver should remove these components from the entity.
        /// </summary>
        public List<Type> RemovedComponents { get; set; } = new();

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EntityId.ToByteArray());
            writer.Put(IsNew);
            writer.Put(IsDestroyed);

            // Serialize components
            writer.Put(AddedOrModifiedComponents.Count);
            foreach (var component in AddedOrModifiedComponents)
                ComponentSerializer.Serialize(writer, component);

            // Serialize removed components
            writer.Put(RemovedComponents.Count);
            foreach (var type in RemovedComponents)
                writer.Put(type.AssemblyQualifiedName);
        }

        public void Deserialize(NetDataReader reader)
        {
            var bytes = new byte[16];
            reader.GetBytes(bytes, 16);
            EntityId = new Guid(bytes);

            IsNew = reader.GetBool();
            IsDestroyed = reader.GetBool();

            // Deserialize components
            var count = reader.GetInt();
            for (var i = 0; i < count; i++)
                AddedOrModifiedComponents.Add(ComponentSerializer.Deserialize(reader));

            // Deserialize removed components
            count = reader.GetInt();
            for (var i = 0; i < count; i++)
                RemovedComponents.Add(Type.GetType(reader.GetString()));
        }
    }
}