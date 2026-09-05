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

        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private PlayerHealth _health;

        [Header("Blend")]
        [SerializeField] private float _locomotionDampTime = 0.08f;

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
        }

        private void HandleShotFired(Gun gun) => _animator.SetTrigger(ShootHash);
        private void HandleGunChanged(Gun gun) => _animator.SetInteger(WeaponTypeHash, gun.Definition.AnimatorWeaponType);
        private void HandleDamaged(DamageInfo info) => _animator.SetTrigger(HitHash);
        private void HandleDied() => _animator.SetTrigger(DeathHash);
    }
}
