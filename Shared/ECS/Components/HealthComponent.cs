namespace Shared.ECS.Components;

/// <summary>
///     Stores the health state of an entity.
/// </summary>
public class HealthComponent : IComponent
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
}