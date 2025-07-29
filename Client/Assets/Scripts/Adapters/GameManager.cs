using System.Collections.Generic;
using Adapters.Character;
using Adapters.Input;
using Adapters.Projectiles;
using Core.Player;
using Core.Projectiles;
using UnityEngine;

namespace Adapters
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private InputListener _inputListener;

        private Player _player;
        private List<Player> _players = new();
        private List<IProjectile> _projectiles = new();

        public void SpawnPlayer(string id)
        {
            _player = new Player(id, _inputListener);
            _player.OnShoot += HandleShoot;
            PlayerView view = Instantiate(Resources.Load<GameObject>("Player").GetComponentInChildren<PlayerView>());
            view.Setup(_player);
            _players.Add(_player);
        }

        private void HandleShoot(Vector3 position, Vector3 direction)
        {
            Projectile projectile = new Projectile(position, direction);
            ProjectileView projectileView = Instantiate(Resources.Load<ProjectileView>("Projectile"));
            projectileView.Setup(projectile);
            _projectiles.Add(projectile);
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _players.Count; i++)
            {
                _players[i].Tick();
            }

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
