using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.Level
{
    public sealed class FireZone : MonoBehaviour, IPoolable
    {
        public enum Phase
        {
            Telegraph,
            Burning,
            Finished
        }

        // Authored at 1 m diameter; scaled to the zone diameter on ignite.
        [SerializeField] private Transform _telegraphRing;
        [SerializeField] private ParticleSystem _flames;

        private Transform _transform;
        private float _telegraphDuration;
        private float _activeDuration;
        private float _tickInterval;
        private float _timer;
        private float _tickTimer;

        public Phase CurrentPhase { get; private set; }
        public Vector3 Position => _transform.position;

        private void Awake()
        {
            _transform = transform;
        }

        public void OnSpawned()
        {
            CurrentPhase = Phase.Telegraph;
        }

        public void OnDespawned()
        {
            _flames.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _telegraphRing.gameObject.SetActive(false);
        }

        public void Ignite(float radius, float telegraphDuration, float activeDuration, float tickInterval)
        {
            _telegraphDuration = telegraphDuration;
            _activeDuration = activeDuration;
            _tickInterval = tickInterval;
            _timer = telegraphDuration;
            _tickTimer = 0f;
            CurrentPhase = Phase.Telegraph;

            float diameter = radius * 2f;
            _telegraphRing.localScale = new Vector3(diameter, _telegraphRing.localScale.y, diameter);
            _telegraphRing.gameObject.SetActive(true);
            _flames.transform.localScale = Vector3.one * radius;
        }

        // Returns true on frames where a damage tick is due.
        public bool Tick(float deltaTime)
        {
            switch (CurrentPhase)
            {
                case Phase.Telegraph:
                    TickTelegraph(deltaTime);
                    return false;
                case Phase.Burning:
                    return TickBurning(deltaTime);
                default:
                    return false;
            }
        }

        private void TickTelegraph(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            CurrentPhase = Phase.Burning;
            _timer = _activeDuration;
            _tickTimer = 0f;
            _telegraphRing.gameObject.SetActive(false);
            _flames.Play(true);
        }

        private bool TickBurning(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                CurrentPhase = Phase.Finished;
                _flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return false;
            }

            _tickTimer -= deltaTime;
            if (_tickTimer > 0f)
            {
                return false;
            }

            _tickTimer += _tickInterval;
            return true;
        }
    }
}
