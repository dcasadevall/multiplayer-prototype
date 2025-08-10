using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Shared.Replication
{
    /// <summary>
    /// A container for a collection of entity deltas, representing all the changes in the world since the last update.
    /// This message is sent from the server to clients to synchronize the game state.
    /// It is designed for binary serialization to be as efficient as possible.
    /// </summary>
    public class WorldDeltaMessage : INetSerializable
    {
        private readonly IComponentSerializer _componentSerializer;
        private readonly ComponentTypeRegistry _componentTypeRegistry;
        public List<EntityDelta> Deltas { get; set; } = new();

        public WorldDeltaMessage(IComponentSerializer componentSerializer, ComponentTypeRegistry componentTypeRegistry)
        {
            _componentSerializer = componentSerializer;
            _componentTypeRegistry = componentTypeRegistry;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((ushort)Deltas.Count);
            Deltas.ForEach(delta => delta.Serialize(writer, _componentSerializer, _componentTypeRegistry));
        }

        public void Deserialize(NetDataReader reader)
        {
            var count = reader.GetUShort();
            for (var i = 0; i < count; i++)
            {
                var delta = new EntityDelta();
                delta.Deserialize(reader, _componentSerializer, _componentTypeRegistry);
                Deltas.Add(delta);
            }
        }
    }
}