using System;
using Core.Input;
using Shared.Scheduling;
using UnityEngine;

namespace Adapters.Input
{
    public class InputListener : IInputListener, ITickable
    {
        public event Action OnShoot;

        public Vector2 Movement { get; private set; }

        public void Tick()
        {
            if (UnityEngine.Input.GetKeyUp(KeyCode.Space))
            {
                OnShoot?.Invoke();
            }

            Movement = new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
        }
    }
}
