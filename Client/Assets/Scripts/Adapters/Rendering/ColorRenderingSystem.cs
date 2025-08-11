using Core.ECS.Rendering;
using Shared.ECS;
using Shared.ECS.Components;

namespace Adapters.Rendering
{
    public class ColorRenderingSystem : ISystem
    {
        private readonly IEntityViewRegistry _viewRegistry;

        public ColorRenderingSystem(IEntityViewRegistry viewRegistry)
        {
            _viewRegistry = viewRegistry;
        }

        public void Update(Shared.ECS.Entities.EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            foreach (var entity in registry.With<ColorComponent>())
            {
                if (_viewRegistry.TryGetEntityView(entity.Id, out var view))
                {
                    if (view.TryGetComponent<ColorSetter>(out var colorSetter))
                    {
                        var color = entity.GetRequired<ColorComponent>().Value;
                        colorSetter.SetColor(ToUnityColor(color));
                    }
                }
            }
        }
        
        private static UnityEngine.Color ToUnityColor(System.Drawing.Color color)
        {
            // Convert from 0-255 range to 0-1 range for Unity
            if (color.A < 255)
            {
                // If the color has transparency, use the alpha channel
                return new UnityEngine.Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            }
            
            // Otherwise, ignore alpha channel
            return new UnityEngine.Color(color.R / 255f, color.G / 255f, color.B / 255f, 1f);
        }
    }
}
