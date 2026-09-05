using UnityEngine;

namespace ZombieWar.UI
{
    // One skill tree node as the page draws it; the UI never sees SkillTreeNodeSO.
    public readonly struct SkillNodeData
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string EffectLabel;
        // TextMeshPro SetText format for CurrentValue / NextValue.
        public readonly string ValueFormat;
        public readonly Sprite Icon;
        public readonly Color Accent;
        public readonly int Rank;
        public readonly int MaxRank;
        // Every prerequisite holds a rank; a locked node still opens its detail sheet.
        public readonly bool Unlocked;
        public readonly string RequiredNodeName;
        public readonly int Cost;
        public readonly bool CanAfford;
        public readonly int CoinsMissing;
        public readonly float CurrentValue;
        public readonly float NextValue;

        public SkillNodeData(string displayName, string description, string effectLabel, string valueFormat, Sprite icon,
            Color accent, int rank, int maxRank, bool unlocked, string requiredNodeName, int cost, bool canAfford,
            int coinsMissing, float currentValue, float nextValue)
        {
            DisplayName = displayName;
            Description = description;
            EffectLabel = effectLabel;
            ValueFormat = valueFormat;
            Icon = icon;
            Accent = accent;
            Rank = rank;
            MaxRank = maxRank;
            Unlocked = unlocked;
            RequiredNodeName = requiredNodeName;
            Cost = cost;
            CanAfford = canAfford;
            CoinsMissing = coinsMissing;
            CurrentValue = currentValue;
            NextValue = nextValue;
        }

        public bool IsMaxed => Rank >= MaxRank;
        public bool IsOwned => Rank > 0;
        public bool CanBuy => Unlocked && !IsMaxed && CanAfford;
    }
}
