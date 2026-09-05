using UnityEngine;

namespace ZombieWar.Data
{
    // Meta progression knobs: what a run pays out and how player levels grow.
    [CreateAssetMenu(menuName = "Zombie War/Progression Rules", fileName = "ProgressionRules")]
    public sealed class ProgressionRulesSO : ScriptableObject
    {
        [Header("Run Rewards")]
        [SerializeField] private int _xpPerKill = 2;
        [SerializeField] private int _xpWinBonus = 60;
        // Coins granted per point of total score.
        [SerializeField] private float _coinsPerScorePoint = 0.5f;
        [SerializeField] private int _coinsWinBonus = 100;

        [Header("Player Level")]
        [SerializeField] private int _startingCoins = 300;
        [SerializeField] private int _maxLevel = 50;
        [SerializeField] private int _baseXpToLevelUp = 100;
        [SerializeField] private float _xpGrowthPerLevel = 1.2f;

        public int StartingCoins => _startingCoins;
        public int MaxLevel => _maxLevel;

        public int XpToLevelUp(int level)
        {
            return Mathf.RoundToInt(_baseXpToLevelUp * Mathf.Pow(_xpGrowthPerLevel, Mathf.Max(0, level - 1)));
        }

        public int CoinsForRun(int totalScore, bool won)
        {
            return Mathf.RoundToInt(totalScore * _coinsPerScorePoint) + (won ? _coinsWinBonus : 0);
        }

        public int XpForRun(int kills, bool won)
        {
            return kills * _xpPerKill + (won ? _xpWinBonus : 0);
        }
    }
}
