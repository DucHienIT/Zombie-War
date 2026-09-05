using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Enemies;

namespace ZombieWar.Player
{
    // The persistent half of the orbiting-blade ability. Authored under Abilities (a sibling of
    // the Player, never its child) with its whole blade pool as children, so the skill asset only
    // ever switches blades on and sets numbers; nothing is created at runtime. It follows the
    // player's position only: parenting it to the soldier would make the ring swing with every
    // turn of the model.
    public sealed class OrbitBladesController : MonoBehaviour
    {
        private const string LogPrefix = "[Ability]";
        private const int HitBufferSize = 16;
        private const int TrackedZombieCapacity = 64;
        private const float StaleSweepInterval = 2f;

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private LayerMask _enemyMask;
        // Read for position only, so the ring keeps its own heading whichever way the soldier faces.
        [SerializeField] private Transform _anchor;
        // Height of the ring above the anchor, which sits at the soldier's feet.
        [SerializeField] private float _height = 1f;
        // Spun around Y; every blade is a child of it, placed on the orbit by Configure.
        [SerializeField] private Transform _pivot;
        // Authored count is the ceiling any stack level can reach.
        [SerializeField] private Transform[] _blades;
        // Contact radius of one blade, matched to the authored disc.
        [SerializeField] private float _bladeRadius = 0.35f;

        private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];
        private readonly Dictionary<ZombieController, float> _nextHitTime = new Dictionary<ZombieController, float>(TrackedZombieCapacity);
        private readonly List<ZombieController> _stale = new List<ZombieController>(TrackedZombieCapacity);
        private int _activeBlades;
        private float _spinDegreesPerSecond;
        private float _damage;
        private float _knockback;
        private float _hitInterval;
        private float _sweepTimer;

        private void Awake()
        {
            bool missing = _flow == null || _zombies == null || _anchor == null || _pivot == null || _blades == null || _blades.Length == 0;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} OrbitBladesController has an unassigned reference.", this);
                return;
            }

            Deactivate();
        }

        // Idempotent on purpose: the skill calls this on every rebuild with the full numbers.
        public void Configure(int bladeCount, float radius, float spinDegreesPerSecond, float damage, float knockback, float hitInterval)
        {
            if (bladeCount > _blades.Length)
            {
                Debug.LogError($"{LogPrefix} asked for {bladeCount} blades but only {_blades.Length} are authored under Abilities.", this);
                bladeCount = _blades.Length;
            }

            _activeBlades = bladeCount;
            _spinDegreesPerSecond = spinDegreesPerSecond;
            _damage = damage;
            _knockback = knockback;
            _hitInterval = hitInterval;

            for (int i = 0; i < _blades.Length; i++)
            {
                bool on = i < bladeCount;
                _blades[i].gameObject.SetActive(on);
                if (!on)
                {
                    continue;
                }

                Quaternion turn = Quaternion.Euler(0f, 360f / bladeCount * i, 0f);
                _blades[i].localPosition = turn * Vector3.forward * radius;
                _blades[i].localRotation = turn;
            }

            _pivot.gameObject.SetActive(bladeCount > 0);
        }

        public void Deactivate()
        {
            _activeBlades = 0;
            for (int i = 0; i < _blades.Length; i++)
            {
                _blades[i].gameObject.SetActive(false);
            }

            _pivot.gameObject.SetActive(false);
            _nextHitTime.Clear();
        }

        private void Update()
        {
            if (_activeBlades == 0 || _flow.State != GameState.Playing)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float now = Time.time;
            transform.position = _anchor.position + Vector3.up * _height;
            _pivot.Rotate(0f, _spinDegreesPerSecond * deltaTime, 0f, Space.Self);
            for (int i = 0; i < _activeBlades; i++)
            {
                Slash(_blades[i], now);
            }

            SweepStale(deltaTime, now);
        }

        private void Slash(Transform blade, float now)
        {
            Vector3 center = blade.position;
            int count = Physics.OverlapSphereNonAlloc(center, _bladeRadius, _hitBuffer, _enemyMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!_zombies.TryGetZombie(_hitBuffer[i], out ZombieController zombie) || !zombie.IsTargetable)
                {
                    continue;
                }

                // A blade sits inside a body for many frames; the interval turns that into ticks.
                if (_nextHitTime.TryGetValue(zombie, out float nextHit) && now < nextHit)
                {
                    continue;
                }

                _nextHitTime[zombie] = now + _hitInterval;
                Vector3 direction = zombie.Position - _pivot.position;
                direction.y = 0f;
                zombie.TakeDamage(new DamageInfo(_damage, center, direction.normalized, _knockback, DamageSource.Melee));
            }
        }

        // Expired entries are dropped so the map cannot grow for the length of a run, and so a
        // pooled body that comes back as a fresh zombie does not inherit an old tick timer.
        private void SweepStale(float deltaTime, float now)
        {
            _sweepTimer += deltaTime;
            if (_sweepTimer < StaleSweepInterval)
            {
                return;
            }

            _sweepTimer = 0f;
            _stale.Clear();
            foreach (KeyValuePair<ZombieController, float> entry in _nextHitTime)
            {
                if (now >= entry.Value)
                {
                    _stale.Add(entry.Key);
                }
            }

            for (int i = 0; i < _stale.Count; i++)
            {
                _nextHitTime.Remove(_stale[i]);
            }
        }
    }
}
