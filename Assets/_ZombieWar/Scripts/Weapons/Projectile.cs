using UnityEngine;
using ZombieWar.Data;
using ZombieWar.Utils;
using ZombieWar.VFX;

namespace ZombieWar.Weapons
{
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private TrailRenderer _trail;

        private Transform _transform;

        public Vector3 Position => _transform.position;
        public Vector3 Direction { get; private set; }
        public float Speed { get; private set; }
        public float Radius { get; private set; }
        public float Damage { get; private set; }
        public float Knockback { get; private set; }
        public float RemainingLife { get; private set; }
        public PooledVfx FleshImpactVfx { get; private set; }
        public PooledVfx PropImpactVfx { get; private set; }

        private void Awake()
        {
            _transform = transform;
        }

        public void OnSpawned()
        {
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        public void OnDespawned()
        {
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        public void Launch(Vector3 direction, GunDefinitionSO definition)
        {
            Direction = direction;
            Speed = definition.ProjectileSpeed;
            Radius = definition.ProjectileRadius;
            Damage = definition.Damage;
            Knockback = definition.Knockback;
            RemainingLife = definition.Range / definition.ProjectileSpeed;
            FleshImpactVfx = definition.FleshImpactVfx;
            PropImpactVfx = definition.PropImpactVfx;
            _transform.rotation = Quaternion.LookRotation(direction);
        }

        public bool Advance(float deltaTime)
        {
            _transform.position += Direction * (Speed * deltaTime);
            RemainingLife -= deltaTime;
            return RemainingLife > 0f;
        }
    }
}
