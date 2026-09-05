using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Passive Skill", fileName = "PassiveSkill")]
    public sealed class PassiveSkillSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _accentColor;

        [Header("Draft")]
        [SerializeField] private int _maxStacks = 5;
        // Relative chance of being offered against the other skills still under their cap.
        [SerializeField] private float _draftWeight = 1f;

        [Header("Effect")]
        // Applied once per owned stack; that is the whole behaviour of a passive, which is
        // what lets a new skill be a new asset instead of new code.
        [SerializeField] private StatModifier[] _modifiers;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color AccentColor => _accentColor;
        public int MaxStacks => _maxStacks;
        public float DraftWeight => _draftWeight;
        public StatModifier[] Modifiers => _modifiers;
    }
}
