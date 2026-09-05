using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Sentry Drone", fileName = "Skill_SentryDrone")]
    public sealed class SentryDroneSkillSO : ActiveSkillSO
    {
        [Header("Drone")]
        // Ballistics, VFX and clip of the drone's gun; its damage and fire interval are the stack-1 values.
        [SerializeField] private GunDefinitionSO _gun;
        // Multiplied in once per extra stack.
        [SerializeField] private float _damagePerStack = 1.2f;
        // Multiplied in once per extra stack, so 0.9 reads as 10% faster per level.
        [SerializeField] private float _fireIntervalPerStack = 0.9f;

        // Continuous: the drone simply hovers there, so the runner never ticks this one.
        public override float CooldownFor(int stacks) => 0f;

        public override void OnEquipped(in AbilityContext context, int stacks)
        {
            int extraStacks = Mathf.Max(0, stacks - 1);
            float damage = _gun.Damage * Mathf.Pow(_damagePerStack, extraStacks);
            float fireInterval = _gun.FireInterval * Mathf.Pow(_fireIntervalPerStack, extraStacks);
            context.Drone.Configure(_gun, damage, fireInterval);
        }

        public override void OnUnequipped(in AbilityContext context) => context.Drone.Deactivate();
    }
}
