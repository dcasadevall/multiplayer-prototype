using System;
using System.Text.Json.Serialization;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Links an entity to a specific client.
    /// This component is used to identify which client owns a particular entity.
    /// </summary>
    public class ClientIdComponent : IComponent
    {
        /// <summary>
        /// The unique identifier of the client that owns this entity.
        /// </summary>
        [JsonPropertyName("clientId")]
        public Guid ClientId { get; set; }

        public ClientIdComponent()
        {
            ClientId = Guid.Empty;
        }

        public ClientIdComponent(Guid clientId)
        {
            ClientId = clientId;
        }
    }
} 