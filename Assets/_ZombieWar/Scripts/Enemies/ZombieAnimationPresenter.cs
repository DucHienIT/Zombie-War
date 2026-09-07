using System;
using UnityEngine;

namespace ZombieWar.Enemies
{
    public sealed class ZombieAnimationPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[Zombie]";
        private const int BaseLayerIndex = 0;
        private const float NeutralLocomotionSpeed = 1f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int LocomotionSpeedHash = Animator.StringToHash("LocomotionSpeed");
        private static readonly int LocomotionStateHash = Animator.StringToHash("Locomotion");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int KnockbackHash = Animator.StringToHash("Knockback");
        private static readonly int HitSideHash = Animator.StringToHash("HitSide");

        [Serializable]
        private struct GaitVariant
        {
            [SerializeField] private RuntimeAnimatorController _controller;
            // Playback multiplier that brings this variant's stride cadence back to the shared blend tree thresholds.
            [SerializeField] private float _locomotionSpeed;

            public RuntimeAnimatorController Controller => _controller;
            public float LocomotionSpeed => _locomotionSpeed;
        }

        [SerializeField] private Animator _animator;
        // Upper-body flinch layer of ZombieAnimator.controller; muted while the death clip plays.
        [SerializeField] private int _hitReactionLayerIndex = 1;

        [Header("Blend")]
        [SerializeField] private float _speedDampTime = 0.1f;

        [Header("Variety")]
        // One entry is drawn per body so a crowd never shares the same walk cycle and claw swing.
        [SerializeField] private GaitVariant[] _gaits;

        private float _locomotionSpeed = NeutralLocomotionSpeed;

        private void Awake()
        {
            if (_gaits.Length == 0)
            {
                Debug.LogError($"{LogPrefix} {name} has no gait variant assigned.", this);
                return;
            }

            GaitVariant gait = _gaits[UnityEngine.Random.Range(0, _gaits.Length)];
            if (gait.Controller == null)
            {
                Debug.LogError($"{LogPrefix} {name} has a gait variant without a controller.", this);
                return;
            }

            _locomotionSpeed = gait.LocomotionSpeed;
            _animator.runtimeAnimatorController = gait.Controller;
        }

        public void SetSpeed(float metersPerSecond, float deltaTime)
        {
            _animator.SetFloat(SpeedHash, metersPerSecond, _speedDampTime, deltaTime);
        }

        public void TriggerAttack() => _animator.SetTrigger(AttackHash);
        public void TriggerHit(HitSide side)
        {
            _animator.SetInteger(HitSideHash, (int)side);
            _animator.SetTrigger(HitHash);
        }

        public void SetKnockback(bool active) => _animator.SetBool(KnockbackHash, active);

        public void TriggerDeath()
        {
            _animator.SetLayerWeight(_hitReactionLayerIndex, 0f);
            _animator.SetTrigger(DeathHash);
        }

        public void ResetAll()
        {
            // Rebind clears every parameter and returns to the default state so a pooled body never keeps a stale pose.
            _animator.Rebind();
            _animator.SetFloat(LocomotionSpeedHash, _locomotionSpeed);
            // A random phase keeps bodies that spawn side by side from stepping in lockstep.
            _animator.Play(LocomotionStateHash, BaseLayerIndex, UnityEngine.Random.value);
            _animator.Update(0f);
            _animator.SetLayerWeight(_hitReactionLayerIndex, 1f);
        }
    }
}
