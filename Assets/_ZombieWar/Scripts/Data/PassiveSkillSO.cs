using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Passive Skill", fileName = "PassiveSkill")]
    public sealed class PassiveSkillSO : SkillDefinitionSO
    {
        [Header("Effect")]
        // Applied once per owned stack; that is the whole behaviour of a passive, which is
        // what makes a new passive a new asset instead of new code.
        [SerializeField] private StatModifier[] _modifiers;

        public override SkillKind Kind => SkillKind.Passive;

        public override void Apply(in SkillApplyContext context, int stacks) => context.Stats.Apply(_modifiers, stacks);
    }
}
