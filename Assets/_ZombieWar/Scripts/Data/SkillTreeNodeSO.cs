using UnityEngine;

namespace ZombieWar.Data
{
    // One purchasable node of the permanent skill tree. Every rank applies the same modifiers
    // once more, so a node is one stat with a price curve; the shape of the tree lives in the
    // prerequisites, not in code.
    [CreateAssetMenu(menuName = "Zombie War/Skill Tree Node", fileName = "Node")]
    public sealed class SkillTreeNodeSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _accentColor;

        [Header("Effect")]
        // The first entry is the one the detail panel quotes as "current -> next".
        [SerializeField] private StatModifier[] _modifiersPerRank;
        [SerializeField] private string _effectLabel;
        // TextMeshPro SetText format of the quoted value, e.g. "+{0:0}%" or "+{0:1}".
        [SerializeField] private string _valueFormat = "+{0:0}";
        [SerializeField] private int _maxRank = 5;

        [Header("Cost")]
        [SerializeField] private int _baseCost = 100;
        [SerializeField] private float _costGrowth = 1.5f;

        [Header("Tree")]
        // Each listed node needs at least one rank before this one opens; none marks a root.
        [SerializeField] private SkillTreeNodeSO[] _prerequisites;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color AccentColor => _accentColor;
        public StatModifier[] ModifiersPerRank => _modifiersPerRank;
        public string EffectLabel => _effectLabel;
        public string ValueFormat => _valueFormat;
        public int MaxRank => _maxRank;
        public SkillTreeNodeSO[] Prerequisites => _prerequisites;

        // Price of the step from currentRank to currentRank + 1.
        public int GetCost(int currentRank)
        {
            return Mathf.RoundToInt(_baseCost * Mathf.Pow(_costGrowth, Mathf.Max(0, currentRank)));
        }

        // The quoted bonus at a rank. Additive stacks linearly; multiplicative compounds and is
        // quoted as a percentage, so two ranks of x1.06 read as the +12.4% they really are.
        public float EffectValueAt(int rank)
        {
            if (_modifiersPerRank == null || _modifiersPerRank.Length == 0 || rank <= 0)
            {
                return 0f;
            }

            StatModifier primary = _modifiersPerRank[0];
            if (primary.Kind == StatModifierKind.Additive)
            {
                return primary.ValuePerStack * rank;
            }

            return (Mathf.Pow(primary.ValuePerStack, rank) - 1f) * 100f;
        }
    }
}
