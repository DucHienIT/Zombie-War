using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Shockwave", fileName = "Skill_Shockwave")]
    public sealed class ShockwaveSkillSO : ActiveSkillSO
    {
        [Header("Aura")]
        // An aura beats on its own rhythm instead of waiting for a crowd to gather, so the
        // cooldown is that beat: one pulse a second, every second, whatever stands around.
        [SerializeField] private float _tickInterval = 1f;
        [SerializeField] private float _baseRadius = 3.5f;
        // Added once per extra stack.
        [SerializeField] private float _radiusPerStack = 0.3f;

        [Header("Damage")]
        // A fraction of the old single blast on purpose: the aura earns its damage over time.
        [SerializeField] private float _baseDamagePerTick = 7f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _damagePerStack = 1.2f;
        // Below the zombie physics-knockback threshold on purpose: a pulse shoves the crowd back
        // a step, it does not launch it. Launching every second would erase the chase entirely.
        [SerializeField] private float _knockback = 1.5f;

        // The beat never changes with rank - only the reach and the bite do.
        public override float CooldownFor(int stacks) => _tickInterval;

        // The aura effect is continuous even though the damage is not, so it is switched on
        // here (idempotent, re-sized whenever a new rank widens the circle) rather than per beat.
        public override void OnEquipped(in AbilityContext context, int stacks) => context.Shockwave.SetAura(RadiusFor(stacks));

        public override void OnUnequipped(in AbilityContext context) => context.Shockwave.Deactivate();

        public override void Trigger(in AbilityContext context, int stacks)
        {
            float damage = _baseDamagePerTick * Mathf.Pow(_damagePerStack, Mathf.Max(0, stacks - 1));
            context.Shockwave.Pulse(RadiusFor(stacks), damage, _knockback);
        }

        private float RadiusFor(int stacks) => _baseRadius + _radiusPerStack * Mathf.Max(0, stacks - 1);
    }
}
