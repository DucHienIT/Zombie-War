using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.Weapons
{
    public sealed class Bomb : MonoBehaviour, IPoolable
    {
        [SerializeField] private Rigidbody _rigidbody;
        // Authored as a 1 m diameter ring; scaled to the blast diameter on launch.
        [SerializeField] private Transform _telegraphRing;
        [SerializeField] private TrailRenderer _trail;

        [Header("Telegraph")]
        // The ring breathes so the last half second reads as a countdown, not a static decal.
        [SerializeField] private float _telegraphPulseAmplitude = 0.08f;
        [SerializeField] private float _telegraphPulseFrequency = 5f;

        private float _fuse;
        private float _telegraphLead;
        private float _ringDiameter;
        private float _ringHeight;

        public Vector3 Position => _rigidbody.position;
        // Held on the bomb so the blast matches the ring the player was shown, even if a
        // level-up widens the radius while this one is still in the air.
        public float BlastRadius { get; private set; }

        private void Awake()
        {
            _ringHeight = _telegraphRing.localScale.y;
        }

        public void OnSpawned()
        {
            _telegraphRing.gameObject.SetActive(false);
            ClearTrail();
        }

        public void OnDespawned()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _telegraphRing.gameObject.SetActive(false);
            ClearTrail();
        }

        public void Launch(Vector3 velocity, float fuseDuration, float telegraphLead, float blastRadius)
        {
            _fuse = fuseDuration;
            _telegraphLead = telegraphLead;
            BlastRadius = blastRadius;
            _ringDiameter = blastRadius * 2f;
            ApplyRingScale(1f);
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = velocity;
        }

        public bool TickFuse(float deltaTime)
        {
            _fuse -= deltaTime;
            if (_fuse <= _telegraphLead)
            {
                if (!_telegraphRing.gameObject.activeSelf)
                {
                    _telegraphRing.gameObject.SetActive(true);
                }

                float elapsed = _telegraphLead - _fuse;
                float pulse = 1f + _telegraphPulseAmplitude * Mathf.Sin(elapsed * _telegraphPulseFrequency * Mathf.PI * 2f);
                ApplyRingScale(pulse);
            }

            return _fuse <= 0f;
        }

        private void ApplyRingScale(float multiplier)
        {
            float size = _ringDiameter * multiplier;
            _telegraphRing.localScale = new Vector3(size, _ringHeight, size);
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
