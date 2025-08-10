using Shared.Replication;

namespace Shared.ECS
{
    /// <summary>
    /// Marker interface for all components.
    /// Components should be structs that contain only data, not logic.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Serializes the component's data to a binary writer.
        /// </summary>
        /// <param name="writer">The writer to serialize the data to.</param>
        void Serialize(IComponentWriter writer);

        /// <summary>
        /// Deserializes the component's data from a binary reader.
        /// </summary>
        /// <param name="reader">The reader to deserialize the data from.</param>
        void Deserialize(IComponentReader reader);
    }
}