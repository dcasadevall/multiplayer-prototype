using System;
using System.Collections.Generic;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
using UnityEngine;

namespace Adapters.ECS.Debugging
{
    public class ServerPositionVisualizerSystem : ISystem
    {
        private readonly Dictionary<Guid, GameObject> _visualizations = new Dictionary<Guid, GameObject>();
        
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            foreach (var entity in registry.GetAll())
            {
                if (!entity.Has<PredictedComponent<PositionComponent>>()) continue;

                var predicted = entity.GetRequired<PredictedComponent<PositionComponent>>();
                var serverPos = predicted.ServerValue.Value;

                if (!_visualizations.ContainsKey(entity.Id.Value))
                {
                    var debugObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    _visualizations.Add(entity.Id.Value, debugObject);
                }
                
                _visualizations[entity.Id.Value].transform.position = new Vector3(serverPos.X, serverPos.Y, serverPos.Z);
            }
        }
    }
}