using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Roguelike Settings", fileName = "RoguelikeSettings")]
    public sealed class RoguelikeSettingsSO : ScriptableObject
    {
        [Header("Draft")]
        [SerializeField] private SkillDefinitionSO[] _skillPool;
        // Seeded at stack 1 when a run starts, so the bomb is always in hand: it is a spec
        // requirement and must not depend on the draft rolling it.
        [SerializeField] private ActiveSkillSO _startingAbility;
        // The first level-up offers only these, so every run gets an ability early.
        [SerializeField] private ActiveSkillSO[] _firstLevelPool;
        // Never larger than the card slots authored in the level-up popup.
        [SerializeField] private int _offersPerLevelUp = 3;

        [Header("Experience")]
        [SerializeField] private float _baseXpToLevel = 80f;
        // Each battle level costs this much more than the one before it.
        [SerializeField] private float _xpGrowthPerLevel = 1.35f;
        [SerializeField] private int _maxBattleLevel = 12;

        public SkillDefinitionSO[] SkillPool => _skillPool;
        public ActiveSkillSO StartingAbility => _startingAbility;
        public ActiveSkillSO[] FirstLevelPool => _firstLevelPool;
        public int OffersPerLevelUp => _offersPerLevelUp;
        public float BaseXpToLevel => _baseXpToLevel;
        public float XpGrowthPerLevel => _xpGrowthPerLevel;
        public int MaxBattleLevel => _maxBattleLevel;
    }
}
