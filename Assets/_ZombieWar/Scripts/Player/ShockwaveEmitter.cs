using Cinemachine;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Enemies;
using ZombieWar.VFX;

namespace ZombieWar.Player
{
    // The persistent half of the shockwave ability: one ring mesh authored on the Player that
    // is scaled out to the blast radius, plus the blast itself. Force is uniform across the
    // radius so everyone caught in it is launched, not just the closest.
    public sealed class ShockwaveEmitter : MonoBehaviour
    {
        private const string LogPrefix = "[Ability]";
        private const int BlastBufferSize = 96;
        private const float ZeroDirectionSqr = 0.0001f;

        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private AudioService _audio;
        [SerializeField] private CinemachineImpulseSource _impulseSource;
        [SerializeField] private LayerMask _enemyMask;

        [Header("Ring")]
        // Authored at 1 m diameter; scaled out to the blast diameter over the ring duration.
        [SerializeField] private Transform _ring;
        [SerializeField] private float _ringDuration = 0.35f;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _burstVfx;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _cameraImpulse = 0.4f;

        private readonly Collider[] _blastBuffer = new Collider[BlastBufferSize];
        private Transform _transform;
        private float _ringHeight;
        private float _ringDiameter;
        private float _ringTimer;

        private void Awake()
        {
            _transform = transform;
            bool missing = _zombies == null || _vfx == null || _audio == null || _impulseSource == null || _ring == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} ShockwaveEmitter has an unassigned reference.", this);
                return;
            }

            _ringHeight = _ring.localScale.y;
            _ring.gameObject.SetActive(false);
        }

        public int CountZombiesWithin(float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(_transform.position, radius, _blastBuffer, _enemyMask, QueryTriggerInteraction.Ignore);
            int zombies = 0;
            for (int i = 0; i < count; i++)
            {
                if (_zombies.TryGetZombie(_blastBuffer[i], out ZombieController zombie) && zombie.IsTargetable)
                {
                    zombies++;
                }
            }

            return zombies;
        }

        public void Emit(float radius, float damage, float force)
        {
            Vector3 center = _transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, radius, _blastBuffer, _enemyMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!_zombies.TryGetZombie(_blastBuffer[i], out ZombieController zombie) || !zombie.IsTargetable)
                {
                    continue;
                }

                Vector3 direction = zombie.Position - center;
                direction.y = 0f;
                direction = direction.sqrMagnitude > ZeroDirectionSqr ? direction.normalized : _transform.forward;
                zombie.TakeDamage(new DamageInfo(damage, zombie.Position, direction, force, DamageSource.Bomb));
            }

            _ringDiameter = radius * 2f;
            _ringTimer = _ringDuration;
            ApplyRingScale(0f);
            _ring.gameObject.SetActive(true);
            _vfx.Play(_burstVfx, center, Quaternion.identity);
            _audio.PlayWorld(_clip, center);
            _impulseSource.GenerateImpulseWithForce(_cameraImpulse);
        }

        private void Update()
        {
            if (_ringTimer <= 0f)
            {
                return;
            }

            _ringTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(_ringTimer / _ringDuration);
            // Ease-out: the wave races out and settles at the edge it actually hit.
            float eased = 1f - (1f - progress) * (1f - progress);
            ApplyRingScale(eased);
            if (_ringTimer <= 0f)
            {
                _ring.gameObject.SetActive(false);
            }
        }

        private void ApplyRingScale(float progress)
        {
            float size = _ringDiameter * progress;
            _ring.localScale = new Vector3(size, _ringHeight, size);
        }
    }
}
