using System.Collections.Generic;
using Adapters.Input;
using Adapters.Projectiles;
using Core;
using Core.Projectiles;
using UnityEngine;

namespace Adapters
{
    public class GameManager : MonoBehaviour
    {
        private List<IProjectile> _projectiles = new();
        
        private void HandleShoot(Vector3 position, Vector3 direction)
        {
            Projectile projectile = new Projectile(position, direction);
            ProjectileView projectileView = Instantiate(Resources.Load<ProjectileView>("Projectile"));
            projectileView.Setup(projectile);
            _projectiles.Add(projectile);
        }

        private void FixedUpdate()
        {
            // The ECS world is now managed by ClientWorldManager
            // We can still run the legacy game logic here for now

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectiles[i].Tick();

                if (_projectiles[i].Expired)
                {
                    _projectiles.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
