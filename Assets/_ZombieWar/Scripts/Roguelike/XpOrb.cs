using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.Roguelike
{
    // One dropped bundle of experience. It holds nothing but its own state; XpOrbManager
    // moves every orb from a single Update so a hundred of them cost one loop, not a hundred.
    public sealed class XpOrb : MonoBehaviour, IPoolable
    {
        private Transform _transform;

        public float Value { get; private set; }
        public Vector3 DropPoint { get; private set; }
        public Vector3 RestPoint { get; private set; }
        public float Age { get; set; }
        public bool Attracted { get; set; }
        public float FlySpeed { get; set; }

        public Vector3 Position => _transform.position;

        private void Awake()
        {
            _transform = transform;
        }

        public void Drop(float value, Vector3 dropPoint, Vector3 restPoint)
        {
            Value = value;
            DropPoint = dropPoint;
            RestPoint = restPoint;
            Age = 0f;
            Attracted = false;
            FlySpeed = 0f;
        }

        public void SetPosition(Vector3 position) => _transform.position = position;

        public void Spin(float degrees) => _transform.Rotate(0f, degrees, 0f, Space.Self);

        public void OnSpawned() { }

        public void OnDespawned()
        {
            Attracted = false;
            Age = 0f;
        }
    }
}
