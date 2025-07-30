using System.Runtime.Serialization;

namespace Shared.ECS.Components;

/// <summary>
///     Stores the health state of an entity.
/// </summary>
public class HealthComponent : IComponent, ISerializable
{
    public int CurrentHealth;
    public int MaxHealth;

    public HealthComponent()
    {
    }

    public HealthComponent(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public bool IsDead => CurrentHealth <= 0;
    
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }
}