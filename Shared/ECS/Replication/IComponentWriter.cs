using System;
using System.Numerics;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Defines a generic interface for writing component data to a binary stream.
    /// This provides an abstraction layer that decouples component serialization
    /// from any specific networking library.
    /// </summary>
    public interface IComponentWriter
    {
        /// <summary>
        /// Writes an integer value to the stream.
        /// </summary>
        void Put(int value);

        /// <summary>
        /// Writes an unsigned integer value to the stream.
        /// </summary>
        void Put(uint value);

        /// <summary>
        /// Writes a float value to the stream.
        /// </summary>
        void Put(float value);

        /// <summary>
        /// Writes a string value to the stream.
        /// </summary>
        void Put(string value);

        /// <summary>
        /// Writes a boolean value to the stream.
        /// </summary>
        void Put(bool value);

        /// <summary>
        /// Writes a Vector3 value to the stream.
        /// </summary>
        void Put(Vector3 value);

        /// <summary>
        /// Writes a Quaternion value to the stream.
        /// </summary>
        void Put(Quaternion value);

        /// <summary>
        /// Writes a component to the stream.
        /// </summary>
        void Put(IComponent value);

        /// <summary>
        /// Writes a Guid value to the stream.
        /// </summary>
        void Put(Guid value);
    }
}