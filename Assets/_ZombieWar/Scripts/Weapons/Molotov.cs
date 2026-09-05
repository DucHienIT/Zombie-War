using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.Weapons
{
    public sealed class Molotov : MonoBehaviour, IPoolable
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private TrailRenderer _trail;

        [Header("Flight")]
        // Degrees per second the bottle tumbles end over end while airborne.
        [SerializeField] private float _tumbleSpeed = 360f;

        private float _flightRemaining;

        public Vector3 Position => _rigidbody.position;

        public void OnSpawned() => ClearTrail();

        public void OnDespawned()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            ClearTrail();
        }

        public void Launch(Vector3 velocity, float flightTime)
        {
            _flightRemaining = flightTime;
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = velocity;
            Vector3 tumbleAxis = Vector3.Cross(Vector3.up, velocity).normalized;
            _rigidbody.angularVelocity = tumbleAxis * (_tumbleSpeed * Mathf.Deg2Rad);
        }

        // True once the flight time has run out: the bottle shatters wherever it is by then.
        public bool TickFlight(float deltaTime)
        {
            _flightRemaining -= deltaTime;
            return _flightRemaining <= 0f;
        }

        // The pool teleports the body before this runs; without the clear the trail would streak
        // from wherever the previous throw ended.
        private void ClearTrail()
        {
            if (_trail != null)
            {
                _trail.Clear();
            }
        }
    }
}
