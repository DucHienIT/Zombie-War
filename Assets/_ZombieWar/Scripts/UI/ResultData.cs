namespace ZombieWar.UI
{
    // Presentation copy of a finished run. The UI never touches the gameplay result type,
    // which is what lets the whole UI tree live in a prefab of its own.
    public readonly struct ResultData
    {
        public readonly bool Won;
        public readonly int Kills;
        public readonly int Score;
        public readonly int HealthBonus;
        public readonly int DamageTaken;
        public readonly int TotalScore;
        public readonly bool IsNewBest;
        public readonly bool HasNextLevel;
        public readonly int CoinsEarned;
        public readonly int XpEarned;

        public ResultData(bool won, int kills, int score, int healthBonus, int damageTaken, int totalScore, bool isNewBest, bool hasNextLevel,
            int coinsEarned, int xpEarned)
        {
            Won = won;
            Kills = kills;
            Score = score;
            HealthBonus = healthBonus;
            DamageTaken = damageTaken;
            TotalScore = totalScore;
            IsNewBest = isNewBest;
            HasNextLevel = hasNextLevel;
            CoinsEarned = coinsEarned;
            XpEarned = xpEarned;
        }
    }
}
