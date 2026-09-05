using UnityEngine;
using ZombieWar.Data;
using ZombieWar.Enemies;

namespace ZombieWar.Player
{
    public sealed class PlayerAim : MonoBehaviour
    {
        private const string LogPrefix = "[Aim]";
        private const int ScanBufferSize = 64;
        private const float NoTargetScore = float.MaxValue;

        [SerializeField] private PlayerDefinitionSO _definition;
        [SerializeField] private ZombieManager _zombies;
        // Chest-height origin used for line-of-sight checks.
        [SerializeField] private Transform _aimOrigin;
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _obstacleMask;

        private readonly Collider[] _scanBuffer = new Collider[ScanBufferSize];
        private Transform _transform;
        private float _scanRange;
        private float _scanTimer;
        private float _holdTimer;
        private float _occludedTimer;

        public ZombieController CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null && CurrentTarget.IsTargetable;
        public Vector3 TargetPosition => CurrentTarget.Position;
        public float AimErrorDegrees { get; private set; }

        private void Awake()
        {
            _transform = transform;
            if (_definition == null || _zombies == null || _aimOrigin == null)
            {
                Debug.LogError($"{LogPrefix} Definition, zombie manager or aim origin is missing.", this);
            }
        }

        public void SetScanRange(float range)
        {
            _scanRange = range;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _holdTimer += deltaTime;
            _scanTimer += deltaTime;

            if (CurrentTarget != null && !IsWithinRange(CurrentTarget))
            {
                ClearTarget();
            }

            if (_scanTimer >= _definition.TargetScanInterval)
            {
                _scanTimer = 0f;
                Scan();
            }

            UpdateAimError();
        }

        private void Scan()
        {
            Vector3 origin = _transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, _scanRange, _scanBuffer, _enemyMask, QueryTriggerInteraction.Ignore);

            ZombieController best = null;
            float bestScore = NoTargetScore;
            float currentScore = NoTargetScore;
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

                float score = ScoreCandidate(candidate, origin);
                if (candidate == CurrentTarget)
                {
                    currentScore = score;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            TrackOcclusion();

            bool currentIsValid = HasTarget && _occludedTimer < _definition.OcclusionGrace;
            bool holdActive = currentIsValid && _holdTimer < _definition.TargetHoldDuration;
            if (holdActive || best == CurrentTarget)
            {
                return;
            }

            if (best == null && currentIsValid)
            {
                return;
            }

            // In a crowd the nearest body changes every scan; a challenger only takes the aim
            // when it is clearly better, so the soldier stops whipping between neighbours.
            bool challengerTooClose = currentIsValid && bestScore > currentScore * (1f - _definition.TargetSwitchMargin);
            if (challengerTooClose)
            {
                return;
            }

            CurrentTarget = best;
            _holdTimer = 0f;
            _occludedTimer = 0f;
        }

        private void TrackOcclusion()
        {
            if (!HasTarget)
            {
                return;
            }

            if (IsOccluded(CurrentTarget))
            {
                _occludedTimer += _definition.TargetScanInterval;
                if (_occludedTimer >= _definition.OcclusionGrace)
                {
                    ClearTarget();
                }
            }
            else
            {
                _occludedTimer = 0f;
            }
        }

        private float ScoreCandidate(ZombieController candidate, Vector3 origin)
        {
            Vector3 toTarget = candidate.Position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float angle = Vector3.Angle(_transform.forward, toTarget);
            return distance + angle * _definition.AnglePenaltyWeight;
        }

        private bool IsOccluded(ZombieController candidate)
        {
            Vector3 from = _aimOrigin.position;
            Vector3 to = candidate.AimPoint.position;
            return Physics.Linecast(from, to, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private bool IsWithinRange(ZombieController candidate)
        {
            Vector3 offset = candidate.Position - _transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _scanRange * _scanRange;
        }

        private void UpdateAimError()
        {
            if (!HasTarget)
            {
                AimErrorDegrees = float.MaxValue;
                return;
            }

            Vector3 toTarget = CurrentTarget.Position - _transform.position;
            toTarget.y = 0f;
            AimErrorDegrees = Vector3.Angle(_transform.forward, toTarget);
        }

        private void ClearTarget()
        {
            CurrentTarget = null;
            _holdTimer = 0f;
            _occludedTimer = 0f;
        }
    }
}
