using UnityEngine;
using ZombieWar.Player;
using ZombieWar.Weapons;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Molotov", fileName = "Skill_Molotov")]
    public sealed class MolotovSkillSO : ActiveSkillSO
    {
        [Header("Auto Throw")]
        [SerializeField] private float _baseInterval = 9f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _intervalPerStack = 0.85f;
        [SerializeField] private float _minInterval = 4f;

        [Header("Fire")]
        [SerializeField] private float _radius = 2.2f;
        [SerializeField] private float _baseBurnDuration = 4f;
        // Added once per extra stack.
        [SerializeField] private float _burnDurationPerStack = 1f;
        [SerializeField] private float _baseDamagePerTick = 8f;
        // Multiplied in once per extra stack.
        [SerializeField] private float _damagePerStack = 1.15f;
        [SerializeField] private float _tickInterval = 0.4f;

        public override float CooldownFor(int stacks) => ScaledCooldown(_baseInterval, _intervalPerStack, _minInterval, stacks);

        public override void Trigger(in AbilityContext context, int stacks)
        {
            int extraStacks = Mathf.Max(0, stacks - 1);
            var burn = new MolotovBurn(
                _radius,
                _baseBurnDuration + _burnDurationPerStack * extraStacks,
                _baseDamagePerTick * Mathf.Pow(_damagePerStack, extraStacks),
                _tickInterval);
            context.Molotovs.Throw(burn);
        }
    }
}
