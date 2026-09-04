using UnityEngine;

namespace ZombieWar.Enemies
{
    public sealed class ZombieAnimationPresenter : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int KnockbackHash = Animator.StringToHash("Knockback");

        [SerializeField] private Animator _animator;

        [Header("Blend")]
        [SerializeField] private float _speedDampTime = 0.1f;

        public void SetSpeed(float normalizedSpeed, float deltaTime)
        {
            _animator.SetFloat(SpeedHash, normalizedSpeed, _speedDampTime, deltaTime);
        }

        public void TriggerAttack() => _animator.SetTrigger(AttackHash);
        public void TriggerHit() => _animator.SetTrigger(HitHash);
        public void TriggerDeath() => _animator.SetTrigger(DeathHash);
        public void SetKnockback(bool active) => _animator.SetBool(KnockbackHash, active);

        public void ResetAll()
        {
            // Rebind clears every parameter and returns to the default state so a pooled body never keeps a stale pose.
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
}
