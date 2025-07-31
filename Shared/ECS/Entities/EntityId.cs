using System;

namespace Shared.ECS
{
    /// <summary>
    /// Uniquely identifies an entity in the ECS world.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly Guid Value;

        public EntityId(Guid value)
        {
            Value = value;
        }
        public static EntityId New() => new EntityId(Guid.NewGuid());
        public override string ToString() => Value.ToString();

        public bool Equals(EntityId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}