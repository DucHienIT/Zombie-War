using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.VFX
{
    public sealed class PooledVfx : MonoBehaviour, IPoolable
    {
        [SerializeField] private ParticleSystem _rootParticle;
        // Fixed lifetime instead of polling IsAlive on the whole hierarchy every frame.
        [SerializeField] private float _lifetime = 1f;

        private Transform _transform;
        private float _remaining;

        // True while the effect rides on another transform (a muzzle flash on a moving gun).
        public bool IsAttached { get; private set; }

        private void Awake()
        {
            _transform = transform;
        }

        public void OnSpawned()
        {
            _remaining = _lifetime;
            _rootParticle.Play(true);
        }

        public void OnDespawned()
        {
            _rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Parents the effect to the anchor so it follows every frame the anchor moves, including
        // the animator and barrel re-aim that run after the shot was fired.
        public void AttachTo(Transform anchor)
        {
            _transform.SetParent(anchor, false);
            _transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            IsAttached = true;
        }

        public void Detach(Transform poolParent)
        {
            _transform.SetParent(poolParent, false);
            IsAttached = false;
        }

        public bool Tick(float deltaTime)
        {
            _remaining -= deltaTime;
            return _remaining > 0f;
        }
    }
}
