using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.Weapons
{
    public sealed class Bomb : MonoBehaviour, IPoolable
    {
        [SerializeField] private Rigidbody _rigidbody;
        // Authored as a 1 m diameter ring; scaled to the blast diameter on launch.
        [SerializeField] private Transform _telegraphRing;

        private float _fuse;
        private float _telegraphLead;

        public Vector3 Position => _rigidbody.position;
        // Held on the bomb so the blast matches the ring the player was shown, even if a
        // level-up widens the radius while this one is still in the air.
        public float BlastRadius { get; private set; }

        public void OnSpawned()
        {
            _telegraphRing.gameObject.SetActive(false);
        }

        public void OnDespawned()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _telegraphRing.gameObject.SetActive(false);
        }

        public void Launch(Vector3 velocity, float fuseDuration, float telegraphLead, float blastRadius)
        {
            _fuse = fuseDuration;
            _telegraphLead = telegraphLead;
            BlastRadius = blastRadius;
            _telegraphRing.localScale = new Vector3(blastRadius * 2f, _telegraphRing.localScale.y, blastRadius * 2f);
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = velocity;
        }

        public bool TickFuse(float deltaTime)
        {
            _fuse -= deltaTime;
            if (_fuse <= _telegraphLead && !_telegraphRing.gameObject.activeSelf)
            {
                _telegraphRing.gameObject.SetActive(true);
            }

            return _fuse <= 0f;
        }
    }
}
