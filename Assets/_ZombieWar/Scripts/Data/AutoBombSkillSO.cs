using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Active Skill/Auto Bomb", fileName = "Skill_AutoBomb")]
    public sealed class AutoBombSkillSO : ActiveSkillSO
    {
        [Header("Auto Throw")]
        [SerializeField] private float _baseInterval = 7f;
        // Multiplied in once per extra stack, so 0.84 reads as 16% faster per level.
        [SerializeField] private float _intervalPerStack = 0.84f;
        // A bomb is in the air for over a second; volleys closer than this land on each other.
        [SerializeField] private float _minInterval = 2f;

        public override float CooldownFor(int stacks) => ScaledCooldown(_baseInterval, _intervalPerStack, _minInterval, stacks);

        public override void Trigger(in AbilityContext context, int stacks) => context.Bombs.ThrowVolley();
    }
}
