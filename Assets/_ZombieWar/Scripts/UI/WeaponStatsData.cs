namespace ZombieWar.UI
{
    public readonly struct WeaponStatsData
    {
        public readonly float Damage;
        public readonly float ShotsPerSecond;

        public WeaponStatsData(float damage, float shotsPerSecond)
        {
            Damage = damage;
            ShotsPerSecond = shotsPerSecond;
        }
    }
}
