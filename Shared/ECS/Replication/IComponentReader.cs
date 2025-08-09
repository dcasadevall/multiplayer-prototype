using System;
using System.Numerics;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Defines a generic interface for reading component data from a binary stream.
    /// This provides an abstraction layer that decouples component deserialization
    /// from any specific networking library.
    /// </summary>
    public interface IComponentReader
    {
        /// <summary>
        /// Reads an integer value from the stream.
        /// </summary>
        int GetInt();

        /// <summary>
        /// Reads an unsigned integer value from the stream.
        /// </summary>
        uint GetUInt();

        /// <summary>
        /// Reads a float value from the stream.
        /// </summary>
        float GetFloat();

        /// <summary>
        /// Reads a string value from the stream.
        /// </summary>
        string GetString();

        /// <summary>
        /// Reads a boolean value from the stream.
        /// </summary>
        bool GetBool();

        /// <summary>
        /// Reads a Vector3 value from the stream.
        /// </summary>
        Vector3 GetVector3();

        /// <summary>
        /// Reads a Quaternion value from the stream.
        /// </summary>
        Quaternion GetQuaternion();

        /// <summary>
        /// Reads a component from the stream.
        /// </summary>
        T GetComponent<T>() where T : IComponent;

        /// <summary>
        /// Reads a Guid value from the stream.
        /// </summary>
        Guid GetGuid();
    }
}