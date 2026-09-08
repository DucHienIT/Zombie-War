using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Enemies;

namespace ZombieWar.Player
{
    // The persistent half of the shockwave ability, which is an aura rather than a cast. One
    // looping effect authored on the Player burns for as long as the skill is owned and marks
    // how far it reaches; the beat itself is silent visually, because a ring flashing out once
    // a second read as a strobe over the top of an aura that already shows the radius. Damage
    // is uniform across the radius so the whole crowd pressing in takes the same tick.
    public sealed class ShockwaveEmitter : MonoBehaviour
    {
        private const string LogPrefix = "[Ability]";
        private const int BlastBufferSize = 96;
        private const float ZeroDirectionSqr = 0.0001f;
        // Guards the aura scale against a zero authored radius rather than dividing by it.
        private const float MinVisualRadius = 0.01f;

        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private AudioService _audio;
        [SerializeField] private LayerMask _enemyMask;

        [Header("Aura")]
        // The looping effect that IS the aura, authored as an inactive child and switched on for
        // as long as the skill is owned. It never restarts per beat - it simply burns.
        [SerializeField] private Transform _aura;
        // Radius its ground disc covers at scale 1, so the effect can be sized onto whatever
        // radius the current rank reaches instead of hard-coding a scale.
        [SerializeField] private float _auraVisualRadius = 3f;

        [Header("Feedback")]
        [SerializeField] private AudioClip _clip;
        // The aura pulses every second; a tail that long on every one of them would smear into
        // a drone, so the sound keeps its own slower beat.
        [SerializeField] private float _soundInterval = 2f;

        private readonly Collider[] _blastBuffer = new Collider[BlastBufferSize];
        private Transform _transform;
        private float _nextSoundTime;

        private void Awake()
        {
            _transform = transform;
            bool missing = _zombies == null || _audio == null || _aura == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} ShockwaveEmitter has an unassigned reference.", this);
                return;
            }

            _aura.gameObject.SetActive(false);
        }

        // Idempotent on purpose: the skill calls this on every rebuild with the radius its
        // current rank reaches, so the effect always matches the circle that actually hurts.
        public void SetAura(float radius)
        {
            float scale = radius / Mathf.Max(MinVisualRadius, _auraVisualRadius);
            _aura.localScale = new Vector3(scale, scale, scale);
            _aura.gameObject.SetActive(true);
        }

        // Only reached when a fresh run drops the skill; the aura has no reason to outlive it.
        public void Deactivate()
        {
            _aura.gameObject.SetActive(false);
        }

        // One beat of the aura: everything standing in it takes the tick and a shove outwards.
        public void Pulse(float radius, float damage, float knockback)
        {
            Vector3 center = _transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, radius, _blastBuffer, _enemyMask, QueryTriggerInteraction.Ignore);
            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                if (!_zombies.TryGetZombie(_blastBuffer[i], out ZombieController zombie) || !zombie.IsTargetable)
                {
                    continue;
                }

                Vector3 direction = zombie.Position - center;
                direction.y = 0f;
                direction = direction.sqrMagnitude > ZeroDirectionSqr ? direction.normalized : _transform.forward;
                zombie.TakeDamage(new DamageInfo(damage, zombie.Position, direction, knockback, DamageSource.Shock));
                hits++;
            }

            // A beat that caught nobody has nothing to sound off about.
            if (hits > 0 && Time.time >= _nextSoundTime)
            {
                _nextSoundTime = Time.time + _soundInterval;
                _audio.PlayWorld(_clip, center);
            }
        }
    }
}
