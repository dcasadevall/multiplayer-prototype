using System.Numerics;
using System.Text.Json;
using Shared.ECS;
using Shared.ECS.Components;

namespace Server.Scenes;

public class EntityDescription
{
    public Dictionary<string, JsonElement> Components { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public static class SceneLoader
{
    public static void Load(string path, EntityRegistry registry)
    {
        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<EntityDescription>>(json);

        foreach (var entry in entries)
        {
            var entity = registry.CreateEntity();
            foreach (var comp in entry.Components)
                switch (comp.Key)
                {
                    case "PositionComponent":
                        entity.AddComponent(new PositionComponent(ToVector3(comp.Value)));
                        break;
                    case "HealthComponent":
                        entity.AddComponent(new HealthComponent(comp.Value.GetProperty("maxHealth").GetInt32()));
                        break;
                }
        }
    }

    private static Vector3 ToVector3(JsonElement el)
    {
        return new Vector3(el.GetProperty("x").GetSingle(), el.GetProperty("y").GetSingle(),
            el.GetProperty("z").GetSingle());
    }
}