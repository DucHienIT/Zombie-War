using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Roguelike Settings", fileName = "RoguelikeSettings")]
    public sealed class RoguelikeSettingsSO : ScriptableObject
    {
        [Header("Draft")]
        [SerializeField] private PassiveSkillSO[] _skillPool;
        // Never larger than the card slots authored in the level-up popup.
        [SerializeField] private int _offersPerLevelUp = 3;

        [Header("Experience")]
        [SerializeField] private float _baseXpToLevel = 80f;
        // Each battle level costs this much more than the one before it.
        [SerializeField] private float _xpGrowthPerLevel = 1.35f;
        [SerializeField] private int _maxBattleLevel = 12;

        public PassiveSkillSO[] SkillPool => _skillPool;
        public int OffersPerLevelUp => _offersPerLevelUp;
        public float BaseXpToLevel => _baseXpToLevel;
        public float XpGrowthPerLevel => _xpGrowthPerLevel;
        public int MaxBattleLevel => _maxBattleLevel;
    }
}
