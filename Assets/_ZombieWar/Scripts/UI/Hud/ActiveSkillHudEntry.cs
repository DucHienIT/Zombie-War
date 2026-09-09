using UnityEngine;

namespace ZombieWar.UI
{
    // What one HUD slot draws: the icon and how much of its cooldown is left (0 = ready,
    // 1 = just triggered). Never sees the ActiveSkillSO behind it, the same way SkillCardData
    // never sees it either.
    public readonly struct ActiveSkillHudEntry
    {
        public readonly Sprite Icon;
        public readonly float CooldownFraction;

        public ActiveSkillHudEntry(Sprite icon, float cooldownFraction)
        {
            Icon = icon;
            CooldownFraction = cooldownFraction;
        }
    }
}
