using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    // An ability that fires itself. The player never taps anything for these: the game already
    // aims and shoots on its own, so a button would fight that design.
    //
    // The asset is shared by every instance of the game, so it must stay stateless - cooldown
    // and stack count live in AbilityRunner, never here.
    public abstract class ActiveSkillSO : SkillDefinitionSO
    {
        public sealed override SkillKind Kind => SkillKind.Active;

        public sealed override void Apply(in SkillApplyContext context, int stacks) => context.Abilities.Equip(this, stacks);

        // Seconds between automatic triggers. Zero or less marks a continuous ability: the
        // runner never ticks it, and it does its work from OnEquipped instead.
        public abstract float CooldownFor(int stacks);

        // Asked once the cooldown has run out, every frame until it says yes; the runner holds
        // the cooldown at zero meanwhile, so a trigger is never spent on an empty field.
        public virtual bool CanTrigger(in AbilityContext context, int stacks) => true;

        // Called on every rebuild, so it has to be idempotent - it sets up a continuous
        // ability to match the current stack count rather than adding to what is there.
        public virtual void OnEquipped(in AbilityContext context, int stacks) { }

        // Called once when a rebuild drops the ability, which only happens when a new run
        // starts. A continuous ability switches its persistent piece back off here.
        public virtual void OnUnequipped(in AbilityContext context) { }

        public virtual void Trigger(in AbilityContext context, int stacks) { }

        // The shared cooldown curve: every stack past the first multiplies the interval once,
        // and the floor keeps a maxed skill from turning into a metronome.
        protected static float ScaledCooldown(float baseInterval, float intervalPerStack, float minInterval, int stacks)
        {
            float interval = baseInterval * Mathf.Pow(intervalPerStack, Mathf.Max(0, stacks - 1));
            return Mathf.Max(minInterval, interval);
        }
    }
}
