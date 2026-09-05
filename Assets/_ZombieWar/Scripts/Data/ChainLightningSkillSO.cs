using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Chain Lightning", fileName = "Skill_ChainLightning")]
    public sealed class ChainLightningSkillSO : ActiveSkillSO
    {
        [Header("Auto Cast")]
        [SerializeField] private float _baseInterval = 5f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _intervalPerStack = 0.88f;
        [SerializeField] private float _minInterval = 2.5f;

        [Header("Bolt")]
        [SerializeField] private float _baseDamage = 40f;
        // Each body after the first takes this fraction of the previous hit.
        [SerializeField] private float _damageFalloffPerJump = 0.8f;
        // Bodies struck after the first one; the caster needs 1 + max jumps bolts authored.
        [SerializeField] private int _baseJumps = 3;
        [SerializeField] private int _jumpsPerStack = 1;
        [SerializeField] private float _jumpRadius = 4f;
        // Below the zombie physics-knockback threshold on purpose: a bolt makes the body flinch, not fly.
        [SerializeField] private float _knockback = 1f;

        public override float CooldownFor(int stacks) => ScaledCooldown(_baseInterval, _intervalPerStack, _minInterval, stacks);

        // A bolt into empty air would waste the whole cooldown; wait until the soldier has a mark.
        public override bool CanTrigger(in AbilityContext context, int stacks) => context.Lightning.HasTarget;

        public override void Trigger(in AbilityContext context, int stacks)
        {
            int jumps = _baseJumps + _jumpsPerStack * Mathf.Max(0, stacks - 1);
            context.Lightning.Strike(_baseDamage, _damageFalloffPerJump, jumps, _jumpRadius, _knockback);
        }
    }
}
