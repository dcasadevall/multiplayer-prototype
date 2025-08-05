using Core.PlayerDeprecated;
using UnityEngine;

namespace Adapters.Player
{
    public class PlayerView : MonoBehaviour
    {
        private IPlayer _player;

        public void Setup(IPlayer player)
        {
            _player = player;
            enabled = true;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _player.Position, 0.8f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _player.Rotation, 0.8f);
        }
    }
}
