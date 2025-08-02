using System.Collections.Generic;
using System.Linq;
using Adapters.Input;
using Core.ECS.Rendering;
using Core.Input;
using Core.Player;
using LiteNetLib;
using Shared.ECS;
using Shared.ECS.Components;
using UnityEngine;

namespace Adapters.Character
{
    /// <summary>
    /// System responsible for creating player views and associating the Player class for the local player.
    /// </summary>
    public class PlayerViewSystem : ISystem
    {
        private readonly Dictionary<EntityId, int> _players = new();
        private readonly NetManager _netManager;
        private readonly IInputListener _inputListener;
        private readonly IEntityViewRegistry _entityViewRegistry;
        private readonly int _localPlayerId;
        private Player _localPlayer;

        public PlayerViewSystem(NetManager netManager, 
            IInputListener inputListener, 
            IEntityViewRegistry entityViewRegistry)
        {
            _netManager = netManager;
            _inputListener = inputListener;
            _entityViewRegistry = entityViewRegistry;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var peers = registry.GetAll().Where(x => x.Has<PeerComponent>());
            
            foreach (var peerEntity in peers)
            {
                var peerComponent = peerEntity.Get<PeerComponent>()!;
                var entityId = peerEntity.Id;

                // Create player view if it doesn't exist
                if (!_players.ContainsKey(entityId))
                {
                    _players[entityId] = peerComponent.PeerId;

                    // If this is the local player, associate the Player class
                    if (peerComponent!.PeerId == _localPlayerId && !_players.ContainsKey(entityId))
                    {
                        var player = new Player(_inputListener);
                        var playerView = _entityViewRegistry.GetEntityView(entityId);
                        playerView.gameObject.AddComponent<PlayerView>().Setup(player);
                    }
                }
            }
        }
    }
}