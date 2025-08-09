using System;
using Core.ECS.Entities;
using Core.ECS.Rendering;
using Shared;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Simulation;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Scheduling;
using ILogger = Shared.Logging.ILogger;

namespace Core.Input
{
    /// <summary>
    /// System that handles client-side projectile creation and shoot cooldown.
    /// For a brief period of time after a player presses the shoot button,
    /// the system will create a predicted projectile entity.
    /// The <see cref="EntityViewSystem"/> handles destroying the predicted projectile
    /// when the server sends the authoritative projectile entity.
    /// </summary>
    public class PredictedPlayerShotSystem : ISystem, IInitializable, IDisposable
    {
        private readonly IInputListener _inputListener;
        private readonly EntityRegistry _entityRegistry;
        private readonly IMessageSender _messageSender;
        private readonly ITickSync _tickSync;
        private readonly int _localPeerId;
        private readonly ILogger _logger;
        
        // Cooldown tracking
        private uint _lastShotTick;

        public PredictedPlayerShotSystem(
            IInputListener inputListener,
            EntityRegistry entityRegistry,
            IMessageSender messageSender,
            IClientConnection clientConnection,
            ITickSync tickSync,
            ILogger logger)
        {
            _inputListener = inputListener;
            _entityRegistry = entityRegistry;
            _messageSender = messageSender;
            _tickSync = tickSync;
            _logger = logger;
            _localPeerId = clientConnection.AssignedPeerId;
        }

        public void Initialize()
        {
            _inputListener.OnShoot += HandleShootInput;
        }

        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
        }
        
        public void Dispose()
        {
            _inputListener.OnShoot -= HandleShootInput;
        }
        
        private void HandleShootInput()
        {
            var clientTick = _tickSync.ClientTick;
            
            // Check cooldown
            if (clientTick < _lastShotTick + GameplayConstants.PlayerShotCooldown.ToNumTicks())
            {
                return;
            }
            _lastShotTick = clientTick;

            var localPlayer = _entityRegistry.GetLocalPlayerEntity(_localPeerId);

            // Create predicted projectile entity
            var projectile = ProjectileArchetype.CreateFromEntity(_entityRegistry, 
                localPlayer, 
                clientTick); 
            
            // This projectile will be destroyed once the server sends the authoritative projectile entity.
            // Adding LocalEntityTagComponent will take care of that.
            projectile.AddComponent<LocalEntityTagComponent>();
            
            // Send shot message to server
            var predictedProjectileId = projectile.Id;
            SendShotMessage(clientTick, predictedProjectileId.Value);

            _logger.Debug(LoggedFeature.Input, "Fired predicted projectile {0} at tick {1}", predictedProjectileId, _tickSync.ServerTick);
        }

        private void SendShotMessage(uint tick, Guid predictedProjectileId)
        {
            var shotMessage = new PlayerShotMessage
            {
                Tick = tick,
                PredictedProjectileId = predictedProjectileId
            };
            
            try
            {
                _messageSender.SendMessageToServer(MessageType.PlayerShot, shotMessage);
                _logger.Debug(LoggedFeature.Input, "Sent shot message for tick {0}", tick);
            }
            catch (Exception ex)
            {
                _logger.Error(LoggedFeature.Input, "Failed to send shot message: {0}", ex.Message);
            }
        }
    }
}