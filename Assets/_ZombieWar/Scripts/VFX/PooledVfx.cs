using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.VFX
{
    public sealed class PooledVfx : MonoBehaviour, IPoolable
    {
        [SerializeField] private ParticleSystem _rootParticle;
        // Fixed lifetime instead of polling IsAlive on the whole hierarchy every frame.
        [SerializeField] private float _lifetime = 1f;

        private float _remaining;

        public void OnSpawned()
        {
            _remaining = _lifetime;
            _rootParticle.Play(true);
        }

        public void OnDespawned()
        {
            _rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public bool Tick(float deltaTime)
        {
            _remaining -= deltaTime;
            return _remaining > 0f;
        }
    }
}
