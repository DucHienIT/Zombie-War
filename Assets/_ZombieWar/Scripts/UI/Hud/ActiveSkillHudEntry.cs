using UnityEngine;

namespace ZombieWar.UI
{
    // What one HUD slot draws: the icon, how many stacks are owned, and how much of its
    // cooldown is left (0 = ready, 1 = just triggered). Never sees the ActiveSkillSO behind
    // it, the same way SkillCardData never sees it either.
    public readonly struct ActiveSkillHudEntry
    {
        public readonly Sprite Icon;
        public readonly int Stacks;
        public readonly float CooldownFraction;

        public ActiveSkillHudEntry(Sprite icon, int stacks, float cooldownFraction)
        {
            Icon = icon;
            Stacks = stacks;
            CooldownFraction = cooldownFraction;
        }
    }
}
