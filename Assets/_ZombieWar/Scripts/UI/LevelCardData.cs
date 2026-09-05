namespace ZombieWar.UI
{
    // What a level card draws. The card slot never sees the LevelDefinitionSO behind it.
    public readonly struct LevelCardData
    {
        public readonly int Index;
        public readonly string DisplayName;
        public readonly bool Unlocked;
        public readonly int BestScore;

        public LevelCardData(int index, string displayName, bool unlocked, int bestScore)
        {
            Index = index;
            DisplayName = displayName;
            Unlocked = unlocked;
            BestScore = bestScore;
        }
    }
}
