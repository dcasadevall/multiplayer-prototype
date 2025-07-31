using System.Runtime.Serialization;

namespace Shared.ECS.Components;

/// <summary>
///     Stores the health state of an entity.
/// </summary>
public class HealthComponent(int maxHealth) : IComponent
{
    public int CurrentHealth = maxHealth;
    public int MaxHealth = maxHealth;

    public bool IsDead => CurrentHealth <= 0;
}