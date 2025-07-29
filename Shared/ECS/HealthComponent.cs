namespace Shared.ECS;

/// <summary>
/// Stores the health state of an entity.
/// </summary>
public class HealthComponent : IComponent
{
    public int CurrentHealth;
    public int MaxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public HealthComponent() { }
    public HealthComponent(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }
}