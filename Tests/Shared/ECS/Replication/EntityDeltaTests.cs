using System.Numerics;
using LiteNetLib.Utils;
using Shared.ECS;
using Shared.ECS.Replication;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Replication
{
    public class EntityDeltaTests
    {
        [Fact]
        public void SerializeAndDeserialize_FullDelta_ReturnsEqualDelta()
        {
            // Arrange
            var serializer = new JsonComponentSerializer();
            var originalDelta = new EntityDelta
            {
                EntityId = Guid.NewGuid(),
                IsNew = true,
                IsDestroyed = false,
                AddedOrModifiedComponents = new List<IComponent>
                {
                    new PositionComponent { Value = Vector3.UnitY },
                    new VelocityComponent { Value = Vector3.UnitX },
                },
                RemovedComponents = [typeof(RotationComponent)]
            };

            var writer = new NetDataWriter();
            var reader = new NetDataReader();

            // Act
            originalDelta.Serialize(writer, serializer);
            reader.SetSource(writer);

            var deserializedDelta = new EntityDelta();
            deserializedDelta.Deserialize(reader, serializer);

            // Assert basic properties
            Assert.Equal(originalDelta.EntityId, deserializedDelta.EntityId);
            Assert.Equal(originalDelta.IsNew, deserializedDelta.IsNew);
            Assert.Equal(originalDelta.IsDestroyed, deserializedDelta.IsDestroyed);

            // Compare Added or Modified components
            Assert.Equal(originalDelta.AddedOrModifiedComponents.Count, deserializedDelta.AddedOrModifiedComponents.Count);
            Assert.Equal(((PositionComponent)originalDelta.AddedOrModifiedComponents[0]).Value,
                ((PositionComponent)deserializedDelta.AddedOrModifiedComponents[0]).Value);
            Assert.Equal(((VelocityComponent)originalDelta.AddedOrModifiedComponents[1]).Value,
                ((VelocityComponent)deserializedDelta.AddedOrModifiedComponents[1]).Value);

            // Compare removed components
            Assert.Equal(originalDelta.RemovedComponents.Count, deserializedDelta.RemovedComponents.Count);
            for (int i = 0; i < originalDelta.RemovedComponents.Count; i++)
            {
                Assert.Equal(originalDelta.RemovedComponents[i], deserializedDelta.RemovedComponents[i]);
                Assert.Equal(originalDelta.RemovedComponents[i].AssemblyQualifiedName,
                    deserializedDelta.RemovedComponents[i].AssemblyQualifiedName);
            }
        }
    }
}