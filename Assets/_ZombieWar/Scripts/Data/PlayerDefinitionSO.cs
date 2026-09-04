using UnityEngine;

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

        [Header("Input")]
        [SerializeField] private float _joystickDeadZone = 0.12f;

        [Header("Auto Target")]
        [SerializeField] private float _targetScanInterval = 0.1f;
        [SerializeField] private float _targetHoldDuration = 0.35f;
        [SerializeField] private float _anglePenaltyWeight = 0.35f;
        [SerializeField] private float _occlusionGrace = 0.2f;

        [Header("Auto Fire")]
        [SerializeField] private float _aimToleranceDegrees = 8f;
        [SerializeField] private float _switchLockDuration = 0.25f;
        [SerializeField] private float _minCooldownAfterSwitch = 0.15f;

        public float MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float TurnSpeedDegrees => _turnSpeedDegrees;
        public float HitInvulnerability => _hitInvulnerability;
        public float JoystickDeadZone => _joystickDeadZone;
        public float TargetScanInterval => _targetScanInterval;
        public float TargetHoldDuration => _targetHoldDuration;
        public float AnglePenaltyWeight => _anglePenaltyWeight;
        public float OcclusionGrace => _occlusionGrace;
        public float AimToleranceDegrees => _aimToleranceDegrees;
        public float SwitchLockDuration => _switchLockDuration;
        public float MinCooldownAfterSwitch => _minCooldownAfterSwitch;
    }
}
