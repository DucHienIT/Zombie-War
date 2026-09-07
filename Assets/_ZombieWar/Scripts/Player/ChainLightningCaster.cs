using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Enemies;
using ZombieWar.VFX;

namespace ZombieWar.Player
{
    // The persistent half of the chain-lightning ability: a pool of bolt renderers authored on
    // the Player, lit for a fraction of a second whenever the skill strikes. Bolts ignore line
    // of sight on purpose - the arc is what lets the soldier hit what is behind them.
    public sealed class ChainLightningCaster : MonoBehaviour
    {
        private const string LogPrefix = "[Ability]";
        private const int ScanBufferSize = 32;
        private const int ChainCapacity = 16;
        private const float ZeroDirectionSqr = 0.0001f;

        [SerializeField] private PlayerAim _aim;
        [SerializeField] private ZombieManager _zombies;
        // Chest-height point every strike starts from.
        [SerializeField] private Transform _origin;
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private AudioService _audio;

        [Header("Bolts")]
        // Authored count is the ceiling on bodies one strike can reach.
        [SerializeField] private LineRenderer[] _bolts;
        [SerializeField] private int _segmentsPerBolt = 6;
        // Metres each inner point is thrown off the straight line.
        [SerializeField] private float _jitter = 0.35f;
        [SerializeField] private float _boltDuration = 0.18f;
        [SerializeField] private float _flickerInterval;
        [SerializeField] private AnimationCurve _widthOverLifetime;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _hitVfx;
        [SerializeField] private AudioClip _zapClip;

        private readonly Collider[] _scanBuffer = new Collider[ScanBufferSize];
        private readonly List<ZombieController> _struck = new List<ZombieController>(ChainCapacity);
        private Vector3[] _points;
        private float[] _authoredWidths;
        private float _boltTimer;
        private float _flickerTimer;
        private Vector3[] _boltStarts;
        private Vector3[] _boltEnds;
        private int _litBolts;

        public bool HasTarget => _aim.HasTarget;

        private void Awake()
        {
            bool missing = _aim == null || _zombies == null || _origin == null || _vfx == null || _audio == null
                           || _bolts == null || _bolts.Length == 0;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} ChainLightningCaster has an unassigned reference.", this);
                return;
            }

            _points = new Vector3[_segmentsPerBolt + 1];
            _authoredWidths = new float[_bolts.Length];
            _boltStarts = new Vector3[_bolts.Length];
            _boltEnds = new Vector3[_bolts.Length];
            for (int i = 0; i < _bolts.Length; i++)
            {
                _authoredWidths[i] = _bolts[i].widthMultiplier;
                _bolts[i].positionCount = _points.Length;
                _bolts[i].gameObject.SetActive(false);
            }
        }

        public void Strike(float damage, float falloffPerJump, int jumps, float jumpRadius, float knockback)
        {
            if (!_aim.HasTarget)
            {
                return;
            }

            int maxTargets = 1 + jumps;
            if (maxTargets > _bolts.Length)
            {
                Debug.LogError($"{LogPrefix} asked for {maxTargets} bolts but only {_bolts.Length} are authored on the Player.", this);
                maxTargets = _bolts.Length;
            }

            _struck.Clear();
            ClearBolts();
            ZombieController current = _aim.CurrentTarget;
            Vector3 from = _origin.position;
            float amount = damage;
            int count = 0;
            while (current != null && count < maxTargets)
            {
                Vector3 hitPoint = current.AimPoint.position;
                Vector3 direction = FlatDirection(from, hitPoint);
                _struck.Add(current);
                DrawBolt(count, from, hitPoint);
                _vfx.Play(_hitVfx, hitPoint, Quaternion.LookRotation(-direction));
                current.TakeDamage(new DamageInfo(amount, hitPoint, direction, knockback, DamageSource.Shock));

                amount *= falloffPerJump;
                from = hitPoint;
                count++;
                current = FindNext(current.Position, jumpRadius);
            }

            _litBolts = count;
            _boltTimer = _boltDuration;
            _flickerTimer = _flickerInterval;
            _audio.PlayWorld(_zapClip, _origin.position);
        }

        private Vector3 FlatDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > ZeroDirectionSqr ? direction.normalized : _origin.forward;
        }

        // Nearest body in reach that this strike has not touched yet, or null when the chain ends.
        private ZombieController FindNext(Vector3 center, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, _scanBuffer, _enemyMask, QueryTriggerInteraction.Ignore);
            ZombieController best = null;
            float bestDistanceSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!_zombies.TryGetZombie(_scanBuffer[i], out ZombieController candidate) || !candidate.IsTargetable)
                {
                    continue;
                }

                if (_struck.Contains(candidate))
                {
                    continue;
                }

                float distanceSqr = (candidate.Position - center).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    best = candidate;
                }
            }

            return best;
        }

        private void DrawBolt(int index, Vector3 from, Vector3 to)
        {
            _boltStarts[index] = from;
            _boltEnds[index] = to;
            int last = _points.Length - 1;
            float phase = Time.time / Mathf.Max(_flickerInterval, Mathf.Epsilon);
            for (int i = 0; i <= last; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, i / (float)last);
                bool inner = i > 0 && i < last;
                // Visual noise must not consume the random sequence used by combat and loot.
                Vector3 offset = new Vector3(
                    Mathf.PerlinNoise(index + i, phase) * 2f - 1f,
                    Mathf.PerlinNoise(index + i + 17f, phase) * 2f - 1f,
                    Mathf.PerlinNoise(index + i + 37f, phase) * 2f - 1f);
                _points[i] = inner ? point + offset * (_jitter * Mathf.Sin(Mathf.PI * i / last)) : point;
            }

            LineRenderer bolt = _bolts[index];
            bolt.SetPositions(_points);
            bolt.widthMultiplier = _authoredWidths[index];
            bolt.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_litBolts == 0)
            {
                return;
            }

            _boltTimer -= Time.deltaTime;
            float remaining = Mathf.Clamp01(_boltTimer / _boltDuration);
            _flickerTimer -= Time.deltaTime;
            bool redraw = _flickerTimer <= 0f;
            if (redraw) _flickerTimer = _flickerInterval;
            for (int i = 0; i < _litBolts; i++)
            {
                if (redraw) DrawBolt(i, _boltStarts[i], _boltEnds[i]);
                _bolts[i].widthMultiplier = _authoredWidths[i] * _widthOverLifetime.Evaluate(1f - remaining);
            }

            if (_boltTimer > 0f)
            {
                return;
            }

            ClearBolts();
        }

        private void OnDisable() => ClearBolts();

        private void ClearBolts()
        {
            for (int i = 0; i < _litBolts; i++)
            {
                _bolts[i].gameObject.SetActive(false);
            }

            _litBolts = 0;
        }
    }
}
