namespace ZombieWar.Core
{
    public readonly struct LevelResult
    {
        public readonly bool Won;
        public readonly int Kills;
        public readonly int Score;
        public readonly int HealthBonus;
        public readonly float DamageTaken;
        public readonly bool IsNewBest;
        public readonly int CoinsEarned;
        public readonly int XpEarned;

        public LevelResult(bool won, int kills, int score, int healthBonus, float damageTaken, bool isNewBest, int coinsEarned, int xpEarned)
        {
            Won = won;
            Kills = kills;
            Score = score;
            HealthBonus = healthBonus;
            DamageTaken = damageTaken;
            IsNewBest = isNewBest;
            CoinsEarned = coinsEarned;
            XpEarned = xpEarned;
        }

        public int TotalScore => Score + HealthBonus;
    }
}
