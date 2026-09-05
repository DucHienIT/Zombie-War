using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Shockwave", fileName = "Skill_Shockwave")]
    public sealed class ShockwaveSkillSO : ActiveSkillSO
    {
        [Header("Auto Cast")]
        [SerializeField] private float _baseInterval = 8f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _intervalPerStack = 0.85f;
        [SerializeField] private float _minInterval = 3.5f;

        [Header("Blast")]
        [SerializeField] private float _baseRadius = 3.5f;
        // Added once per extra stack.
        [SerializeField] private float _radiusPerStack = 0.3f;
        [SerializeField] private float _baseDamage = 30f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _damagePerStack = 1.2f;
        // Above the zombie physics-knockback threshold on purpose: everything in reach is launched.
        [SerializeField] private float _force = 7f;
        // The wave waits, cooldown spent, until this many zombies stand inside it. It is a
        // get-off-me move, not a metronome.
        [SerializeField] private int _minZombiesToTrigger = 2;

        public override float CooldownFor(int stacks) => ScaledCooldown(_baseInterval, _intervalPerStack, _minInterval, stacks);

        public override bool CanTrigger(in AbilityContext context, int stacks)
        {
            return context.Shockwave.CountZombiesWithin(RadiusFor(stacks)) >= _minZombiesToTrigger;
        }

        public override void Trigger(in AbilityContext context, int stacks)
        {
            float damage = _baseDamage * Mathf.Pow(_damagePerStack, Mathf.Max(0, stacks - 1));
            context.Shockwave.Emit(RadiusFor(stacks), damage, _force);
        }

        private float RadiusFor(int stacks) => _baseRadius + _radiusPerStack * Mathf.Max(0, stacks - 1);
    }
}
