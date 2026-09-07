using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.Weapons
{
    public sealed class Bomb : MonoBehaviour, IPoolable
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private TrailRenderer _trail;

        private float _fuse;

        public Vector3 Position => _rigidbody.position;
        // Held on the bomb so the blast matches the throw, even if a level-up widens the
        // radius while this one is still in the air.
        public float BlastRadius { get; private set; }

        public void OnSpawned()
        {
            ClearTrail();
        }

        public void OnDespawned()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            ClearTrail();
        }

        public void Launch(Vector3 velocity, float fuseDuration, float blastRadius)
        {
            _fuse = fuseDuration;
            BlastRadius = blastRadius;
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = velocity;
        }

        public bool TickFuse(float deltaTime)
        {
            _fuse -= deltaTime;
            return _fuse <= 0f;
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
