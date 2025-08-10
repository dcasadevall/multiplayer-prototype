using Shared.ECS;
using Shared.Replication;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Component that assigns a name to an entity.
    /// </summary>
    public class NameComponent : IComponent
    {
        public string Name { get; set; } = string.Empty;

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(Name);
        }

        public void Deserialize(IComponentReader reader)
        {
            Name = reader.GetString();
        }
    }
}