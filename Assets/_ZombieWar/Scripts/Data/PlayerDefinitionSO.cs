using UnityEngine;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Player Definition", fileName = "PlayerDefinition")]
    public sealed class PlayerDefinitionSO : ScriptableObject
    {
        [Header("Stats")]
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private float _moveSpeed = 4.6f;
        [SerializeField] private float _acceleration = 24f;
        [SerializeField] private float _turnSpeedDegrees = 720f;
        [SerializeField] private float _hitInvulnerability = 0.4f;

        [Header("Auto Target")]
        [SerializeField] private float _targetScanInterval = 0.1f;
        [SerializeField] private float _targetHoldDuration = 0.35f;
        [SerializeField] private float _anglePenaltyWeight = 0.35f;
        [SerializeField] private float _occlusionGrace = 0.2f;
        // A challenger must beat the current target's score by this fraction before the aim jumps to it.
        [SerializeField] private float _targetSwitchMargin = 0.25f;

        [Header("Auto Fire")]
        [SerializeField] private float _aimToleranceDegrees = 8f;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _hitVfx;
        [SerializeField] private float _hitCameraImpulse = 0.2f;

        public float MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float TurnSpeedDegrees => _turnSpeedDegrees;
        public float HitInvulnerability => _hitInvulnerability;
        public float TargetScanInterval => _targetScanInterval;
        public float TargetHoldDuration => _targetHoldDuration;
        public float AnglePenaltyWeight => _anglePenaltyWeight;
        public float OcclusionGrace => _occlusionGrace;
        public float TargetSwitchMargin => _targetSwitchMargin;
        public float AimToleranceDegrees => _aimToleranceDegrees;
        public PooledVfx HitVfx => _hitVfx;
        public float HitCameraImpulse => _hitCameraImpulse;
    }
}
