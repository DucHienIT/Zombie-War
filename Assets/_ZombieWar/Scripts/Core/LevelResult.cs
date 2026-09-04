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

        public LevelResult(bool won, int kills, int score, int healthBonus, float damageTaken, bool isNewBest)
        {
            Won = won;
            Kills = kills;
            Score = score;
            HealthBonus = healthBonus;
            DamageTaken = damageTaken;
            IsNewBest = isNewBest;
        }

        public int TotalScore => Score + HealthBonus;
    }
}
