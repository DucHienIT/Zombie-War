namespace ZombieWar.UI
{
    // What the shared menu header draws: player level, XP toward the next level and coins.
    public readonly struct MenuHeaderData
    {
        public readonly int Level;
        public readonly int Xp;
        public readonly int XpToNext;
        public readonly int Coins;

        public MenuHeaderData(int level, int xp, int xpToNext, int coins)
        {
            Level = level;
            Xp = xp;
            XpToNext = xpToNext;
            Coins = coins;
        }
    }
}
