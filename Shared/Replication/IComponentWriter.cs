using System;
using System.Numerics;
using Shared.ECS;

namespace Shared.Replication
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

        void Put(Guid value);

        /// <summary>
        /// Writes a quantized Vector3 using 3 x Int16 with the given step size.
        /// For example, step 0.01f stores values at centimeter precision in meters.
        /// </summary>
        /// <param name="value">Vector to write.</param>
        /// <param name="step">Quantization step (units per LSB), e.g. 0.01f.</param>
        void PutVector3Q(Vector3 value, float step);

        /// <summary>
        /// Writes a compressed unit quaternion using the smallest-three scheme:
        /// drops the largest component, stores the remaining three as Int16 in [-32767, 32767],
        /// and a header byte encoding dropped index and sign. Total 7 bytes.
        /// </summary>
        /// <param name="value">Unit quaternion to write. Will be normalized if needed.</param>
        void PutQuaternionCompressed(Quaternion value);
    }
}