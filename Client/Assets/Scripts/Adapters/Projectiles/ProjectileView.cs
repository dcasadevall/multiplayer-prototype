using Core.Projectiles;
using UnityEngine;

namespace Adapters.Projectiles
{
    public class ProjectileView : MonoBehaviour
    {
        private IProjectile _projectile;

        public void Setup(IProjectile player)
        {
            _projectile = player;
            _projectile.OnExpire += () => Destroy(gameObject);
            enabled = true;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _projectile.Position, 0.8f);
        }
    }
}
