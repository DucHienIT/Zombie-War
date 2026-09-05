using UnityEngine;

namespace ZombieWar.Roguelike
{
    // Battle levels are earned inside a single run and die with it. Pure math on purpose:
    // the curve can be checked without a scene, the same way ScoreTracker can.
    public sealed class BattleXpTracker
    {
        private readonly float _baseXpToLevel;
        private readonly float _growthPerLevel;
        private readonly int _maxLevel;
        private float _xpIntoLevel;

        public BattleXpTracker(float baseXpToLevel, float growthPerLevel, int maxLevel)
        {
            _baseXpToLevel = baseXpToLevel;
            _growthPerLevel = growthPerLevel;
            _maxLevel = maxLevel;
            Level = 1;
            XpForNextLevel = baseXpToLevel;
        }

        public int Level { get; private set; }
        public float XpForNextLevel { get; private set; }
        public bool IsMaxLevel => Level >= _maxLevel;
        public float Normalized => IsMaxLevel ? 1f : Mathf.Clamp01(_xpIntoLevel / XpForNextLevel);

        // Returns how many levels this XP completed: one fat kill can owe the player two picks.
        public int AddXp(float amount)
        {
            if (amount <= 0f || IsMaxLevel)
            {
                return 0;
            }

            _xpIntoLevel += amount;
            int gained = 0;
            while (!IsMaxLevel && _xpIntoLevel >= XpForNextLevel)
            {
                _xpIntoLevel -= XpForNextLevel;
                Level++;
                gained++;
                XpForNextLevel = _baseXpToLevel * Mathf.Pow(_growthPerLevel, Level - 1);
            }

            if (IsMaxLevel)
            {
                _xpIntoLevel = 0f;
            }

            return gained;
        }
    }
}
