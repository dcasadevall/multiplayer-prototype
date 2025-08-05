using System;
using UnityEngine;

namespace Core.PlayerDeprecated
{
    public interface IPlayer : IDisposable
    {
        event OnShootHandler OnShoot;

        Vector3 Position { get; }
        Quaternion Rotation { get; }

        void Tick();
    }
}
