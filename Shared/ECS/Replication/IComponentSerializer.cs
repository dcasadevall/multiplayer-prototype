namespace Shared.ECS.Replication
{
    /// <summary>
    /// Defines methods for serializing and deserializing ECS components for replication.
    /// Implementations of this interface are responsible for converting components to and from a binary format,
    /// enabling efficient network transmission and reconstruction of component state.
    /// </summary>
    public interface IComponentSerializer
    {
        /// <summary>
        /// Serializes the specified component into a byte array.
        /// </summary>
        /// <param name="component">The component to serialize.</param>
        /// <returns>A byte array representing the serialized component.</returns>
        byte[] Serialize(IComponent component);

        /// <summary>
        /// Deserializes a component from the provided byte array.
        /// </summary>
        /// <param name="data">The byte array containing the serialized component data.</param>
        /// <returns>The deserialized <see cref="IComponent"/> instance.</returns>
        IComponent Deserialize(byte[] data);
    }
}