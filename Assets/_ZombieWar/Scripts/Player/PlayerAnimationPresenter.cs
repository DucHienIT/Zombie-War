using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Weapons;

namespace ZombieWar.Player
{
    public sealed class PlayerAnimationPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[Player]";

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int LocomotionSpeedHash = Animator.StringToHash("LocomotionSpeed");

        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private PlayerHealth _health;

        [Header("Blend")]
        [SerializeField] private float _locomotionDampTime = 0.08f;

        [Header("Locomotion Playback")]
        // Blend magnitude the walk clip sits at inside the locomotion tree; above it the tree ramps into the run clips.
        [Range(0.05f, 0.95f)] [SerializeField] private float _walkBlendThreshold = 0.5f;
        // Ground speed each locomotion clip was authored for, measured from its stance foot (m/s).
        [Min(0.01f)] [SerializeField] private float _walkClipSpeed = 2.2f;
        [Min(0.01f)] [SerializeField] private float _runClipSpeed = 5.7f;
        [Min(0.01f)] [SerializeField] private float _runBackwardClipSpeed = 3.05f;
        [SerializeField] private float _minPlaybackSpeed = 0.5f;
        [SerializeField] private float _maxPlaybackSpeed = 1.8f;
        [SerializeField] private float _playbackDampTime = 0.12f;

        private void Awake()
        {
            if (_animator == null || _motor == null || _weapons == null || _health == null)
            {
                Debug.LogError($"{LogPrefix} PlayerAnimationPresenter has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _weapons.OnShotFired += HandleShotFired;
            _weapons.OnGunChanged += HandleGunChanged;
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _weapons.OnShotFired -= HandleShotFired;
            _weapons.OnGunChanged -= HandleGunChanged;
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Vector2 local = _motor.LocalMoveDirection;
            _animator.SetFloat(MoveXHash, local.x, _locomotionDampTime, deltaTime);
            _animator.SetFloat(MoveYHash, local.y, _locomotionDampTime, deltaTime);
            _animator.SetFloat(SpeedHash, _motor.NormalizedSpeed, _locomotionDampTime, deltaTime);
            UpdateLocomotionPlayback(local, deltaTime);
        }

        // Keeps the stride in step with the ground speed: the clips were authored for a faster soldier than the one
        // we drive, and a MoveSpeed buff has to speed the legs up with it instead of sliding harder.
        private void UpdateLocomotionPlayback(Vector2 local, float deltaTime)
        {
            float blend = Mathf.Clamp01(_motor.NormalizedSpeed);
            float moveSpeed = _motor.CurrentMoveSpeed;
            float ratio;
            if (blend <= _walkBlendThreshold)
            {
                // Below the walk node the tree already scales the stride with the blend, so the ratio is constant;
                // fade it back to 1 at a standstill so the idle pose keeps its authored timing.
                float walkRatio = moveSpeed * _walkBlendThreshold / _walkClipSpeed;
                ratio = Mathf.Lerp(1f, walkRatio, blend / _walkBlendThreshold);
            }
            else
            {
                // Backpedalling runs a slower clip than the forward run, so weight the reference by the local heading.
                float backwardWeight = Mathf.Clamp01(-local.y / blend);
                float runClipSpeed = Mathf.Lerp(_runClipSpeed, _runBackwardClipSpeed, backwardWeight);
                float authored = Mathf.Lerp(_walkClipSpeed, runClipSpeed,
                    (blend - _walkBlendThreshold) / (1f - _walkBlendThreshold));
                ratio = moveSpeed * blend / authored;
            }

            ratio = Mathf.Clamp(ratio, _minPlaybackSpeed, _maxPlaybackSpeed);
            _animator.SetFloat(LocomotionSpeedHash, ratio, _playbackDampTime, deltaTime);
        }

        private void HandleShotFired(Gun gun) => _animator.SetTrigger(ShootHash);
        private void HandleGunChanged(Gun gun) => _animator.SetInteger(WeaponTypeHash, gun.Definition.AnimatorWeaponType);
        private void HandleDamaged(DamageInfo info) => _animator.SetTrigger(HitHash);
        private void HandleDied() => _animator.SetTrigger(DeathHash);
    }
}
