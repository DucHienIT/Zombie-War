using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Data
{
    // Everything a level-up card needs to draw, plus the one hook that says what owning the
    // skill actually does. Passive and active skills share this so the draft, the popup and
    // the star row never learn the difference between them.
    public abstract class SkillDefinitionSO : ScriptableObject
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

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color AccentColor => _accentColor;
        public int MaxStacks => _maxStacks;
        public float DraftWeight => _draftWeight;
        // Passive and active read the same on a card unless the card is told which it is.
        public abstract SkillKind Kind { get; }

        // Called for every owned skill on every rebuild, so it must be idempotent: read the
        // stack count, write the result, never accumulate onto whatever was there before.
        public abstract void Apply(in SkillApplyContext context, int stacks);
    }
}
