using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Utils;
using ZombieWar.VFX;

namespace ZombieWar.Weapons
{
    public sealed class ProjectileManager : MonoBehaviour
    {
        private const string LogPrefix = "[Weapon]";

        [SerializeField] private Projectile _prefab;
        [SerializeField] private int _prewarmCount = 180;
        [SerializeField] private Transform _poolParent;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private LayerMask _hitMask;

        private ComponentPool<Projectile> _pool;
        private List<Projectile> _active;

        private void Awake()
        {
            if (_prefab == null || _zombies == null || _vfx == null)
            {
                Debug.LogError($"{LogPrefix} ProjectileManager has an unassigned reference.", this);
                return;
            }

            _pool = new ComponentPool<Projectile>(_prefab, _poolParent, _prewarmCount);
            _active = new List<Projectile>(_prewarmCount);
        }

        public void Spawn(Vector3 origin, Vector3 direction, GunDefinitionSO definition, in ShotStats shot)
        {
            Projectile projectile = _pool.Get(origin, Quaternion.LookRotation(direction));
            projectile.Launch(direction, definition, shot);
            _active.Add(projectile);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Projectile projectile = _active[i];
                float step = projectile.Speed * deltaTime;
                bool hitSomething = Physics.SphereCast(projectile.Position, projectile.Radius, projectile.Direction,
                    out RaycastHit hit, step, _hitMask, QueryTriggerInteraction.Ignore);

                // A pierced body stays in the bullet's path for a frame or two, so the collider
                // it just went through is ignored rather than eating another pierce.
                if (hitSomething && hit.collider != projectile.LastPiercedCollider && ResolveHit(projectile, hit))
                {
                    Despawn(i);
                    continue;
                }

                if (!projectile.Advance(deltaTime))
                {
                    Despawn(i);
                }
            }
        }

        // True when the bullet is spent. Only flesh can be pierced; a wall always stops it.
        private bool ResolveHit(Projectile projectile, in RaycastHit hit)
        {
            if (_zombies.TryGetZombie(hit.collider, out ZombieController zombie))
            {
                var info = new DamageInfo(projectile.Damage, hit.point, projectile.Direction, projectile.Knockback, DamageSource.Bullet);
                zombie.TakeDamage(info);
                _vfx.Play(projectile.FleshImpactVfx, hit.point, Quaternion.LookRotation(hit.normal));
                return !projectile.TryPierce(hit.collider);
            }

            _vfx.Play(projectile.PropImpactVfx, hit.point, Quaternion.LookRotation(hit.normal));
            return true;
        }

        private void Despawn(int index)
        {
            Projectile projectile = _active[index];
            _active.RemoveAtSwap(index);
            _pool.Release(projectile);
        }
    }
}
