using UnityEngine;

namespace ZombieWar.UI
{
    // What one upgrade card draws. The card never sees the PassiveSkillSO behind it, which
    // is what keeps the level-up popup inside UIRoot.prefab like every other screen.
    public readonly struct SkillCardData
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly Sprite Icon;
        public readonly Color AccentColor;
        // The level this pick would reach, so the star row previews the reward before the tap.
        public readonly int NextStack;
        public readonly int MaxStacks;

        public SkillCardData(string displayName, string description, Sprite icon, Color accentColor, int nextStack, int maxStacks)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            AccentColor = accentColor;
            NextStack = nextStack;
            MaxStacks = maxStacks;
        }

        public bool IsNew => NextStack <= 1;
    }
}
