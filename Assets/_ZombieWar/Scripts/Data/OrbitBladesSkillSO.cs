using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Orbit Blades", fileName = "Skill_OrbitBlades")]
    public sealed class OrbitBladesSkillSO : ActiveSkillSO
    {
        [Header("Blades")]
        [SerializeField] private int _baseBlades = 2;
        [SerializeField] private int _bladesPerStack = 1;
        [SerializeField] private float _orbitRadius = 2.2f;
        [SerializeField] private float _spinDegreesPerSecond = 240f;

        [Header("Damage")]
        [SerializeField] private float _baseDamage = 14f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _damagePerStack = 1.2f;
        // Below the zombie physics-knockback threshold on purpose: a blade nudges, it does not launch.
        [SerializeField] private float _knockback = 1.2f;
        // Seconds between hits on the same zombie while a blade is inside it.
        [SerializeField] private float _hitInterval = 0.4f;

        // Continuous: the blades are simply there, so the runner never ticks this one.
        public override float CooldownFor(int stacks) => 0f;

        public override void OnEquipped(in AbilityContext context, int stacks)
        {
            int extraStacks = Mathf.Max(0, stacks - 1);
            int blades = _baseBlades + _bladesPerStack * extraStacks;
            float damage = _baseDamage * Mathf.Pow(_damagePerStack, extraStacks);
            context.Blades.Configure(blades, _orbitRadius, _spinDegreesPerSecond, damage, _knockback, _hitInterval);
        }

        public override void OnUnequipped(in AbilityContext context) => context.Blades.Deactivate();
    }
}
