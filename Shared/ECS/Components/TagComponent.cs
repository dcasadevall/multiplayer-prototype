using Shared.Replication;

namespace Shared.ECS.Components
{
    /// <summary>
    /// A base class for tag components.
    /// Tag components are used to mark entities with a specific characteristic, but they do not contain any data.
    /// This base class provides an empty implementation of the serialization methods.
    /// </summary>
    public abstract class TagComponent : IComponent
    {
        /// <summary>
        /// This method is empty because tag components do not have any data to serialize.
        /// </summary>
        public void Serialize(IComponentWriter writer)
        {
        }

        /// <summary>
        /// This method is empty because tag components do not have any data to deserialize.
        /// </summary>
        public void Deserialize(IComponentReader reader)
        {
        }
    }
}