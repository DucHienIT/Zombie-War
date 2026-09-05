using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.VFX;
using ZombieWar.Weapons;

namespace ZombieWar.Player
{
    // The persistent half of the sentry-drone ability. It lives outside the Player hierarchy on
    // purpose: the soldier snaps to face whatever they shoot, and a child drone would be flung
    // around with every turn. Instead the drone circles the soldier's world position on its
    // own clock, trailing a little when they sprint, and picks its own target - preferring a
    // body the soldier is not already shooting so it covers a flank.
    public sealed class SentryDroneController : MonoBehaviour
    {
        private const string LogPrefix = "[Ability]";
        private const int ScanBufferSize = 48;
        private const float NoTargetScore = float.MaxValue;
        private const float ZeroDirectionSqr = 0.0001f;

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerAim _playerAim;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private ProjectileManager _projectiles;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private AudioService _audio;
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _obstacleMask;
        // The soldier. Only its position is ever read, never its rotation.
        [SerializeField] private Transform _anchor;

        [Header("Body")]
        // Moved and turned in world space; the muzzle is a child of it.
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _muzzle;

        [Header("Flight")]
        [SerializeField] private float _orbitRadius = 1.4f;
        [SerializeField] private float _orbitDegreesPerSecond = 60f;
        [SerializeField] private float _hoverHeight = 1.7f;
        [SerializeField] private float _hoverAmplitude = 0.12f;
        [SerializeField] private float _hoverFrequency = 1.2f;
        // Seconds the drone needs to catch up with the soldier, so it is never welded to them.
        [SerializeField] private float _followSmoothTime = 0.2f;
        [SerializeField] private float _turnSpeedDegrees = 540f;
        // Below this horizontal speed the drone keeps its heading instead of chasing a tiny drift.
        [SerializeField] private float _headingSpeedThreshold = 0.2f;

        [Header("Targeting")]
        [SerializeField] private float _scanRange = 10f;
        [SerializeField] private float _scanInterval = 0.2f;
        // Metres added to a candidate's score while the soldier is already shooting it.
        [SerializeField] private float _sharedTargetPenalty = 4f;
        [SerializeField] private float _aimToleranceDegrees = 6f;

        private readonly Collider[] _scanBuffer = new Collider[ScanBufferSize];
        private GunDefinitionSO _gun;
        private float _damage;
        private float _fireInterval;
        private float _cooldown;
        private float _scanTimer;
        private float _hoverTime;
        private float _orbitAngle;
        private Vector3 _followVelocity;
        private ZombieController _target;
        private bool _active;

        private void Awake()
        {
            bool missing = _flow == null || _health == null || _playerAim == null || _zombies == null || _projectiles == null
                           || _vfx == null || _audio == null || _anchor == null || _body == null || _muzzle == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} SentryDroneController has an unassigned reference.", this);
                return;
            }

            Deactivate();
        }

        // Idempotent on purpose: the skill calls this on every rebuild with the full numbers.
        public void Configure(GunDefinitionSO gun, float damage, float fireInterval)
        {
            _gun = gun;
            _damage = damage;
            _fireInterval = fireInterval;
            if (_active)
            {
                return;
            }

            _active = true;
            // Appear on the orbit rather than gliding in from wherever the body was left.
            _body.position = OrbitPoint();
            _followVelocity = Vector3.zero;
            _body.gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            _active = false;
            _target = null;
            _body.gameObject.SetActive(false);
        }

        // After the soldier's own movement for the frame, so the drone trails the rendered position.
        private void LateUpdate()
        {
            if (!_active)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Fly(deltaTime);
            if (_flow.State != GameState.Playing || !_health.IsAlive)
            {
                return;
            }

            _scanTimer -= deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = _scanInterval;
                Retarget();
            }

            _cooldown -= deltaTime;
            if (_target != null)
            {
                TrackAndFire(deltaTime);
                return;
            }

            FaceTravelDirection(deltaTime);
        }

        // The orbit runs on its own clock in world space, so a soldier spinning on the spot
        // leaves the drone exactly where it was.
        private void Fly(float deltaTime)
        {
            _hoverTime += deltaTime;
            _orbitAngle += _orbitDegreesPerSecond * deltaTime;
            Vector3 goal = OrbitPoint();
            _body.position = Vector3.SmoothDamp(_body.position, goal, ref _followVelocity, _followSmoothTime, float.PositiveInfinity, deltaTime);
        }

        private Vector3 OrbitPoint()
        {
            float radians = _orbitAngle * Mathf.Deg2Rad;
            float bob = Mathf.Sin(_hoverTime * _hoverFrequency * Mathf.PI * 2f) * _hoverAmplitude;
            var offset = new Vector3(Mathf.Cos(radians) * _orbitRadius, _hoverHeight + bob, Mathf.Sin(radians) * _orbitRadius);
            return _anchor.position + offset;
        }

        private void FaceTravelDirection(float deltaTime)
        {
            Vector3 heading = _followVelocity;
            heading.y = 0f;
            if (heading.sqrMagnitude < _headingSpeedThreshold * _headingSpeedThreshold)
            {
                return;
            }

            Face(heading, deltaTime);
        }

        private void Face(Vector3 flatDirection, float deltaTime)
        {
            Quaternion look = Quaternion.LookRotation(flatDirection);
            _body.rotation = Quaternion.RotateTowards(_body.rotation, look, _turnSpeedDegrees * deltaTime);
        }

        // A committed target is kept while it is still worth shooting; otherwise the nearest
        // visible body wins, with the soldier's own mark pushed down the list.
        private void Retarget()
        {
            if (_target != null && IsStillValid(_target))
            {
                return;
            }

            Vector3 center = _body.position;
            int count = Physics.OverlapSphereNonAlloc(center, _scanRange, _scanBuffer, _enemyMask, QueryTriggerInteraction.Ignore);
            ZombieController best = null;
            float bestScore = NoTargetScore;
            for (int i = 0; i < count; i++)
            {
                if (!_zombies.TryGetZombie(_scanBuffer[i], out ZombieController candidate) || !candidate.IsTargetable)
                {
                    continue;
                }

                if (IsOccluded(candidate))
                {
                    continue;
                }

                float score = (candidate.Position - center).magnitude;
                if (candidate == _playerAim.CurrentTarget)
                {
                    score += _sharedTargetPenalty;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            _target = best;
        }

        private bool IsStillValid(ZombieController target)
        {
            if (!target.IsTargetable)
            {
                return false;
            }

            Vector3 offset = target.Position - _body.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _scanRange * _scanRange && !IsOccluded(target);
        }

        private bool IsOccluded(ZombieController candidate)
        {
            return Physics.Linecast(_muzzle.position, candidate.AimPoint.position, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private void TrackAndFire(float deltaTime)
        {
            Vector3 aimPoint = _target.AimPoint.position;
            Vector3 flat = aimPoint - _body.position;
            flat.y = 0f;
            if (flat.sqrMagnitude <= ZeroDirectionSqr)
            {
                return;
            }

            Face(flat, deltaTime);

            bool onTarget = Vector3.Angle(_body.forward, flat) <= _aimToleranceDegrees;
            if (!onTarget || _cooldown > 0f)
            {
                return;
            }

            Fire(aimPoint);
        }

        private void Fire(Vector3 aimPoint)
        {
            Vector3 origin = _muzzle.position;
            Vector3 direction = (aimPoint - origin).normalized;
            // Nothing from the run's passives or the menu shop reaches the drone: its gun is its own.
            var shot = new ShotStats(_damage, _gun.Knockback, 0);
            _projectiles.Spawn(origin, direction, _gun, shot);
            _vfx.Play(_gun.MuzzleVfx, _muzzle);
            _audio.PlayWorld(_gun.ShotClip, origin);
            _cooldown = _fireInterval;
        }
    }
}
