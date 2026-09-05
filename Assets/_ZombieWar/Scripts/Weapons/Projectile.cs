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
        private int _pierceRemaining;

        public Vector3 Position => _transform.position;
        public Vector3 Direction { get; private set; }
        public float Speed { get; private set; }
        public float Radius { get; private set; }
        public float Damage { get; private set; }
        public float Knockback { get; private set; }
        public float RemainingLife { get; private set; }
        // The body this bullet already went through, skipped on the next cast so one pierce
        // is not spent over and over on the same wide zombie.
        public Collider LastPiercedCollider { get; private set; }
        public PooledVfx FleshImpactVfx { get; private set; }
        public PooledVfx PropImpactVfx { get; private set; }

        private void Awake()
        {
            _transform = transform;
        }

        public void OnSpawned()
        {
            LastPiercedCollider = null;
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        public void OnDespawned()
        {
            LastPiercedCollider = null;
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        // Ballistics come from the static definition; what the shot hits for comes from the
        // gun's upgrade level and the run's passives, already resolved into ShotStats.
        public void Launch(Vector3 direction, GunDefinitionSO definition, in ShotStats shot)
        {
            Direction = direction;
            Speed = definition.ProjectileSpeed;
            Radius = definition.ProjectileRadius;
            Damage = shot.Damage;
            Knockback = shot.Knockback;
            _pierceRemaining = shot.Pierce;
            LastPiercedCollider = null;
            RemainingLife = definition.Range / definition.ProjectileSpeed;
            FleshImpactVfx = definition.FleshImpactVfx;
            PropImpactVfx = definition.PropImpactVfx;
            _transform.rotation = Quaternion.LookRotation(direction);
        }

        // True when the bullet survives the hit and carries on.
        public bool TryPierce(Collider body)
        {
            if (_pierceRemaining <= 0)
            {
                return false;
            }

            _pierceRemaining--;
            LastPiercedCollider = body;
            return true;
        }

        public bool Advance(float deltaTime)
        {
            _transform.position += Direction * (Speed * deltaTime);
            RemainingLife -= deltaTime;
            return RemainingLife > 0f;
        }
    }
}
