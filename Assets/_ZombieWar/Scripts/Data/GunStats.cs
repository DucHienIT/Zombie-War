namespace ZombieWar.Data
{
    // Effective ballistics of a gun at one upgrade level; GunDefinitionSO owns the base values.
    public readonly struct GunStats
    {
        public readonly float Damage;
        public readonly float FireInterval;

        public GunStats(float damage, float fireInterval)
        {
            Damage = damage;
            FireInterval = fireInterval;
        }

        public float ShotsPerSecond => FireInterval > 0f ? 1f / FireInterval : 0f;
    }
}
