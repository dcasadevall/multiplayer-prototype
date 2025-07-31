using System.Text.Json.Serialization;

namespace Shared.ECS.Components;

/// <summary>
///     Stores the health state of an entity.
/// </summary>
public class HealthComponent : IComponent
{
    private int _maxHealth;
    private int _currentHealth;
    
    [JsonPropertyName("maxHealth")]
    public int MaxHealth 
    { 
        get => _maxHealth;
        set 
        { 
            _maxHealth = value;
            _currentHealth = value; // Initialize current health to max health
        }
    }
    
    public int CurrentHealth 
    { 
        get => _currentHealth;
        set => _currentHealth = value;
    }

    public bool IsDead => CurrentHealth <= 0;
    
    public HealthComponent() { }
    
    public HealthComponent(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }
}