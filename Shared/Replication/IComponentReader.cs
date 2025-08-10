using System;
using System.Numerics;
using Shared.ECS;

namespace Shared.Replication
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
        /// Reads a byte value from the stream.
        /// </summary>
        /// <returns></returns>
        byte GetByte();

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

        /// <summary>
        /// Reads a quantized Vector3 previously written with PutVector3Q using the same step size.
        /// This is useful for reading values that were quantized to save bandwidth.
        /// </summary>
        /// <param name="step">Quantization step used during write.</param>
        /// <returns>Dequantized Vector3.</returns>
        Vector3 GetVector3Q(float step);

        /// <summary>
        /// Reads a compressed unit quaternion previously written with PutQuaternionCompressed.
        /// </summary>
        /// <returns>Decompressed unit quaternion.</returns>
        Quaternion GetQuaternionCompressed();
    }
}